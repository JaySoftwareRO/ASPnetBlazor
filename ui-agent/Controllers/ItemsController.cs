using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using lib;
using lib.cache;
using lib.token_getters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ui_agent.Models;

namespace ui_agent.Controllers
{
    public class ItemsController : Controller
    {
        private readonly ILogger<DataController> logger;

        public ItemsController(ILogger<DataController> logger)
        {
            this.logger = logger;
        }

        public IActionResult EbayListings()
        {
            string token = "v^1.1#i^1#f^0#I^3#r^0#p^3#t^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";

            var cache = new DiskCache(@"D:\code\bifrost\data\ebay\item-cache", this.logger);

            var tokenGetter = new EbayHardcodedTokenGetter();
            tokenGetter.Set(token);
            this.ViewBag.Items = new lib.listers.EbayLister(cache, this.logger, 10000, tokenGetter).List().Result;

            return View();
        }
        public IActionResult Welcome()
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
