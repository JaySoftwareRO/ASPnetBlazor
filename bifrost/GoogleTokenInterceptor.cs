using Google.Apis.Auth;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib.bifrost
{
    public class GoogleTokenInterceptor : Interceptor
    {
        private readonly ILogger<GoogleTokenInterceptor> logger;
        private readonly IConfiguration configuration;

        public GoogleTokenInterceptor(ILogger<GoogleTokenInterceptor> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public async override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            LogCall(context);
            return await continuation(request, context);
        }

        private void LogCall(ServerCallContext context)
        {
            this.logger.LogDebug($"authenticating call {context.Method}");
        }

        private async Task AuthenticateCall(ServerCallContext context)
        {
            // Get the token from the http headers
            var token = context.GetHttpContext().Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(token))
            {
                this.logger.LogWarning($"call from {context.GetHttpContext().Connection.RemoteIpAddress} for {context.Method} did not contain an authorization header");
            }

            // Validate google's token
            try
            {
                var validPayload = await GoogleJsonWebSignature.ValidateAsync(
                    token,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new string[] { this.configuration["Authentication:GoogleAppClientID"] }
                    });

                var accountID = validPayload.Subject;
                context.RequestHeaders.Add("googleProfileID", accountID);
            } 
            catch (Exception ex)
            {
                this.logger.LogWarning($"invalid google auth token from {context.GetHttpContext().Connection.RemoteIpAddress} for {context.Method}");
                throw ex;
            }
        }
    }
}

