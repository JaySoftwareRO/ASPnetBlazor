﻿using lib.poshmark_client;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace tests
{
    public class PoshmarkTests
    {
        [Fact]
        public void List()
        {
            var PoshmarkCategories = new PoshmarkCategories();
            var foo = PoshmarkCategories.GetCategories();

            var PoshmarkClient = new PoshmarkClient();
            var foo2 = PoshmarkClient.List();
        }
    }
}
