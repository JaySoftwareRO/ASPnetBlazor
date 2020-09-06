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

        public async Task<IActionResult> ImportPoshmark()
        {
            var tokenGetter = tokenGetters.PoshmarkTokenGetter();
            var userID = await tokenGetter.GetUserID();
            var token = await tokenGetter.GetToken();

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userID))
            {
                // TODO: redirect to a "not logged into" page
                return RedirectToAction("welcome", "item");
            }

            string bifrostURL = this.configuration["Bifrost:Service"];
            var cache = new BifrostCache(bifrostURL, "poshmark-items", logger);

            var items = new List<Item>();

            try
            {   if (userID != null)
                {
                    items = new lib.listers.PoshmarkLister(cache, this.logger, 10000, "ad-" + userID, tokenGetter).List().Result;
                }
                else
                {
                    this.ViewBag.EmptyInventoryMessage = "Please log in to see your Poshmark inventory.";
                }
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
            var tokenGetter = tokenGetters.EbayAccessTokenGetter();
            var accessToken = await tokenGetter.GetToken();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                var refreshTokenGetter = tokenGetters.EBayTokenGetter();
                var refreshToken = await refreshTokenGetter.GetToken();

                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    // TODO: redirect to a "not logged into" page
                    return RedirectToAction("welcome", "item");
                }

                // Use the refresh token to get an access token
                var newAccessToken = EbayTokenUtils.AccessTokenFromRefreshToken(refreshToken, tokenGetter.Scopes(), logger);
                await this.tokenGetters.EbayAccessTokenGetter().Set(newAccessToken.AccessToken, string.Empty);
            }

            string bifrostURL = this.configuration["Bifrost:Service"];
            var cache = new BifrostCache(bifrostURL, "ebay-items", logger);

            //TODO: account should be ebay user's account, and the bifrost service has to authenticate the local account
            var items = new lib.listers.EbayLister(cache, this.logger, 10000, "localAccount", tokenGetter).List().Result;

            this.ViewBag.Selected = "ebay";
            return View("importdata", items);
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
