using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ui_agent.Models;
using lib.poshmark_client;
using lib;
using YamlDotNet.Core.Tokens;
using lib.token_getters;

namespace ui_agent.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> logger;
        private readonly ITokenGetters tokenGetters;

        public AuthController(ILogger<AuthController> logger, ITokenGetters tokenGetters)
        {
            this.logger = logger;
            this.tokenGetters = tokenGetters;
        }

        public IActionResult Declined()
        {
            return View();
        }

        public IActionResult Accept(string code)
        {
            var tokens = EbayHardcodedTokenGetter.TokenFromCode(code, this.logger);
            ((EbayHardcodedTokenGetter)tokenGetters.EBayTokenGetter()).Set(tokens);

            return RedirectToAction("ebayListings", "items");
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
