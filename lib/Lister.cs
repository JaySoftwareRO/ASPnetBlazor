using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Lists all items for a provider

namespace lib
{
    public interface Lister
    {
        Task<List<Category>> Categories();
    }

    public class Category
    {
        public string Name { get; set; }
    }
}
