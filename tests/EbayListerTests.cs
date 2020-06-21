using lib.cache;
using lib.token_getters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using Xunit;

namespace tests
{
    public class EbayListerTests
    {
        string token = "v^1.1#i^1#p^3#I^3#r^0#f^0#t^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";

        [Fact]
        public void List()
        {
            var logger = TestLogger.NewLogger("ebay-categories-tests");
            var cache = new DiskCache(@"D:\code\bifrost\data\ebay\item-cache", logger);

            var tokenGetter = new EbayHardcodedTokenGetter();
            tokenGetter.Set(token);
            var items = new lib.listers.EbayLister(cache, logger, 10000, "tests", tokenGetter).List().Result;

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamWriter sw = new StreamWriter(@"D:\code\bifrost\data\ebay\categories.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, items);
            }

            Assert.True(items.Count > 0);
        }
    }
}
