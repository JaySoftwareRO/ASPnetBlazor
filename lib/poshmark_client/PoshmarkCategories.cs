using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib.poshmark_client
{
    public class Categories
    {
        public string MainCategoryList { get; set; } // Main category name, more broad (e.g. Women)
        public List<string> SecondCategoryList { get; set; } // Second category inside main category (e.g. Women/Accessories)
        public List<string> ThirdCategoryList { get; set; } // Third category inside second category (e.g. Women/Accessories/Belts)
    }

    public class PoshmarkCategories
    {
        public Categories category = new Categories();
        public string GetMainCategoryList(HtmlAgilityPack.HtmlNode node)
        {
            var temp = "";
            try
            {
                temp = node.SelectSingleNode(".").InnerText.Trim();
                
            } 
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to GetMainCategoriesList: " + e);
            }
            return temp;
        }


        public List<string> GetThirdCategoryList(HtmlAgilityPack.HtmlNodeCollection nodes)
        {
            List<string> temp = new List<string>();
            var tempString = "";
            try
            {
                foreach (var node in nodes)
                {
                    var nodes2 = node.SelectNodes("../../../div[2]/div/ul");
                    foreach (var node2 in nodes2)
                    {
                        var nodes3 = node2.SelectNodes("./li");
                        foreach (var node3 in nodes3)
                        {
                            tempString = node3.SelectSingleNode(".").InnerText.Trim();
                            var tempCheck = node3.SelectSingleNode(".").FirstChild.GetAttributeValue("class", "");
                            if (tempCheck == "all-caps")
                            {
                                // category.SecondCategoryList.Add(tempString);
                            }
                            else
                            {
                                temp.Add(tempString);
                            }
                        }
                    }
                }
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Error referring to GetThirdCategoriesList: " + e);
            }
            return temp;
        }

        public List<Categories> GetCategories()
        {
            List<Categories> categoriesList = new List<Categories>(); // To be returned

            var html = @"https://poshmark.com/closet/napelvs";
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(html);
            var nodes = htmlDoc.DocumentNode.SelectNodes("//nav[@class='header--scrollable']/div[1]/ul[1]/li");
                

            foreach (var node in nodes)
            {
                category = new Categories();

                category.MainCategoryList = GetMainCategoryList(node);
                category.ThirdCategoryList = GetThirdCategoryList(nodes);

                categoriesList.Add(category);
            }
            return categoriesList;
        }
    }
}
