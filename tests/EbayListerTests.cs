using System;
using Xunit;

namespace tests
{
    public class EbayListerTests
    {
        [Fact]
        public void List()
        {
            var categories = new lib.listers.EbayLister().Categories().Result;
            Console.WriteLine(categories.Count);
            Assert.True(categories.Count > 0);
        }
    }
}
