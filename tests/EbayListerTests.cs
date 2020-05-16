using lib.cache;
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
        string token = "v^1.1#i^1#r^0#I^3#p^3#f^0#t^H4sIAAAAAAAAAOVYa2wUVRTu9qVNragQ5WWyDJKIzeze2dmZ3Rm7m2y7W7tA26XbViBguTtzpzsyL2futl2CWioBAf0hicovLGgksWmkohgwxpCgKJEfQsCgmPioQkg0xhAlAYwz2wfbqtAHMU3cP5s59zy+851z7ty5oLu07JGtdVv/qHDdUdjbDboLXS6qHJSVllTeXVQ4v6QA5Cm4ersf6i7uKbpYZUFVMfgmZBm6ZiF3l6poFp8ThoiMqfE6tGSL16CKLB4LfDJSv4L3eQBvmDrWBV0h3PFoiKA5yAicxNBMihMEWrSl2ojPZj1EBAXIiSmJCiCGSzF+2l63rAyKaxaGGg4RPuADJGBIim2m/DygeYrzUMHAGsLdikxL1jVbxQOIcA4un7M187DeHCq0LGRi2wkRjkdqk42ReDTW0FzlzfMVHuYhiSHOWGOfanQRuVuhkkE3D2PltPlkRhCQZRHe8FCEsU75yAiYKcDPUS0FOEgFKJrlAJDo20RlrW6qEN8chyORRVLKqfJIwzLO3opRm43Uk0jAw08Ntot41O38rcxARZZkZIaIWHVkdUsy1kS4k4mEqXfIIhKdTCnW5w/QgKFstB0KlPUOqOltYDjMkK9hksfFqdE1UXYos9wNOq5GNmY0lhmaZ/KYsZUatUYzImEHT74eO8JgILjGKelQDTM4rTlVRapNgzv3eGv+RxriRgvcrpaguCDkAn6OFVIBmmH8/9ASzqxPui3CTmUiiYTXwYJSMEuq0NyAsKFAAZGCTW9GRaYs8jQj+eighEiR5STSz0kSmWJElqQkhABCqZTABf8/3YGxKacyGI12yPiFXIohwmGUl6HEY30D0pqzBiLGa+a2neG26LJCRBpjg/d6Ozs7PZ20RzfbvT4AKO+q+hVJIY1USIzqyrdWJuVcgwjItrJkHtsAQkSX3X92cK2dCDfFaptiybq25sblsYaR3h2DLDxe+i+ZJpFgIjyzspNqs2qNsbxB5BpZtbWlXjNYugFFoy2P6Q2VstoOYHXSiMNIOm6Fppe8oBsooSuykP1vGHBmfaIs0KaYgCbOJpGi2IJpJWo5ic6sIjv2lu0AGrLHGTePoKteHdobtiNqyyF2T0TJa9kEeYa2P9uzx0RQ1DUlOxXjSdjIWoe9f+hmdioBR40nYQMFQc9oeCrhhk0nYSFlFElWFGeLnErAPPPJwNSgksWyYE0ppKw53WZNwsSA2VyComwZzqxMyNKW2W9WAXnst13uoDUKdtwsOrM+uSmNGEZcVTMYphQUF2fWuPopX5BlprUJOenNsKxaFSjG7eMJ2WwiJEBMJpqipD8liFAUU5AMcqzfJ/rYaaVd3y7PsKwpjgP2wZSigwDQ08otijpmWklZUfRxIMiSQkryk37RPvoGmSBFMizyQ8BAAIXp1bNGke3BH3smLN7880zIvU63MBInmt04Qd6Z+G8fQ96xdxHhgtyP6nG9B3pcA4UuF/CCJdRisKi0qKW46K75loztDRJKHktu1+xPbBN5NqCsAWWzsNRltMBLS/JuP3rXgbmj9x9lRVR53mUIWHhjpYSa9UCFTQhDsZQf0BS3Biy+sVpM3V8854nrZZVtP/b+uvXUupWMFN125Gz3MVAxquRylRQU97gKqurOrkizc9JHzME3j6lfMu47X11Wffr7xtLCtSsfP0B8U9F3708lA8DYvb/r+UUtX6DO1c+gbz8Y3PhnVV/62e7+ZW8tqO69Wnn4uRcPL73v1OHjB2Z/fOKT+LuJd37rv7bg3MPXojUbS67v3PTLy/POvbKlqr+3LqglvWT13PJ9nQe7Dp10nd87uGV9+a61fTXlA5+u33pxz/Er54vd2+vvmV11Ql2Y/npfbew0+v3Cpr4P95+6tnPV3uiDbx9/YwC3vk9fjj0VfG130foXys+c7D+360L4yNLTH63eUcJsO/PdoRM7vhosPL8LhsVtRz/74crizy+9VF7w6MWrVfHXL4fnbT+w+aj/6fY9ZMXBWUPl+wtAvob+lxIAAA==";

        [Fact]
        public void List()
        {
            var logger = TestLogger.NewLogger("ebay-categories-tests");
            var cache = new DiskCache(@"D:\code\bifrost\data\ebay\aspect-cache", logger);

            var categories = new lib.listers.EbayLister(cache, logger, 1000, () => { return token; }).CategoryTree().Result;

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
