using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using HtmlAgilityPack;

namespace lib.poshmark_client
{
    public class PoshmarkClient
    {
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

        //public List<dynamic> GetImages(dynamic result)
        //{
        //    List<dynamic> images = new List<dynamic>();

        //    // Cover image
        //    images.Add(result.cover_shot.url_small);
        //    images.Add(result.cover_shot.url);
        //    images.Add(result.cover_shot.url_large);

        //    // Rest of the images
        //    foreach (var image in result.pictures)
        //    {
        //        images.Add(image.url_small);
        //        images.Add(image.url);
        //        images.Add(image.url_large);
        //    }

        //    return images;
        //}

        public string GetMainImageURL(dynamic result)
        {
            return result.cover_shot.url;
        }

        public int GetQuantity(dynamic result)
        {
            int quantity = 0;

            foreach (var resultQuantity in result.inventory.size_quantities)
            {
                quantity = resultQuantity.quantity_available;
            }

            return quantity;
        }

        public string GetCreatedDate(dynamic result)
        {
            string date = result.created_at;

            return date.Substring(0, 10);
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

        public List<dynamic> List(string accountName) 
        {
            return PoshmarkAPI.Request(accountName);
        }
    }
}
