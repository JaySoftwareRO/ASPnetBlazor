using Google.Apis.Auth;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace lib.token_getters
{

    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class EBayAuthAttribute : ActionFilterAttribute
    {
        private readonly bool failOnError;

        public EBayAuthAttribute(bool failOnError = false)
        {
            this.failOnError = failOnError;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            this.OnActionExecutionAsync(context, null).Wait();
        }

        public async override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
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
                    if (!this.failOnError)
                    {
                        tokenGetters.Logger().LogDebug("ebay refresh token is empty - redirecting to login page");
                        context.Result = new RedirectToActionResult("welcome", "item", new { welcomeMessage = "You have not logged in with EBay!" });
                        return;
                    }

                    context.Result = new UnauthorizedResult();
                    return;
                }

                // Use the refresh token to get an access token
                var newAccessToken = EbayTokenUtils.AccessTokenFromRefreshToken(refreshToken, tokenGetter.Scopes(), tokenGetters.Logger());

                if (newAccessToken == null)
                {
                    tokenGetters.Logger().LogDebug("couldn't get an access token from the refresh token for ebay; invalidating refresh token and redirecting to login page");

                    if (!this.failOnError)
                    {
                        await tokenGetters.Ebay.Set("", "", "");
                        context.Result = new RedirectToActionResult("welcome", "item", new { welcomeMessage = "You have not logged in with EBay!" });
                        return;
                    }

                    context.Result = new UnauthorizedResult();
                    return;
                }

                tokenGetters.Logger().LogDebug("setting new ebay access token");
                await tokenGetters.Ebay.Set(newAccessToken.AccessToken, newAccessToken.UserID, newAccessToken.UserID);
            }

            // Verify the access token with EBay
            // ...

            await next();
        }
    }
}
