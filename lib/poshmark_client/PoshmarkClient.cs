using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using HtmlAgilityPack;

namespace lib.poshmark_client
{
    public class PoshmarkItem
    {
        public string Title { get; set; }

        public double Price { get; set; }

        public string Status { get; set; }

        public string Size { get; set; }

        public string Brand { get; set; }

        public string ProductPageLink { get; set; }

        public string Description { get; set; }

        public List<string> Categories { get; set; }

        public List<string> Color { get; set; }

        public string MainImageURL { get; set; }
    }
    public class PoshmarkClient
    {
        public PoshmarkItem Item = new PoshmarkItem();
        // Title of a product
        public string GetTitle(HtmlAgilityPack.HtmlNode node)
        {
            var tempTitle = "";
            try
            {
                tempTitle = node.SelectSingleNode("./div/div[@class='item__details']/div/a").InnerText.Trim();
            } 
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to Title: " + e);
            }
            return tempTitle;
        }
        // Price of a product
        public double GetPrice(HtmlAgilityPack.HtmlNode node)
        {
            var tempPrice = 0.0;
            try
            {
                tempPrice = Convert.ToDouble(node.SelectSingleNode(".//span[@class='p--t--1 fw--bold']").InnerText.Trim().Trim('$'));
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to Price: " + e);
            }
            return tempPrice;
        }
        
        // Size of of a product
        public string GetSize(HtmlAgilityPack.HtmlNode node)
        {
            var tempSize = "";
            try
            {
                tempSize = node.SelectSingleNode("./div/div[@class='item__details']/div[2]/div[2]/a[1]").InnerText.Trim();
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to Size: " + e);
                
            }
            return tempSize;
        }
        // Brand of a product
        public string GetBrand(HtmlAgilityPack.HtmlNode node)
        {
            var tempBrand = "";
            try
            {
                tempBrand = node.SelectSingleNode("./div/div[@class='item__details']/div[2]/div[2]/a[2]").InnerText.Trim();
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to Brand: " + e);
            }
            return tempBrand;
        }

        // Categories of a product
        public List<String> GetCategories(HtmlAgilityPack.HtmlNodeCollection nodes)
        {
            List<string> categories = new List<string>();
            try
            {                
                foreach (var node in nodes)
                {
                    var nodes2 = node.SelectNodes("./div[@class='d--fl fw--w']/div[1]/a");

                    foreach (var node2 in nodes2)
                    {
                        categories.Add(node2.SelectSingleNode(".").InnerText.Trim());
                    }
                }
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("Error referring to Categories: " + e);
            }
            return categories;
        }

        public string GetDescription(HtmlAgilityPack.HtmlNodeCollection nodes)
        {
            var tempDescription = "";
            try
            {
                foreach (var node in nodes)
                {
                    tempDescription = node.SelectSingleNode("./div[@class='listing__description fw--light']").InnerText.Trim();
                }
            } catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to Description: " + e);
            }
            return tempDescription;
        }

        public string GetProductPageLink(HtmlAgilityPack.HtmlNode node)
        {
            var tempLink = "";
            try
            {
                tempLink = node.SelectSingleNode("./div/a").GetAttributeValue("href", "");
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to GetProductPageLink: " + e);
            }
            return tempLink;
        }

        public List<string> GetColor(HtmlAgilityPack.HtmlNodeCollection nodes)
        {
            List<string> colors = new List<string>();
            try
            {
                foreach (var node in nodes)
                {
                    var nodes2 = node.SelectNodes("./div[@class='d--fl fw--w']/div[2]/a");
                    foreach (var node2 in nodes2)
                    {
                        colors.Add(node2.SelectSingleNode(".").GetAttributeValue("data-et-prop-listing_color", ""));
                    }
                }
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to Description: " + e);
            }
            return colors;
        }

        public HtmlNodeCollection GetProductPage()
        { 
            // Web page with product pages
            var htmlProduct = "https://poshmark.com" + Item.ProductPageLink;
            HtmlWeb webProduct = new HtmlWeb();
            var htmlDocProduct = webProduct.Load(htmlProduct);
            var nodesProduct = htmlDocProduct.DocumentNode.SelectNodes("//div[@class='listing__layout-grid listing__layout-item listing__info col-x24 col-m12']");

            return nodesProduct;
        }

        public string GetMainImageURL(HtmlAgilityPack.HtmlNode node)
        {
            var tempURL = "";
            try
            {
                tempURL = node.SelectSingleNode(".//div[@class='img__container img__container--square']/img").GetAttributeValue("src", "");
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to GetMainImageURL: " + e);
            }
            return tempURL;
        }

        // Status of a product. TODO: "problem" status
        public string GetStatus(HtmlAgilityPack.HtmlNode node)
        {
            var tempStatus = "";
            try
            {
                if (node.SelectSingleNode(".//span[@class='inventory-tag__text']").InnerText.Trim() == "Not for Sale")
                {
                    tempStatus = "unlisted";
                }
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to Status: " + e);
            }

            try
            {
                if (node.SelectSingleNode(".//span[@class='inventory-tag__text']").InnerText.Trim() == "Sold Out")
                {
                    tempStatus = "sold";
                }
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to Status: " + e);
            }

            try
            {
                if (node.SelectSingleNode(".//span[@class='inventory-tag__text']").InnerText.Trim() == "Sold")
                {
                    tempStatus = "sold";
                }
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to Status: " + e);
            }

            return tempStatus;
        }

        public List<PoshmarkItem> List() 
        {
            List<PoshmarkItem> items = new List<PoshmarkItem>();

            // Web page with all the listings of an user
            var htmlCloset = @"https://poshmark.com/closet/flippinoptimist?availability=sold_out";
            HtmlWeb webCloset = new HtmlWeb();
            var htmlDocCloset = webCloset.Load(htmlCloset);
            var nodesCloset = htmlDocCloset.DocumentNode.SelectNodes("//section[@class='main__column col-l19 col-x16']/div[@class='m--t--1']/div");

            foreach (var nodeCloset in nodesCloset)
            {
                Item = new PoshmarkItem(); // Create new PoshmarkItem object to be populated

                Item.Title = GetTitle(nodeCloset);
                Item.Price = GetPrice(nodeCloset);
                Item.Status = GetStatus(nodeCloset);
                Item.Size = GetSize(nodeCloset);
                Item.Brand = GetBrand(nodeCloset);
                Item.ProductPageLink = GetProductPageLink(nodeCloset); // Get link for the product page 
                Item.MainImageURL = GetMainImageURL(nodeCloset);

                HtmlNodeCollection nodesProduct = GetProductPage(); 

                Item.Description = GetDescription(nodesProduct);
                Item.Categories = GetCategories(nodesProduct); // Categories requires all the nodes as it has a foreach loop inside
                Item.Color = GetColor(nodesProduct); 

                items.Add(Item);
            }
            return items;
        }
    }
}
