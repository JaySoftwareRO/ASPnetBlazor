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

        [GoogleAuth, PoshmarkAuth]
        public IActionResult ImportPoshmark()
        {
            var cache = new BifrostCache(this.configuration, "poshmark-items", this.tokenGetters.Google, logger);

            var items = new List<Item>();
            try
            {   
                // TODO: live call limits should not be hardcoded
                items = new lib.listers.PoshmarkLister(cache, this.logger, 10000, this.tokenGetters.Poshmark).List().Result;
            }
            catch (Exception)
            {
                this.ViewBag.EmptyInventoryMessage = "Please add items to your Poshmark inventory.";
            }

            this.ViewBag.Selected = "poshmark";
            return View("importdata", items);
        }

        [GoogleAuth, EBayAuth]
        public IActionResult ImportEbay()
        {
            var cache = new BifrostCache(this.configuration, "ebay-items", this.tokenGetters.Google, logger);
            var items = new lib.listers.EbayLister(cache, this.logger, 10000, this.tokenGetters.EbayAccess).List().Result;

            this.ViewBag.Selected = "ebay";
            return View("importdata", items);
        }

        [HttpPost, GoogleAuth, EBayAuth]
        public IActionResult ImportData([FromBody] ItemImportModel itemsToImport)
        {
            var treecatServiceCache = new BifrostCache(configuration, "treecat-items", this.tokenGetters.Google, logger); // Connect to the TreeCat service cache

            // Check which field has the more than 0 IDs stored and return view to the appropriate action
            // Ebay try/catch
            try
            {
                if (itemsToImport.EbayIDs != null && itemsToImport.EbayIDs.Count > 0)
                {
                    logger.LogInformation($"FOUND {itemsToImport.EbayIDs.Count} items to import into TreeCat(gmail) account.");

                    // Get the user's EBAY items from cache
                    var ebayCache = new BifrostCache(this.configuration, "ebay-items", this.tokenGetters.Google, logger);
                    var userEbayCachedItems = new lib.listers.EbayLister(ebayCache, this.logger, 10000, this.tokenGetters.EbayAccess).List().Result;

                    //Set the user's TREECAT items in cache, returns the cached items.
                    List<string> treecatIDs = new lib.listers.EbayLister(treecatServiceCache, this.logger, 10000, this.tokenGetters.EbayAccess).ImportTreecatIDs(itemsToImport, userEbayCachedItems).Result;

                    logger.LogInformation($"SUCCESSFULLY imported {treecatIDs.Count} items into TreeCat.");

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
                if (itemsToImport.PoshmarkIDs != null && itemsToImport.PoshmarkIDs.Count > 0)
                {
                    logger.LogInformation($"FOUND {itemsToImport.PoshmarkIDs.Count} items to import into TreeCat(gmail) account.");

                    // Get the user's POSHMARK items from cache
                    var poshmarkCache = new BifrostCache(this.configuration, "poshmark-items", this.tokenGetters.Google, logger);
                    List<Item> userPoshmarkCachedItems = new lib.listers.PoshmarkLister(poshmarkCache, this.logger, 10000, this.tokenGetters.Poshmark).List().Result;

                    // Set the user's TREECAT IDs in cache
                    List<string> treecatItems = new lib.listers.PoshmarkLister(treecatServiceCache, this.logger, 10000, this.tokenGetters.Poshmark).ImportTreecatIDs(itemsToImport, userPoshmarkCachedItems).Result; // Returns the IDs imported into the user's TREECAT(GMAIL) account database

                    logger.LogInformation($"SUCCESSFULLY imported {treecatItems.Count} items into TreeCat.");

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
            var treecatServiceCache = new BifrostCache(this.configuration, "treecat-items", this.tokenGetters.Google, logger); // Connect to the TreeCat service cache

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

            return View("inventory", inventory);
        }

        [HttpPost]
        public IActionResult ImportToPoshmark([FromBody] ItemImportModel itemsToImport)
        {
            logger.LogInformation("Importing " + itemsToImport.PoshmarkIDs.Count.ToString() + " items to Poshmark.");

            return Redirect("inventory");
        }

        [HttpPost]
        public async Task<IActionResult> ImportToEbayAsync([FromBody] ItemImportModel itemsToImport)
        {
            logger.LogInformation("Importing " + itemsToImport.EbayIDs.Count.ToString() + " items to eBay.");

            // Get entire Item from cache
            // First, we need to access the TreeCat cache and take the "treecat_list" key with all the IDs of the unique items
            // Get "treecat_list" from cache into list
            var treecatServiceCache = new BifrostCache(this.configuration, "treecat-items", this.tokenGetters.Google, logger); // Connect to the TreeCat service cache

            var treecatByteItems = await treecatServiceCache.GetAsync("treecat_list");

            List<Item> treecatItems = new List<Item>(); // Items to diplay on the Inventory page 
            List<Item> treecatToEbayItems = new List<Item>(); // Items to import into Ebay

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

                        treecatItems.Add(treecatItem); // Add Item to display

                        // If the there is a match for an item the user has selected, import it to ebay 
                        if (itemsToImport.PoshmarkIDs.Contains(treecatItem.ID))
                        {
                            // Create the ebayitem with all the values from the TreeCat Item objects
                            ebayws.VerifyAddItemRequest ebayItem = new ebayws.VerifyAddItemRequest();

                            ebayItem.VerifyAddItemRequest1.Item.Title = treecatItem.Title;
                            ebayItem.VerifyAddItemRequest1.Item.Description = treecatItem.Description;
                            ebayItem.VerifyAddItemRequest1.Item.ConditionDescription = treecatItem.ConditionDescription;
                            ebayItem.VerifyAddItemRequest1.Item.PrimaryCategory.CategoryName = treecatItem.PrimaryCategory.CategoryName;
                            ebayItem.VerifyAddItemRequest1.Item.SecondaryCategory.CategoryName = treecatItem.SecondaryCategory.CategoryName;
                            ebayItem.VerifyAddItemRequest1.Item.FreeAddedCategory.CategoryName = treecatItem.FreeAddedCategory.CategoryName;
                            ebayItem.VerifyAddItemRequest1.Item.Site = ebayws.SiteCodeType.US;
                        }
                        else if (itemsToImport.EbayIDs != null)
                        {
                            // Error message "You can't import ebay items to ebay!"
                        }
                    }
                }
            }
            return Redirect("inventory");
        }

        public IActionResult Welcome(string welcomeMessage)
        {
            ViewBag.WelcomeMessage = welcomeMessage;
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
