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

        public IActionResult InventoryEbay()
        {
            string bifrostURL = this.configuration["Bifrost:Service"];
            var cache = new BifrostCache(bifrostURL, "ebay-items", logger);
            var tokenGetter = this.tokenGetters.EBayTokenGetter();
            
            try
            {
                this.ViewBag.Items = new lib.listers.EbayLister(cache, this.logger, 10000, "fake-account", tokenGetter).List().Result;
            }
            catch (Exception)
            {
                this.ViewBag.Items = "";
                this.ViewBag.EmptyInventoryMessage = "Please add some items to your eBay inventory.";
            }

            return View("importdata");
        }

        public IActionResult InventoryPoshmark()
        {
            var token = tokenGetters.PoshmarkTokenGetter();
            var userID = token.GetUserID();

            string bifrostURL = this.configuration["Bifrost:Service"];
            var cache = new BifrostCache(bifrostURL, "poshmark-items", logger);

            try
            {   if (userID != null)
                {
                    this.ViewBag.Items = new lib.listers.PoshmarkLister(cache, this.logger, 10000, "ad-" + userID, token).List().Result;
                }
                else
                {
                    this.ViewBag.Items = string.Empty;
                    this.ViewBag.EmptyInventoryMessage = "Please log in to see your Poshmark inventory.";
                }
            }
            catch (Exception)
            {
                this.ViewBag.EmptyInventoryMessage = "Please add items to your Poshmark inventory.";
            }
            return View("importdata");
        }

        public IActionResult ImportData()
        {
            if (this.ViewBag.Items == null)
            {
                this.ViewBag.Items = string.Empty;
            }

            this.ViewBag.EmptyInventoryMessage = "Choose a Source Platform from the dropdown list.";

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
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
