﻿using ebaytaxonomy;
using ebaytaxonomy.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace lib.listers
{
    public delegate string EbayTokenGetter();

    public class EbayLister: Lister
    {
        // https://developer.ebay.com/api-docs/commerce/taxonomy/static/supportedmarketplaces.html
        private const string MarketplaceUSA = "EBAY_US";

        IDistributedCache cache;
        ILogger logger;
        int liveCallLimit;
        int liveCalls = 0;
        EbayTokenGetter tokenGetter;

        public EbayLister(IDistributedCache cache, ILogger logger, int liveCallLimit, EbayTokenGetter tokenGetter)
        {
            this.cache = cache;
            this.logger = logger;
            this.liveCallLimit = liveCallLimit;
            this.tokenGetter = tokenGetter;
        }

        public async Task<Category> CategoryTree()
        {
            string eBayOAuthToken = this.tokenGetter();
            
            HttpClient httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip
            });
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {eBayOAuthToken}");
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));

            EbaytaxonomyClient client = new ebaytaxonomy.EbaytaxonomyClient(new TokenCredentials(eBayOAuthToken));

            var defaultCategoryTree = await client.GetDefaultCategoryTreeIdAsync(MarketplaceUSA);
            var categoryTree = await client.GetCategoryTreeWithHttpMessagesAsync(defaultCategoryTree.CategoryTreeId);
            
            return await ConstructCategoryTree(categoryTree.Body.RootCategoryNode, null, defaultCategoryTree.CategoryTreeId, client);
        }

        private async Task<Category> ConstructCategoryTree(CategoryTreeNode node, Category parent, string categoryTreeID, EbaytaxonomyClient client)
        {
            if (node.LeafCategoryTreeNode != null && node.LeafCategoryTreeNode == true)
            {
                Category leaf = new Category()
                {
                    Name = node.Category.CategoryName,
                    Features= new List<Feature>(),
                };

                var cachedAspects = await this.cache.GetAsync(node.Category.CategoryId);
                AspectMetadata aspects = new AspectMetadata(new List<Aspect>());

                if (cachedAspects == null && liveCalls < this.liveCallLimit)
                {
                    // This is a leaf node, so we want to query for all available features
                    liveCalls += 1;
                    aspects = await client.GetItemAspectsForCategoryAsync(node.Category.CategoryId, categoryTreeID);
                    await this.cache.SetAsync(
                        node.Category.CategoryId,
                        ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(aspects)),
                        new DistributedCacheEntryOptions()
                        {
                            AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                        });
                }
                else if (cachedAspects != null)
                {
                    aspects = JsonConvert.DeserializeObject<AspectMetadata>(
                        ASCIIEncoding.UTF8.GetString(cachedAspects));
                }

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
                await ConstructCategoryTree(subNode, category, categoryTreeID, client);
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
