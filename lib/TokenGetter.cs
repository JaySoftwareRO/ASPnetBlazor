using lib.token_getters;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib
{
    public interface ITokenGetter
    {
        string Get();

        string LoginURL();
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
        private readonly EbayHardcodedTokenGetter ebayTokenGetter = new EbayHardcodedTokenGetter();
        private readonly ITokenGetter poshmarkTokenGetter = new PoshmarkHardcodedTokenGetter();

        public ITokenGetter AmazonTokenGetter()
        {
            throw new NotImplementedException();
        }

        public ITokenGetter EBayTokenGetter()
        {
            return this.ebayTokenGetter;
        }

        public ITokenGetter MercariTokenGetter()
        {
            throw new NotImplementedException();
        }

        public ITokenGetter PoshmarkTokenGetter()
        {
            return this.poshmarkTokenGetter;
        }
    }
}
