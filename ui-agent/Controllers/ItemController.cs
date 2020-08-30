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

        public IActionResult Inventory()
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
                // Dirty implementation, should find a better way to achieve this.
                this.ViewBag.Items = "";
                this.ViewBag.EmptyInventoryMessage = "Please add some items to your eBay inventory.";
            }

            return View();
        }

        public IActionResult InventoryPoshmark()
        {
            string bifrostURL = this.configuration["Bifrost:Service"];
            var cache = new BifrostCache(bifrostURL, "poshmark-items", logger);

            try
            {
                this.ViewBag.Items = new lib.listers.PoshmarkLister(cache, this.logger, 10000, "fake-ckingsings").List().Result; 
            }
            catch (Exception)
            {
                // Dirty implementation, should find a better way to achieve this.
                this.ViewBag.Items = "";
                this.ViewBag.EmptyInventoryMessage = "Please add some items to your Poshmark inventory.";
            }

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
