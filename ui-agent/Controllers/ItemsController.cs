using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using lib;
using lib.cache;
using lib.cache.postgresql;
using lib.token_getters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ui_agent.Models;

namespace ui_agent.Controllers
{
    public class ItemsController : Controller
    {
        private readonly ILogger<DataController> logger;
        private readonly IConfiguration configuration;

        public ItemsController(ILogger<DataController> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public IActionResult EbayListings()
        {
            string token = configuration.GetValue<string>("Authentication:EBay:Token");
            var connectionString = configuration.GetValue<string>("Data:PSQLCache:ConnectionString");

            var schemaName = "bifrost";
            var tableName = "ebay_items";
            var createInfrastructure = true;

            var cache = new PostgreSqlCache(new PostgreSqlCacheOptions()
            {
                ConnectionString = connectionString,
                SchemaName = schemaName,
                TableName = tableName,
                CreateInfrastructure = createInfrastructure,
            });

            cache.Set("init",
                Encoding.UTF8.GetBytes(DateTime.UtcNow.ToLongDateString()),
                new DistributedCacheEntryOptions().SetAbsoluteExpiration(DateTimeOffset.UtcNow.AddDays(700)));

            var tokenGetter = new EbayHardcodedTokenGetter();
            tokenGetter.Set(token);
            this.ViewBag.Items = new lib.listers.EbayLister(cache, this.logger, 10000, "localAccount", tokenGetter).List().Result;

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
