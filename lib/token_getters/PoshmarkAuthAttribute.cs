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
    public class PoshmarkAuthAttribute : ActionFilterAttribute
    {
        private readonly bool failOnError;

        public PoshmarkAuthAttribute(bool failOnError = false)
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

            var tokenGetter = tokenGetters.Poshmark;
            var token = await tokenGetter.GetToken();

            if (string.IsNullOrWhiteSpace(token))
            {
                tokenGetters.Logger().LogDebug("poshmark token is empty - redirecting to login page");

                if (!this.failOnError)
                {
                    context.Result = new RedirectToActionResult("welcome", "item", new { welcomeMessage = "You have not logged in with Poshmark!" });
                    return;
                }

                context.Result = new UnauthorizedResult();
                return;
            }

            // Verify the access token with Poshmark
            // ...

            await next();
        }
    }
}
