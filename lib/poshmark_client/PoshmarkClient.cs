using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using HtmlAgilityPack;

namespace lib.poshmark_client
{
    public class PoshmarkItem
    {
        public string ProductID { get; set; }

        public string Title { get; set; }

        public int Price { get; set; }

        public int OriginalPrice { get; set; }

        public string Status { get; set; }

        public string Size { get; set; }

        public string Brand { get; set; }

        public string Description { get; set; }

        public List<dynamic> Categories { get; set; }

        public List<dynamic> Colors { get; set; }

        public List<dynamic> Images { get; set; }

        public string Date { get; set; }

        public List<dynamic> Quantity { get; set; } // Quantity [0] = available, [1] = reserved, [2] sold

        public string URL { get; set; }

        public string Shares { get; set; }

        public string Comments { get; set; }

        public string Likes { get; set; }

        public string HasOffer { get; set; }
    }

    public class PoshmarkClient
    {
        private PoshmarkItem Item = new PoshmarkItem();

        public List<dynamic> GetCategories(dynamic result) 
        {
            List<dynamic> categories = new List<dynamic>();

            categories.Add(result.department.display);

            foreach(var category in result.category_features)
            {
                categories.Add(category.display);
            }

            categories.Add(result.category_v2.display);

            return categories;
        }

        public List<dynamic> GetColors(dynamic result)
        {
            List<dynamic> colors = new List<dynamic>();

            foreach (var color in result.colors)
            {
                colors.Add(color.name);
            }

            return colors;
        }

        public List<dynamic> GetImages(dynamic result)
        {
            List<dynamic> images = new List<dynamic>();

            // Cover image
            images.Add(result.cover_shot.url_small);
            images.Add(result.cover_shot.url);
            images.Add(result.cover_shot.url_large);

            // Rest of the images
            foreach (var image in result.pictures)
            {
                images.Add(image.url_small);
                images.Add(image.url);
                images.Add(image.url_large);
            }

            return images;
        }

        public List<dynamic> GetQuantity(dynamic result)
        {
            List<dynamic> quantities = new List<dynamic>();

            // Quantity [0] = available, [1] = reserved, [2] sold
            foreach (var quantity in result.inventory.size_quantities)
            {
                quantities.Add(quantity.quantity_available); 
                quantities.Add(quantity.quantity_reserved);
                quantities.Add(quantity.quantity_sold);
            }

            return quantities;
        }

        public string GetStatus()
        {
            string status = String.Empty;

            if (Item.Quantity[2] > "0")
            {
                status = "Sold";
            } 
            else if(Item.Quantity[1] > "0")
            {
                status = "Reserved";
            }
            else if (Item.Quantity[0] > "0")
            {
                status = "Published";
            }

            return status;
        }

        public string GetCreatedDate(dynamic result)
        {
            string date = result.created_at;

            return date.Substring(0, 10);
        }

        public string GetURL()
        {
            string URL = String.Empty;

            string title = Item.Title.Replace("/", "");

            URL = "https://poshmark.com/listing/" + title + " " + Item.ProductID;

            return URL;
        }

        public string GetHasOffer(dynamic result)
        {
            string hasOffer = String.Empty;

            if (result.has_offer == true)
            {
                hasOffer = "Yes";
            } 
            else if (result.has_offer == false)
            {
                hasOffer = "No";
            }

            return hasOffer;
        }

        public List<PoshmarkItem> List() 
        {
            List<PoshmarkItem> items = new List<PoshmarkItem>();

            var results = new List<dynamic>();
            results = PoshmarkAPI.Request("simplysabr");
            
            foreach(var result in results)
            {
                Item = new PoshmarkItem();

                Item.ProductID = result.id;
                Item.Title = result.title;
                Item.Price = result.price;
                Item.OriginalPrice = result.original_price;
                Item.Size = result.size;
                Item.Brand = result.brand;
                Item.Description = result.description;
                Item.Shares = result.aggregates.shares;
                Item.Comments = result.aggregates.comments;
                Item.Likes = result.aggregates.likes;
                Item.Categories = GetCategories(result);
                Item.Colors = GetColors(result);
                Item.Images = GetImages(result);
                Item.Quantity = GetQuantity(result);
                Item.Status = GetStatus();
                Item.Date = GetCreatedDate(result);
                Item.URL = GetURL();
                Item.HasOffer = GetHasOffer(result);

                items.Add(Item);
            }
            return items;
        }
    }
}
