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

        public bool NotForSale { get; set; }

        public string Size { get; set; }

        public string Brand { get; set; }

        public string ProductPageLink { get; set; }

        public string Description { get; set; }

        public List<string> Categories { get; set; }

        public List<string> Color { get; set; }
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
                tempTitle = node.SelectSingleNode("./div[@class='tile  ']/a").GetAttributeValue("title", "");
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
                tempPrice = Convert.ToDouble(node.SelectSingleNode("./div[@class='tile  ']").GetAttributeValue("data-post-price", "").Trim('$'));
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to Price: " + e);
            }
            return tempPrice;
        }
        // NotForSale boolean of a product
        public bool GetNotForSale(HtmlAgilityPack.HtmlNode node)
        {
            var tempBool = false;
            try
            {   
                if (node.SelectSingleNode(".//i[@class='icon inventory-tag not-for-sale-tag']").InnerText == "Not for Sale")
                {
                    tempBool = true;
                } 
                else
                {
                    tempBool = false;
                }
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to NotForSale: " + e);
            }
            return tempBool;
        }
        // Size of of a product
        public string GetSize(HtmlAgilityPack.HtmlNode node)
        {
            var tempSize = "";
            try
            {
                tempSize = node.SelectSingleNode("./div[@class='tile  ']/div[@class='item-details']/ul[@class='pipe']/li[@class='size']/div").GetAttributeValue("title", "");
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to Size: " + e);
                
                tempSize = node.SelectSingleNode("./div[@class='tile  ']/div[@class='item-details']/ul[@class='pipe']/li/a").GetAttributeValue("title", "");
                
            }
            return tempSize;
        }
        // Brand of a product
        public string GetBrand(HtmlAgilityPack.HtmlNode node)
        {
            var tempBrand = "";
            try
            {
                tempBrand = node.SelectSingleNode("./div[@class='tile  ']/div[@class='item-details']/ul[@class='pipe']/li[@class='brand']/a").GetAttributeValue("title", "");
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
                tempLink = node.SelectSingleNode("./div[@data-post-price='$" + Item.Price + "']/a[@title='" + Item.Title + "']").GetAttributeValue("href", "");
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

        public List<PoshmarkItem> List() 
        {
            List<PoshmarkItem> items = new List<PoshmarkItem>();

            // Web page with all the listings of an user
            var htmlCloset = @"https://poshmark.com/closet/napelvs";
            HtmlWeb webCloset = new HtmlWeb();
            var htmlDocCloset = webCloset.Load(htmlCloset);
            var nodesCloset = htmlDocCloset.DocumentNode.SelectNodes("//div[@id='tiles-con']/div[@class='col-x12 col-l6 col-s8']");
;

            foreach (var nodeCloset in nodesCloset)
            {
                Item = new PoshmarkItem(); // Create new PoshmarkItem object to be populated

                Item.Title = GetTitle(nodeCloset);
                Item.Price = GetPrice(nodeCloset);
                Item.NotForSale = GetNotForSale(nodeCloset);
                Item.Size = GetSize(nodeCloset);
                Item.Brand = GetBrand(nodeCloset);
                Item.ProductPageLink = GetProductPageLink(nodeCloset); // Get link for the product page 

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
