using Microsoft.Rest;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace lib.listers
{
    public class EbayLister: Lister
    {
        public async Task<List<Category>> Categories()
        {
            string eBayOAuthToken = "";
            
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {eBayOAuthToken}");

            ebaytaxonomy.EbaytaxonomyClient client = new ebaytaxonomy.EbaytaxonomyClient(new TokenCredentials(eBayOAuthToken));
         
            var response = await client.GetCategoryTreeWithHttpMessagesAsync("0");
            

            List<Category> result = new List<Category>();
            foreach (var category in response.Body.RootCategoryNode.ChildCategoryTreeNodes)
            {
                result.Add(new Category() { Name = category.Category.CategoryName });
            }

            return result;
        }
    }
}
