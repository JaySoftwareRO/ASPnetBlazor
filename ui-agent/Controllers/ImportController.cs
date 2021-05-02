using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using lib.cache.bifrost;
using lib;
using lib.token_getters;
using lib.providers.ebay;

namespace ui_agent.Controllers
{
    public class ImportController : Controller
    {
        private readonly ILogger<ItemController> logger;
        private readonly IConfiguration configuration;
        private readonly ITokenGetters tokenGetters;

        public ImportController(ILogger<ItemController> logger, IConfiguration configuration, ITokenGetters tokenGetters)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.tokenGetters = tokenGetters;
        }

        [GoogleAuth, PoshmarkAuth, EBayAuth]
        public IActionResult List(bool ebay, bool poshmark)
        {
            this.ViewBag.Poshmark = ebay;
            this.ViewBag.EBay = poshmark;
            this.ViewBag.Errors = new List<string>();
            var items = new List<Item>();

            if (poshmark)
            {
                var cache = new BifrostCache(this.configuration, "poshmark-items", this.tokenGetters.Google, logger);
                try
                {
                    // TODO: live call limits should not be hardcoded
                    items = new lib.listers.PoshmarkLister(cache, this.logger, 10000, this.tokenGetters.Poshmark).List().Result;
                }
                catch (Exception)
                {
                    this.ViewBag.Errors.Add("Please add items to your Poshmark inventory.");
                }
            }

            if (ebay)
            {
                var cache = new BifrostCache(this.configuration, "ebay-items", this.tokenGetters.Google, logger);
                var ebayItems = new EbayProvider(this.logger, this.tokenGetters).List().Result;
                items.AddRange(ebayItems);
            }

            return View("list", items);
        }
    }
}
