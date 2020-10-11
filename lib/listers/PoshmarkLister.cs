using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using lib.poshmark_client;
using Newtonsoft.Json;

namespace lib.listers
{
    public class PoshmarkLister : Lister
    {
        IDistributedCache cache;
        ILogger logger;
        int liveCallLimit;
        int liveCalls = 0;
        ITokenGetter tokenGetter;
        string accountID;

        public PoshmarkLister(IDistributedCache cache, ILogger logger, int liveCallLimit, ITokenGetter tokenGetter)
        {
            this.cache = cache;
            this.logger = logger;
            this.liveCallLimit = liveCallLimit;
            this.accountID = tokenGetter.GetUserID().Result;
            this.tokenGetter = tokenGetter;
        }

        public async Task<List<Item>> List()
        {
            PoshmarkClient items = new PoshmarkClient();
            var cachedSellingItems = await this.cache.GetAsync(this.accountID);

            List<PoshmarkItem> sellingItems = new List<PoshmarkItem>();

            if (cachedSellingItems == null && liveCalls < this.liveCallLimit)
            {
                liveCalls += 1;
                sellingItems = items.List(await this.tokenGetter.GetUserID()); // Account ID taken after the user is logged.

                await this.cache.SetAsync(
                    this.accountID,
                    ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(sellingItems)),
                    new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                    });
            } 
            else if (cachedSellingItems != null)
            {
                sellingItems = JsonConvert.DeserializeObject<List<PoshmarkItem>>(
                    ASCIIEncoding.UTF8.GetString(cachedSellingItems));
            }

            List<Item> returnItems = new List<Item>();  // List with items to be returned

            foreach (var item in sellingItems)
            {
                try
                {
                    var treecatItem = new Item();

                    treecatItem.ID = item.ID;
                    treecatItem.Provisioner = "Poshmark";
                    treecatItem.Title = item.Title;
                    treecatItem.Price = item.Price;
                    treecatItem.Description = item.Description;
                    treecatItem.Status = item.InventoryStatus;
                    treecatItem.Stock = item.InventorySizeQuantitiesQuantityAvailable;
                    treecatItem.MainImageURL = item.CoverShotUrl;
                    treecatItem.Size = item.Size;
                    treecatItem.Brand = item.Brand;
                    //Item.Shares = item.aggregates.shares;
                    //Item.Comments = item.aggregates.comments;
                    //Item.Likes = item.aggregates.likes;
                    //Item.Categories = items.GetCategories(item);
                    //Item.Colors = items.GetColors(item);
                    treecatItem.Date = items.GetCreatedDate(item);
                    treecatItem.URL = "https://poshmark.com/listing/" + treecatItem.ID;
                    treecatItem.HasOffer = items.GetHasOffer(item);

                    returnItems.Add(treecatItem);
                }
                catch (NullReferenceException e)
                {
                    logger.LogInformation("Error listing items: " + e.ToString());
                }
            }
            return returnItems;
        }

        public async Task<List<string>> ImportTreecatIDs(dynamic itemsToImport, List<Item> userPoshmarkCachedItems)
        {
            List<string> treecatIDs = new List<string>();
            for (int i = 0; itemsToImport.PoshmarkIDs.Count > i; i++)
            {
                // New TREECAT ID using Guid
                Guid treecatItemID = Guid.NewGuid();

                // Get "treecat_list" from cache into list
                var treecatByteItems = await this.cache.GetAsync("treecat_list");

                if (treecatByteItems != null)
                {
                    treecatIDs = JsonConvert.DeserializeObject<List<string>>(
                        ASCIIEncoding.UTF8.GetString(treecatByteItems));
                }

                // Link TreeCat ID to Item by importing the entire Item value to the GUID key
                // Also checks the item to import 
                foreach (var poshmarkItem in userPoshmarkCachedItems)
                {
                    if (treecatByteItems != null)
                    {
                        foreach (var treecatID in treecatIDs)
                        {
                            var treecatGUID = await this.cache.GetAsync(treecatID);
                            Item treecatItem = new Item();

                            treecatItem = JsonConvert.DeserializeObject<Item>(
                                    ASCIIEncoding.UTF8.GetString(treecatGUID));

                            if (itemsToImport.PoshmarkIDs[i] == poshmarkItem.ID)
                            {
                                if (poshmarkItem.ID != treecatItem.ID)
                                {
                                    // Insert GUID Key to Poshmark Item value
                                    await this.cache.SetAsync(
                                        treecatItemID.ToString(),
                                        ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(poshmarkItem)),
                                        new DistributedCacheEntryOptions()
                                        {
                                            AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                                        });

                                    // Update "treecat_list" cache by adding the new TreeCat ID generated with GUID
                                    await this.cache.RemoveAsync("treecat_list");
                                    treecatIDs.Add(treecatItemID.ToString());

                                    await this.cache.SetAsync(
                                        "treecat_list",
                                        ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(treecatIDs)),
                                        new DistributedCacheEntryOptions()
                                        {
                                            AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                                        });
                                }
                            }
                        }
                    }
                    else if (treecatByteItems == null)
                    {
                        if (itemsToImport.PoshmarkIDs[i] == poshmarkItem.ID)
                        {
                            await this.cache.SetAsync(
                                treecatItemID.ToString(),
                                ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(poshmarkItem)),
                                new DistributedCacheEntryOptions()
                                {
                                    AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                                });
                        }
                    }
                }
            }
            return treecatIDs;
        }
    }
}
