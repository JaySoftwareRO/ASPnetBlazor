using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ui_agent.Models;
using lib.poshmark_client;
using Microsoft.Extensions.Configuration;
using lib.cache.bifrost;
using lib;
using lib.token_getters;
using System.Text;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json;

namespace ui_agent.Controllers
{
    public class ItemController : Controller
    {
        private readonly ILogger<ItemController> logger;
        private readonly IConfiguration configuration;
        private readonly ITokenGetters tokenGetters;

        public ItemController(ILogger<ItemController> logger, IConfiguration configuration, ITokenGetters tokenGetters)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.tokenGetters = tokenGetters;
        }

        public IActionResult Add()
        {
            return View();
        }

        public IActionResult Edit()
        {
            return View();
        }

        public IActionResult FrameTest()
        {
            return View();
        }

        public async Task<IActionResult> ImportPoshmark()
        {
            var authToken = await this.tokenGetters.Google.GetToken();

            var tokenGetter = tokenGetters.Poshmark;
            var token = await tokenGetter.GetToken();

            if (string.IsNullOrWhiteSpace(token))
            {
                // TODO: redirect to a "not logged into" page
                return RedirectToAction("welcome", "item");
            }

            string bifrostURL = this.configuration["Bifrost:Service"];
            var cache = new BifrostCache(bifrostURL, "poshmark-items", authToken, logger);

            var items = new List<Item>();

            try
            {   
                // TODO: live call limits should not be hardcoded
                items = new lib.listers.PoshmarkLister(cache, this.logger, 10000, tokenGetter).List().Result;
            }
            catch (Exception)
            {
                this.ViewBag.EmptyInventoryMessage = "Please add items to your Poshmark inventory.";
            }

            this.ViewBag.Selected = "poshmark";
            return View("importdata", items);
        }

        public async Task<IActionResult> ImportEbay()
        {
            // TODO: handle ebay tokens in a more generic way, maybe some middleware with
            // annotations on the actions
            var authToken = await this.tokenGetters.Google.GetToken();

            var tokenGetter = tokenGetters.EbayAccess;
            var accessToken = await tokenGetter.GetToken();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                var refreshTokenGetter = tokenGetters.Ebay;
                var refreshToken = await refreshTokenGetter.GetToken();

                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    // TODO: redirect to a "not logged into" page
                    return RedirectToAction("welcome", "item");
                }

                // Use the refresh token to get an access token
                var newAccessToken = EbayTokenUtils.AccessTokenFromRefreshToken(refreshToken, tokenGetter.Scopes(), logger);

                if (newAccessToken == null)
                {
                    await this.tokenGetters.Ebay.Set("", "");
                    // TODO: redirect to a "not logged into" page
                    return RedirectToAction("welcome", "item");
                }

                await this.tokenGetters.Ebay.Set(newAccessToken.AccessToken, newAccessToken.UserID);    
            }

            string bifrostURL = this.configuration["Bifrost:Service"];
            var cache = new BifrostCache(bifrostURL, "ebay-items", authToken, logger);

            var items = new lib.listers.EbayLister(cache, this.logger, 10000, tokenGetter).List().Result;

            this.ViewBag.Selected = "ebay";
            return View("importdata", items);
        }

        [HttpPost]
        public async Task<IActionResult> ImportData([FromBody] ItemImportModel itemsToImport)
        {
            string bifrostURL = this.configuration["Bifrost:Service"];
            var authToken = await this.tokenGetters.Google.GetToken(); // Get Google auth token\
            var treecatServiceCache = new BifrostCache(bifrostURL, "treecat-items", authToken, logger); // Connect to the TreeCat service cache

            // Check which field has the more than 0 IDs stored and return view to the appropriate action
            // Ebay try/catch
            try
            {
                if (itemsToImport.EbayIDs.Count > 0)
                {
                    logger.LogInformation($"FOUND {itemsToImport.EbayIDs.Count.ToString()} items to import into TreeCat(gmail) account.");

                    // Not sure if we need to check if the accessToken is valid like in "ImportEbay"
                    // because we already do this when we list the ebay items.

                    // Get Ebay token
                    var tokenGetter = tokenGetters.EbayAccess;

                    // Get the user's EBAY items from cache
                    var ebayCache = new BifrostCache(bifrostURL, "ebay-items", authToken, logger);
                    var userEbayCachedItems = new lib.listers.EbayLister(ebayCache, this.logger, 10000, tokenGetter).List().Result;

                    //Set the user's TREECAT items in cache, returns the cached items.
                    List<string> treecatIDs = new lib.listers.EbayLister(treecatServiceCache, this.logger, 10000, tokenGetter).ImportTreecatIDs(itemsToImport, userEbayCachedItems).Result;

                    logger.LogInformation($"SUCCESSFULLY imported {treecatIDs.Count.ToString()} items into TreeCat.");

                    return Redirect("importebay");
                }
            } 
            catch (Exception e)
            {
                logger.LogInformation(e.ToString());
            }

            // Poshmark try/catch
            try
            {
                if (itemsToImport.PoshmarkIDs.Count > 0)
                {
                    logger.LogInformation($"FOUND {itemsToImport.PoshmarkIDs.Count.ToString()} items to import into TreeCat(gmail) account.");

                    // Get Poshmark token
                    var tokenGetter = tokenGetters.Poshmark;

                    // Get the user's POSHMARK items from cache
                    var poshmarkCache = new BifrostCache(bifrostURL, "poshmark-items", authToken, logger);
                    List<Item> userPoshmarkCachedItems = new lib.listers.PoshmarkLister(poshmarkCache, this.logger, 10000, tokenGetter).List().Result;

                    // Set the user's TREECAT IDs in cache
                    List<string> treecatItems = new lib.listers.PoshmarkLister(treecatServiceCache, this.logger, 10000, tokenGetter).ImportTreecatIDs(itemsToImport, userPoshmarkCachedItems).Result; // Returns the IDs imported into the user's TREECAT(GMAIL) account database

                    logger.LogInformation($"SUCCESSFULLY imported {treecatItems.Count.ToString()} items into TreeCat.");

                    return Redirect("importposhmark");
                }
            }
            catch (Exception e)
            {
                logger.LogInformation(e.ToString());
            }

            return View("importdata");
        }

        public async Task<IActionResult> Inventory()
        {
            // This function returns all the items from the user's TreeCat cache
            // First, we need to access the TreeCat cache and take the "treecat_list" key with all the IDs of the unique items
            // Get "treecat_list" from cache into list
            string bifrostURL = this.configuration["Bifrost:Service"];
            var authToken = await this.tokenGetters.Google.GetToken(); // Get Google auth token\
            var treecatServiceCache = new BifrostCache(bifrostURL, "treecat-items", authToken, logger); // Connect to the TreeCat service cache

            var treecatByteItems = await treecatServiceCache.GetAsync("treecat_list");

            List<Item> inventory = new List<Item>(); // Items to diplay on the Inventory page 
            if (treecatByteItems != null)
            {
                // Treecat list with IDs
                List<string> treecatIDsList = JsonConvert.DeserializeObject<List<string>>(
                    ASCIIEncoding.UTF8.GetString(treecatByteItems));

                // Get each individual TreeCat ID from the list
                foreach (var treecatListedID in treecatIDsList)
                {
                    // Get the Item corresponding to each individual ID
                    var treecatByteItem = await treecatServiceCache.GetAsync(treecatListedID);
                    if (treecatByteItem != null)
                    {
                        Item treecatItem = JsonConvert.DeserializeObject<Item>(
                            ASCIIEncoding.UTF8.GetString(treecatByteItem));

                        inventory.Add(treecatItem); // Add Item to display
                    }
                }
            }

            ViewBag.Items = inventory;

            return View();
        }

        public IActionResult Welcome()
        {
            return View();
        }

        public IActionResult Setup()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            this.ViewBag.Path = exceptionHandlerPathFeature.Path;
            this.ViewBag.Error = exceptionHandlerPathFeature.Error;

            return View();
        }
    }
}
