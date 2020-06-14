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
        string token = "v^1.1#i^1#p^3#r^0#f^0#I^3#t^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";

        [Fact]
        public void List()
        {
            var logger = TestLogger.NewLogger("ebay-categories-tests");
            var cache = new DiskCache(@"D:\code\bifrost\data\ebay\item-cache", logger);

            var tokenGetter = new EbayHardcodedTokenGetter();
            tokenGetter.Set(token);
            var items = new lib.listers.EbayLister(cache, logger, 10000, tokenGetter).List().Result;

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
