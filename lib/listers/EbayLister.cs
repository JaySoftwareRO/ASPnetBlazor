using ebaytaxonomy;
using ebaytaxonomy.Models;
using Microsoft.Rest;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace lib.listers
{
    public class EbayLister: Lister
    {
        // https://developer.ebay.com/api-docs/commerce/taxonomy/static/supportedmarketplaces.html
        private const string MarketplaceUSA = "EBAY_US";
        public async Task<Category> CategoryTree()
        {
            string eBayOAuthToken = "v^1.1#i^1#r^0#f^0#I^3#p^3#t^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";
            
            HttpClient httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip
            });
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {eBayOAuthToken}");
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));

            EbaytaxonomyClient client = new ebaytaxonomy.EbaytaxonomyClient(new TokenCredentials(eBayOAuthToken));

            var defaultCategoryTree = await client.GetDefaultCategoryTreeIdAsync(MarketplaceUSA);
            var categoryTree = await client.GetCategoryTreeWithHttpMessagesAsync(defaultCategoryTree.CategoryTreeId);
            
            return await constructCategoryTree(categoryTree.Body.RootCategoryNode, null, defaultCategoryTree.CategoryTreeId, client);
        }

        int limit = 0;
        private async Task<Category> constructCategoryTree(CategoryTreeNode node, Category parent, string categoryTreeID, EbaytaxonomyClient client)
        {
            if (node.LeafCategoryTreeNode != null && node.LeafCategoryTreeNode == true)
            {
                Category leaf = new Category()
                {
                    Name = node.Category.CategoryName,
                    Features= new List<Feature>(),
                };

                if (limit < 10)
                {
                    // This is a leaf node, so we want to query for all available features
                    var aspects = await client.GetItemAspectsForCategoryAsync(node.Category.CategoryId, categoryTreeID);
                    foreach (var aspect in aspects.Aspects)
                    {
                        leaf.Features.Add(new Feature()
                        {
                            Name = aspect.LocalizedAspectName,
                            FeatureType = new FeatureType()
                            {
                                Options = new FeatureTypeOptions()
                                {
                                    ProviderDefinition = new Dictionary<string, object>
                                {
                                    { Providers.EBay, aspect },
                                }
                                }
                            }
                        });
                    }
                }
                limit += 1;
                // Add the leaf to its parent and exit
                parent.SubCategories.Add(leaf);
                return null;
            }

            // This is a normal category - define it and recurse
            Category category = new Category()
            {
                Name = node.Category.CategoryName,
                SubCategories = new List<Category>(),
            };

            foreach (var subNode in node.ChildCategoryTreeNodes)
            {
                await constructCategoryTree(subNode, category, categoryTreeID, client);
            }

            if (parent != null)
            {
                // Add the category to its parent then exit
                parent.SubCategories.Add(category);
                return null;
            }

            // If this is the root category, return it
            return category;
        }
    }
}
