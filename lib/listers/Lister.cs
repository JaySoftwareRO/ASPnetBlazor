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
        public string ID { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public double Price { get; set; }

        public Dictionary<Feature, object> Value { get; set; }

        public string Status { get; set; }

        public int Stock { get; set; }

        public string MainImageURL { get; set; }

        public string Size { get; set; }

        public string Brand { get; set; }

        public int OriginalPrice { get; set; }

        public List<string> Categories { get; set; }

        public List<string> Colors { get; set; }

        //public List<string> Images { get; set; }

        public string Date { get; set; }

        public string URL { get; set; }

        public string Shares { get; set; }

        public string Comments { get; set; }

        public string Likes { get; set; }

        public string HasOffer { get; set; }

    }
}
