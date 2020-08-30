﻿using lib.cache.bifrost;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace tests
{
    public class PoshmarkListerTest
    {
        [Fact]
        public void List()
        {
            var logger = TestLogger.NewLogger("poshmark-lister-tests");

            var cache = new BifrostCache(@"https://localhost:5001/", "poshmark-items", logger);

            var items = new lib.listers.PoshmarkLister(cache, logger, 10000, "fake-helengu").List().Result;

        }
    }
}
