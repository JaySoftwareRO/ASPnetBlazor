using lib.cache;
using lib.cache.bifrost;
using lib.cache.disk;
using lib.token_getters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using Xunit;

namespace tests
{
    public class EbayListerTests
    {
        string token = "v^1.1#i^1#f^0#I^3#r^0#p^3#t^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";

        [Fact]
        public void List()
        {
            var logger = TestLogger.NewLogger("ebay-categories-tests");
            var cache = new BifrostCache(@"https://bifrost.app.asgardtech.io/", logger);

            var tokenGetter = new EbayHardcodedTokenGetter();
            //tokenGetter.Set(token);
            var items = new lib.listers.EbayLister(cache, logger, 10000, "tests", tokenGetter).List().Result;

            Assert.True(items.Count > 0);
        }
    }
}
