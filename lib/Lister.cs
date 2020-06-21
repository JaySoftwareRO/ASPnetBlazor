using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Lists all items for a provider

namespace lib
{
    public interface Lister
    {
        Task<List<Item>> List();
    }

    public class Item
    {
        public string ID
        {
            get; set;
        }

        public string Title
        {
            get; set;
        }

        public string Description
        {
            get; set;
        }

        public double Price
        {
            get; set;
        }

        public Dictionary<Feature, object> Value
        {
            get; set;
        }

        public string Status
        {
            get; set;
        }

        public int Stock
        {
            get; set;
        }

        public string MainImageURL
        {
            get; set;
        }
    }
}
