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
        [Fact]
        public void CategoryTree()
        {
            //var config = Config.InitConfiguration();
            //var logger = TestLogger.NewLogger("ebay-categories-tests");
            //string token = config["EbaySandboxTestToken"];
            //var serviceURL = config["BifrostService"];

            //var cache = new BifrostCache(serviceURL, "ebay-schema", logger);

            //var tokenGetter = new EbayHardcodedTokenGetter();
            //tokenGetter.Set(new EbayToken() { AccessToken = token });

            //var categories = new lib.schema_readers.EbaySchemaReader(cache, logger, 10000, tokenGetter).CategoryTree().Result;

            //Assert.True(categories.SubCategories.Count > 0);
        }
    }
}
