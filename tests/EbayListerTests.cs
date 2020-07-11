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
        string token = "v^1.1#i^1#I^3#p^3#r^0#f^0#t^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";

        [Fact]
        public void List()
        {
            var logger = TestLogger.NewLogger("ebay-categories-tests");
            var cache = new BifrostCache(@"https://localhost:5001", logger);

            var tokenGetter = new EbayHardcodedTokenGetter();
            tokenGetter.Set(token);
            var items = new lib.listers.EbayLister(cache, logger, 10000, "tests", tokenGetter).List().Result;

            Assert.True(items.Count > 0);
        }
    }
}
