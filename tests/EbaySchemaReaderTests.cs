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
    public class EbaySchemaReaderTests
    {
        string token = "v^1.1#i^1#p^1#r^0#I^3#f^0#t^H4sIAAAAAAAAAOVYfWwURRTv9cvUUhEDQtpKzgUELLs3e9v72vTOXL/kgF4LV2qpwTq3O9tuu7d72ZmlPYlJrQT9BxHFmBgxxYRg/AgmkECNJEYwGBI1fhAjCFFiDEGJJESBYKKz21KulUChhzbx/tnMzHtv3vv93ps3N2CguOThzcs3Xyxz3ZU/NAAG8l0uvhSUFBdV3VOQX16UB7IEXEMDCwcKBwvO1GCY0tLiGoTTho6Ruz+l6Vh0JsOMZeqiAbGKRR2mEBaJJCaiTatELwfEtGkQQzI0xh2rDzOBkBQEAX9AERD0AhnQWf2qzVYjzAT5pFfwBmBISPqCEuTpOsYWiumYQJ2EGS/wAhb4WD7YCkKi4BNBgBMEXwfjbkMmVg2dinCAiTjuio6umeXrjV2FGCOTUCNMJBZtTDRHY/UN8dYaT5atyCgOCQKJhceP6gwZudugZqEbb4MdaTFhSRLCmPFERnYYb1SMXnXmNtx3oPYiiECQV6p5BQEF5QbKRsNMQXJjP+wZVWYVR1REOlFJ5maIUjSSPUgio6M4NRGrd9uf1RbUVEVFZphpqI2ui7a0MJE2DcoxYwNkW02EJEjYRG07609KqNpfrSTZkDcQ9PGCb3SfEWOjKE/YqM7QZdXGDLvjBqlF1Gk0ERpvFjRUqFlvNqMKsR3KkuP5MQj5DpvTERIt0q3btKIUxcHtDG9OwJg2IaaatAgaszBxwUEozMB0WpWZiYtOKo5mTz8OM92EpEWPp6+vj+sTOMPs8ngB4D3tTasSUjdKQcaWtWvdkVdvrsCqTigSoppYFUkmTX3pp6lKHdC7mIhQLYSE0Cju492KTJz9x0RWzJ7xBZGrAqlWApIQBFCh6SIBAeaiQCKjOeqx/UBJmGFT0OxFJK1BCbESzTMrhUxVpuYUrxBUECv7QwpbHVIUNumT/SwtVgQQSialUPB/VCeTzfQEkkxEcpPquUpzf2/c722rbYp70s1qkwof9ZuxUHtKrpWC/R0BfXl9FVqZytTF/Q1d4ckWw/WDl4w0ajE0VcrkBAG71nOGgmDKLdAkmVorQ8cJpGn0M6VwsR3u9KLa1sfUAEyrnF3dnGSkPAakx7o91el47J6MkCdpZbguC2FCvZBpY520kkrrg6OHhDx5lZEjiAYweRV6a5MtidzWRs5Zx1Ek1a5ugm9pz/5xoEwpe6LpdCyVsghMaiiWo+b43zTG64an0pvjrcRk1/odj4syO0KxKo9c+ziHZw5vkDgTYcMy6Y2Xa7avQa1GL9JpVyGmoWnIbOOnTPY04/jWeu/thZ3De980Sm1JU2n2dE63yP4NQlWYi7tN4aDr5RwGzvuCoUCo2gcCU4qtzqG1NTPdevpyAxMk34F/KZ7xTyaRPOfHD7oOgkHXcL7LBQKA5avA0uKCtYUFMxhMWzuHoS4njX5OhQpHG6gOiWUirhdl0lA184td6vFvpEtZjzVD68G8seeakgK+NOvtBlReWyniZ84to8D4+CAICZTLDrDg2mohf3/h7DX9L8Te8mcSlad6tm49N+eUenbLg6BsTMjlKsqjmZV397EZ80JfVW6pWCgc3HP5yOXFyZWfwdVFp4V3d/wwvGtx3Y65e9nO4V2LrmyfL3z42+7L36LwrBOvrkjtX3Z8o/fYa4diXW+2P94+/8VDP719cdP6vzZ+VFUyUNMZe/6JJU+x97V8siuyZuYze17f897Rwdk7u39ecUUeegSBdR+0VHz3/byaiq5zByoC3JP5xedP/dmz9PP4O+EV0aS18cIrXz7wcVlj7+8njh4uPf/Sc+VvlF8SzPlfR0/uu7h09x8NCzYdfv/CgdI5J71rf9wXX3Xg6W3HuXuHT7dVkgvxI+2b9uoNkaN40axF5T3KQ3M7d186ov1y9lO874v927f9uv2x0zubzxjLmk56n52zZHiExr8BP8bSQkYTAAA=";

        [Fact]
        public void CategoryTree()
        {
            var logger = TestLogger.NewLogger("ebay-categories-tests");
            var cache = new DiskCache(@"D:\code\bifrost\data\ebay\aspect-cache", logger);

            var tokenGetter = new EbayHardcodedTokenGetter();
            tokenGetter.Set(token);
            var categories = new lib.schema_readers.EbaySchemaReader(cache, logger, 10000, tokenGetter).CategoryTree().Result;

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamWriter sw = new StreamWriter(@"D:\code\bifrost\data\ebay\categories.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, categories);
            }

            Assert.True(categories.SubCategories.Count > 0);
        }
    }
}
