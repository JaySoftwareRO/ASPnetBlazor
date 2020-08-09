using eBay.ApiClient.Auth.OAuth2;
using eBay.ApiClient.Auth.OAuth2.Model;
using Google.Protobuf.WellKnownTypes;
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

        public static EbayToken TokenFromCode(string code)
        {
            CredentialUtil.Load("ebay-auth-config.yaml");
            OAuth2Api oAuth2Api = new OAuth2Api();

            var result = oAuth2Api.ExchangeCodeForAccessToken(OAuthEnvironment.PRODUCTION, code);

            return new EbayToken()
            {
                AccessToken = result.AccessToken.Token,
                RefreshToken = result.AccessToken.Token,
            };
        }
    }
}
