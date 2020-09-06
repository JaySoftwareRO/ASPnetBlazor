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
            CredentialUtil.Load("ebay-auth-config.yaml", logger);
            OAuth2Api oAuth2Api = new OAuth2Api(logger);

            var result = oAuth2Api.ExchangeCodeForAccessToken(OAuthEnvironment.PRODUCTION, code);
            
            return new EbayToken()
            {
                AccessToken = result.AccessToken.Token,
                RefreshToken = result.RefreshToken.Token,                
            };
        }
    }
}
