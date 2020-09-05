using eBay.ApiClient.Auth.OAuth2;
using eBay.ApiClient.Auth.OAuth2.Model;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Core.Tokens;

namespace lib.token_getters
{
    public class EbayToken
    {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }
    }

    public class EbayHardcodedTokenGetter : ITokenGetter
    {
        string accessToken;
        string refreshToken;

        public void Set(EbayToken token)
        {
            this.accessToken = token.AccessToken;
            this.refreshToken = token.RefreshToken;
        }

        public string Get()
        {
            return this.accessToken;
        }   

        public static EbayToken TokenFromCode(string code, ILogger logger)
        {
            CredentialUtil.Load("ebay-auth-config.yaml", logger);
            OAuth2Api oAuth2Api = new OAuth2Api(logger);

            var result = oAuth2Api.ExchangeCodeForAccessToken(OAuthEnvironment.PRODUCTION, code);

            return new EbayToken()
            {
                AccessToken = result.AccessToken.Token,
                RefreshToken = result.AccessToken.Token,
            };
        }

        public string LoginURL()
        {
            return "https://auth.ebay.com/oauth2/authorize?client_id=VladIova-Treecat-PRD-4bcdaddba-89642d26&response_type=code&redirect_uri=Vlad_Iovanov-VladIova-Treeca-vehtia&scope=https://api.ebay.com/oauth/api_scope https://api.ebay.com/oauth/api_scope/sell.marketing.readonly https://api.ebay.com/oauth/api_scope/sell.marketing https://api.ebay.com/oauth/api_scope/sell.inventory.readonly https://api.ebay.com/oauth/api_scope/sell.inventory https://api.ebay.com/oauth/api_scope/sell.account.readonly https://api.ebay.com/oauth/api_scope/sell.account https://api.ebay.com/oauth/api_scope/sell.fulfillment.readonly https://api.ebay.com/oauth/api_scope/sell.fulfillment https://api.ebay.com/oauth/api_scope/sell.analytics.readonly https://api.ebay.com/oauth/api_scope/sell.finances https://api.ebay.com/oauth/api_scope/sell.payment.dispute https://api.ebay.com/oauth/api_scope/commerce.identity.readonly";
        }

        public string GetUserID()
        {
            throw new NotImplementedException();
        }
    }
}
