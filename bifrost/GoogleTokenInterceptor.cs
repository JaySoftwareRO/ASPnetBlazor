using Google.Apis.Auth;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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
            await AuthenticateCall(context);
            return await continuation(request, context);
        }

        public async override Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            LogCall(context);
            await AuthenticateCall(context);
            await continuation(request, responseStream, context);
        }
        public async override Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            LogCall(context);
            await AuthenticateCall(context);
            await continuation(requestStream, responseStream, context);
        }
        public async override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            LogCall(context);
            await AuthenticateCall(context);
            return await continuation(requestStream, context);
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
                context.RequestHeaders.Add("googleprofileid", accountID);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"invalid google auth token from {context.GetHttpContext().Connection.RemoteIpAddress} for {context.Method}");
                throw ex;
            }
        }
    }
}

