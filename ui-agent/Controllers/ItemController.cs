using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ui_agent.Models;
using lib.poshmark_client;

namespace ui_agent.Controllers
{
    public class ItemController : Controller
    {
        private readonly ILogger<DataController> _logger;

        public ItemController(ILogger<DataController> logger)
        {
            _logger = logger;
        }

        public IActionResult Add()
        {
            return View();
        }

        public IActionResult Edit()
        {
            return View();
        }

        public IActionResult FieldMapping()
        {
            return View();
        }

        public IActionResult FrameTest()
        {
            return View();
        }

        public IActionResult ImportData()
        {
            return View();
        }

        public IActionResult Inventory()
        {
            var items = new PoshmarkClient();
            this.ViewBag.Items = items.List();

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
