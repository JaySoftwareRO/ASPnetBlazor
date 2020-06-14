using System;
using System.Collections.Generic;

// Imports an item into our database

namespace lib
{
    public interface Importer
    {
        List<Item> GetItems();
    }
}
