using eBay.ApiClient.Auth.OAuth2;
using eBay.ApiClient.Auth.OAuth2.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib.token_getters
{
    public class EbayToken
    {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }
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
            };
        }

        public static EbayToken AccessTokenFromRefreshToken(string refreshToken, List<string> scopes, ILogger logger)
        {
            logger.LogDebug("loading ebay auth config");
            CredentialUtil.Load("ebay-auth-config.yaml", logger);
            OAuth2Api oAuth2Api = new OAuth2Api(logger);

            logger.LogInformation("getting ebay access token from refresh token");
            var result = oAuth2Api.GetAccessToken(OAuthEnvironment.PRODUCTION, refreshToken, scopes);

            return new EbayToken()
            {
                AccessToken = result.AccessToken.Token,
                RefreshToken = result.RefreshToken.Token,
            };
        }
    }
}
