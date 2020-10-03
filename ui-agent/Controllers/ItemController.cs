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
            // Check which field has the more than 0 IDs stored and return view to the appropriate action
            // Ebay try/catch
            try
            {
                if (itemsToImport.EbayIDs.Count > 0)
                {
                    logger.LogInformation(itemsToImport.EbayIDs.Count.ToString());
                    return Redirect("importebay");
                }
            } 
            catch (NullReferenceException e)
            {
                logger.LogInformation(e.ToString());
            }

            // Poshmark try/catch
            try
            {
                if (itemsToImport.PoshmarkIDs.Count > 0)
                {
                    logger.LogInformation($"FOUND {itemsToImport.PoshmarkIDs.Count.ToString()} items to import into TreeCat(gmail) account.");

                    string bifrostURL = this.configuration["Bifrost:Service"];
                    var authToken = await this.tokenGetters.Google.GetToken();
                    var tokenGetter = tokenGetters.Poshmark;

                    // Get the user's items from cache
                    var cache = new BifrostCache(bifrostURL, "poshmark-items", authToken, logger);
                    List<Item> usersTreecatCachedItems = new lib.listers.PoshmarkLister(cache, this.logger, 10000, tokenGetter).List().Result;

                    var googleUserID = await this.tokenGetters.Google.GetUserID(); // Get user gmail ID
                    var treecatServiceCache = new BifrostCache(bifrostURL, "poshmark-treecat-items", authToken, logger); // Connect to the TreeCat service cache
                    List<Item> treecatItems = new lib.listers.PoshmarkLister(treecatServiceCache, this.logger, 10000, tokenGetter).ListTreecatItems(itemsToImport, googleUserID, usersTreecatCachedItems).Result; // Returns the items located into the user's TREECAT(GMAIL) account database

                    logger.LogInformation($"SUCCESSFULLY imported {treecatItems.Count.ToString()} items into TreeCat.");

                    return Redirect("importposhmark");
                }
            }
            catch (NullReferenceException e)
            {
                logger.LogInformation(e.ToString());
            }

            return View("importdata");
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
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
