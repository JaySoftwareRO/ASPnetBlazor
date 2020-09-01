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
using System.IdentityModel.Tokens.Jwt;

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

        public IActionResult EbayDeclined()
        {
            return View();
        }

        public IActionResult EbayAccept(string code)
        {
            var token = EbayHardcodedTokenGetter.TokenFromCode(code, this.logger);
            ((EbayHardcodedTokenGetter)tokenGetters.EBayTokenGetter()).Set(token);

            return RedirectToAction("welcome", "item");
        }

        public IActionResult PoshmarkAccept(string cookie)
        {
            var jwt = cookie;
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);


            var accessToken = token.Payload.FirstOrDefault(p => p.Key == "access_token");
            if (!string.IsNullOrWhiteSpace(accessToken.Key))
            {
                ((PoshmarkHardcodedTokenGetter)tokenGetters.PoshmarkTokenGetter()).Set(accessToken.Value as string);

                return RedirectToAction("welcome", "item");
            }
            else
            {
                // TODO: redirect to a better error page
                this.logger.LogError("getting poshmark access token failed");
                throw new Exception("poshmark token fail");
            }
        }

        public IActionResult EbayPrivacy()
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
