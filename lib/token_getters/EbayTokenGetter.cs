using System;
using System.Collections.Generic;
using System.Text;

namespace lib.token_getters
{
    public class EbayHardcodedTokenGetter : ITokenGetter
    {
        string token;

        public void Set(string token)
        {
            this.token = token;
        }

        public string Get()
        {
            return token;
        }
    }
}
