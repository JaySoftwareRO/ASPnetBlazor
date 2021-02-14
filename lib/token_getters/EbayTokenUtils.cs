using eBay.ApiClient.Auth.OAuth2;
using eBay.ApiClient.Auth.OAuth2.Model;
using ebayws;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace lib.token_getters
{
    public class EbayToken
    {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public string UserID { get; set; }
    }

    public class EbayTokenUtils
    {
        public static EbayToken TokenFromCode(string code, ILogger logger)
        {
            logger.LogDebug("loading ebay auth config");
            CredentialUtil.Load("ebay-auth-config.yaml", logger);
            OAuth2Api oAuth2Api = new OAuth2Api(logger);

            logger.LogInformation("getting ebay access token from code");
            var result = oAuth2Api.ExchangeCodeForAccessToken(OAuthEnvironment.PRODUCTION, code);
            
            return new EbayToken()
            {
                AccessToken = result.AccessToken.Token,
                RefreshToken = result.RefreshToken.Token,
                UserID = UserID(result.AccessToken.Token, logger)
            };
        }

        public static EbayToken AccessTokenFromRefreshToken(string refreshToken, List<string> scopes, ILogger logger)
        {
            logger.LogDebug("loading ebay auth config");
            CredentialUtil.Load("ebay-auth-config.yaml", logger);
            OAuth2Api oAuth2Api = new OAuth2Api(logger);

            logger.LogInformation("getting ebay access token from refresh token");
            var result = oAuth2Api.GetAccessToken(OAuthEnvironment.PRODUCTION, refreshToken, scopes);

            if (result.AccessToken == null)
            {
                return null; 
            }

            return new EbayToken()
            {
                AccessToken = result.AccessToken.Token,
                RefreshToken = string.Empty,
                UserID = UserID(result.AccessToken.Token, logger)
            };
        }

        public static string UserID(string accessToken, ILogger logger)
        {
            logger.LogInformation("getting ebay user id");

            // TODO: vladi: all of these should be configurable

            // Define the endpoint (e.g., the Sandbox Gateway URI)
            String endpoint = "https://api.ebay.com/wsapi";

            // Define the query string parameters.
            String queryString = "?callname=GetUser"
                                + "&siteid=0"
                                + "&appid=VladIova-Treecat-SBX-6bce464fb-92785135"
                                + "&version=1149"
                                + "&Routing=new";

            String requestURL = endpoint + queryString; // "https://api.ebay.com/wsapi";

            ebayws.eBayAPIInterfaceClient client = new ebayws.eBayAPIInterfaceClient();
            client.Endpoint.Address = new EndpointAddress(requestURL);

            using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
            {
                try
                {
                    var httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers["X-EBAY-API-IAF-TOKEN"] = accessToken;

                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                    var userInfo = client.GetUserAsync(null, new GetUserRequestType()
                    {
                        // TODO: should not be hardcoded
                        Version = "1149"
                    }).GetAwaiter().GetResult();

                    return userInfo.GetUserResponse1.User.Email;
                }
                catch (Exception ex)
                {
                    logger.LogError($"error getting ebay user info from token {ex.Message}");
                    throw ex;
                }
            }
        }
    }
}
