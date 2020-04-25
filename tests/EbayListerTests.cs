using System;
using Xunit;

namespace tests
{
    public class EbayListerTests
    {
        [Fact]
        public void List()
        {
            string body = new lib.listers.EbayLister().Categories();
            Console.WriteLine(body);
        }
    }
}
