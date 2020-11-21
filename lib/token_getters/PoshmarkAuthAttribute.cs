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
    public class PoshmarkAuthAttribute : ActionFilterAttribute
    {
        public async override void OnActionExecuting(ActionExecutingContext context)
        {
            var tokenGetters = (ITokenGetters)context.HttpContext.RequestServices.GetService(typeof(ITokenGetters));

            var tokenGetter = tokenGetters.Poshmark;
            var token = await tokenGetter.GetToken();

            if (string.IsNullOrWhiteSpace(token))
            {
                tokenGetters.Logger().LogDebug("poshmark token is empty - redirecting to login page");

                // TODO: redirect to a "not logged into" page
                context.Result = new RedirectToActionResult("welcome", "item", new { welcomeMessage = "You have not logged in with Poshmark!" });
                return;
            }

            // Verify the access token with Poshmark
        }
    }
}
