using lib.poshmark_client;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace tests
{
    public class PoshmarkTests
    {
        [Fact]
        public void List()
        {
            var PoshmarkClient = new PoshmarkClient();
           var foo = PoshmarkClient.List();

            Assert.Equal("/listing/Gray-Pants-5f01cf1f60fdedb0ffff8717", foo[0].ProductPageLink);
            Assert.Equal("Women", foo[0].Categories[0]);
            Assert.Equal("Jeans", foo[0].Categories[1]);
            Assert.Equal("Straight Leg", foo[0].Categories[2]);
            Assert.Equal("Gray Pants", foo[0].Title);
            Assert.Equal(100.0, foo[0].Price);
            Assert.False(foo[0].NotForSale);
            Assert.Equal("Size: 0", foo[0].Size);
            Assert.Equal("Signature by Levi Strauss", foo[0].Brand);
            Assert.Equal("/listing/Meet-your-Posher-Nora-5e98ceee92ea0f645a1a4114", foo[1].ProductPageLink);
            Assert.Equal("Women", foo[1].Categories[0]);
            Assert.Equal("Other", foo[1].Categories[1]);
            Assert.Equal("Meet your Posher, Nora", foo[1].Title);
            Assert.Equal(0.0, foo[1].Price);
            Assert.True(foo[1].NotForSale);
            Assert.Null(foo[1].Size);
            Assert.Equal("Meet the Posher", foo[1].Brand);
        }
    }
}
