using System;
using System.Collections.Generic;
using System.Text;

namespace lib
{
    public interface ITokenGetter
    {
        string Get();
    }

    public interface ITokenGetters
    {
        ITokenGetter EBayTokenGetter();
        ITokenGetter MercariTokenGetter();
        ITokenGetter PoshmarkTokenGetter();
        ITokenGetter AmazonTokenGetter();

    }

    public class HardcodedTokenGetters : ITokenGetters
    {
        public ITokenGetter AmazonTokenGetter()
        {
            throw new NotImplementedException();
        }

        public ITokenGetter EBayTokenGetter()
        {
            var tokenGetter=  new token_getters.EbayHardcodedTokenGetter();
            tokenGetter.Set("");
            return tokenGetter;
        }

        public ITokenGetter MercariTokenGetter()
        {
            throw new NotImplementedException();
        }

        public ITokenGetter PoshmarkTokenGetter()
        {
            throw new NotImplementedException();
        }
    }
}
