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
    public class GoogleAuthAttribute : ActionFilterAttribute
    {
        private readonly bool failOnError;

        public GoogleAuthAttribute(bool failOnError = false)
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
            var authToken = await tokenGetters.Google.GetToken();

            if (string.IsNullOrWhiteSpace(authToken))
            {
                if (!this.failOnError) {
                    tokenGetters.Logger().LogDebug($"google auth token is empty");
                    context.Result = new RedirectToActionResult("welcome", "item", new { welcomeMessage = "You have not logged in with Google!" });
                    return;
                }

                context.Result = new UnauthorizedResult();
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
                if (!this.failOnError)
                {
                    tokenGetters.Logger().LogDebug(ex, $"invalid google auth token");
                    context.Result = new RedirectToActionResult("welcome", "item", new { welcomeMessage = "You have not logged in with Google!" });
                    return;
                }

                context.Result = new UnauthorizedResult();
                return;
            }

            // Validate the token with bifrost
            // ...

            await next();
        }
    }
}
