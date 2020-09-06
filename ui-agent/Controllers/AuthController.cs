﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ui_agent.Models;
using lib;
using lib.cache.disk;
using YamlDotNet.Core.Tokens;
using lib.token_getters;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Caching.Distributed;

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

        public async Task<IActionResult> EbayAccept(string code)
        {
            var token = EbayTokenUtils.TokenFromCode(code, this.logger);
            await this.tokenGetters.EBayTokenGetter().Set(token.RefreshToken, string.Empty);
            await this.tokenGetters.EbayAccessTokenGetter().Set(token.AccessToken, string.Empty);

            return RedirectToAction("welcome", "item");
        }

        public async Task<IActionResult> PoshmarkAcceptAsync(string cookie)
        {
            // Create a JSON token from the cookie
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(cookie);

            // Get access_token and user_id from cookie token
            var accessToken = token.Payload.FirstOrDefault(p => p.Key == "access_token");
            var userID = token.Payload.FirstOrDefault(p => p.Key == "user_id");

            if (!string.IsNullOrWhiteSpace(accessToken.Key))
            {
                await this.tokenGetters.PoshmarkTokenGetter().Set(accessToken.Value as string, userID.Value as string);

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
