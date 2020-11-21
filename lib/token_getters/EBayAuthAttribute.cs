using Google.Apis.Auth;
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
    public class EBayAuthAttribute : ActionFilterAttribute
    {
        public async override void OnActionExecuting(ActionExecutingContext context)
        {
            var tokenGetters = (ITokenGetters)context.HttpContext.RequestServices.GetService(typeof(ITokenGetters));

            var tokenGetter = tokenGetters.EbayAccess;
            var accessToken = await tokenGetter.GetToken();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                tokenGetters.Logger().LogDebug("ebay access token is empty - will try to use the refresh token to get another");

                var refreshTokenGetter = tokenGetters.Ebay;
                var refreshToken = await refreshTokenGetter.GetToken();

                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    tokenGetters.Logger().LogDebug("ebay refresh token is empty - redirecting to login page");
                    context.Result = new RedirectToActionResult("welcome", "item", new { welcomeMessage = "You have not logged in with EBay!" });
                    return;
                }

                // Use the refresh token to get an access token
                var newAccessToken = EbayTokenUtils.AccessTokenFromRefreshToken(refreshToken, tokenGetter.Scopes(), tokenGetters.Logger());

                if (newAccessToken == null)
                {
                    tokenGetters.Logger().LogDebug("couldn't get an access token from the refresh token for ebay; invalidating refresh token and redirecting to login page");

                    await tokenGetters.Ebay.Set("", "");
                    context.Result = new RedirectToActionResult("welcome", "item", new { welcomeMessage = "You have not logged in with EBay!" });
                    return;
                }

                tokenGetters.Logger().LogDebug("setting new ebay access token");
                await tokenGetters.Ebay.Set(newAccessToken.AccessToken, newAccessToken.UserID);
            }

            // Verify the access token with EBay
        }
    }
}
