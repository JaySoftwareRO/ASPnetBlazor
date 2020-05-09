using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ui_agent.Models;

namespace ui_agent.Controllers
{
    public class DataController : Controller
    {
        private readonly ILogger<DataController> _logger;

        public DataController(ILogger<DataController> logger)
        {
            _logger = logger;
        }

        public IActionResult Mapping()
        {
            lib.listers.EbayLister ebayLister = new lib.listers.EbayLister();
            var allCategories = ebayLister.CategoryTree().Result;

            this.ViewBag.Categories =  new CategoryModel[] { 
                new CategoryModel() { Name = "foo" } 
            };

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
