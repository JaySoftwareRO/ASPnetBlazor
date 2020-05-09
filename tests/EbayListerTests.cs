using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using Xunit;

namespace tests
{
    public class EbayListerTests
    {
        [Fact]
        public void List()
        {
            var categories = new lib.listers.EbayLister().CategoryTree().Result;

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamWriter sw = new StreamWriter(@"d:\ebay.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, categories);
            }

            Assert.True(categories.SubCategories.Count > 0);
        }
    }
}
