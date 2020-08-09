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
using System.Reflection.Metadata.Ecma335;
using Xunit;

namespace tests
{
    public class EbaySchemaReaderTests
    {
        string token = "v^1.1#i^1#I^3#p^3#r^0#f^0#t^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";

        [Fact]
        public void CategoryTree()
        {
            var logger = TestLogger.NewLogger("ebay-categories-tests");
            var cache = new BifrostCache(@"https://127.0.0.1:5001", logger);

            var tokenGetter = new EbayHardcodedTokenGetter();
            //tokenGetter.Set(token);
            var categories = new lib.schema_readers.EbaySchemaReader(cache, logger, 10000, tokenGetter).CategoryTree().Result;

            Assert.True(categories.SubCategories.Count > 0);
        }
    }
}
