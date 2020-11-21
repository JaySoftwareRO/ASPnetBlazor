﻿using Google.Apis.Auth;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib.token_getters
{

    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class GoogleAuthAttribute : ActionFilterAttribute
    {
        public async override void OnActionExecuting(ActionExecutingContext context)
        {
            var tokenGetters = (ITokenGetters)context.HttpContext.RequestServices.GetService(typeof(ITokenGetters));
            var authToken = await tokenGetters.Google.GetToken();

            if (string.IsNullOrWhiteSpace(authToken))
            {
                tokenGetters.Logger().LogDebug($"google auth token is empty");
                context.Result = new RedirectToActionResult("welcome", "item", new { welcomeMessage = "You have not logged in with Google!" });
                return;
            }

            try
            {
                var validPayload = GoogleJsonWebSignature.ValidateAsync(
                    authToken,
                    new GoogleJsonWebSignature.ValidationSettings { }).Result;
            }
            catch (Exception ex)
            {
                tokenGetters.Logger().LogDebug(ex, $"invalid google auth token");
                context.Result = new RedirectToActionResult("welcome", "item", new { welcomeMessage = "You have not logged in with Google!" });
                return;
            }

            // Validate the token with bifrost
        }
    }
}
