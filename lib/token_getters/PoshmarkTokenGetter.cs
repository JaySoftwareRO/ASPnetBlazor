using System;
using System.Collections.Generic;
using System.Text;

namespace lib.token_getters
{
    public class PoshmarkHardcodedTokenGetter : ITokenGetter
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

        public string LoginURL()
        {
            return "https://poshmark.com/login";
        }
    }
}
