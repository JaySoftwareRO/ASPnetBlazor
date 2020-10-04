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
                    var Item = new Item();

                    Item.ID = item.ID;
                    Item.Title = item.Title;
                    Item.Price = item.Price;
                    Item.Description = item.Description;
                    Item.Status = item.InventoryStatus;
                    Item.Stock = item.InventorySizeQuantitiesQuantityAvailable;
                    Item.MainImageURL = item.CoverShotUrl;
                    //Item.Size = result.size;
                    Item.Brand = item.Brand;
                    //Item.Shares = item.aggregates.shares;
                    //Item.Comments = item.aggregates.comments;
                    //Item.Likes = item.aggregates.likes;
                    //Item.Categories = items.GetCategories(item);
                    //Item.Colors = items.GetColors(item);
                    Item.Date = items.GetCreatedDate(item);
                    Item.URL = "https://poshmark.com/listing/" + Item.ID;
                    Item.HasOffer = items.GetHasOffer(item);

                    returnItems.Add(Item);
                }
                catch (NullReferenceException e)
                {
                    logger.LogInformation("Error listing items: " + e.ToString());
                }
            }
            return returnItems;
        }

        public async Task<List<Item>> ListTreecatItems(dynamic itemsToImport, string googleID, List<Item> userPoshmarkCachedItems)
        {
            // Only the Item's ID is saved in the cache, but the function MUST return Item objects.

            // List containing item IDs to be imported
            List<string> listToCache = new List<string>();

            // Variable to return Item objects
            List<Item> itemsToReturn = new List<Item>();

            // User's TREECAT cached items based on GMAIL
            var treecatCachedItemIDs = await this.cache.GetAsync(googleID);
            List<string> treecatItemIDs = new List<string>();

            if (treecatCachedItemIDs != null)
            {
                treecatItemIDs = JsonConvert.DeserializeObject<List<string>>(
                    ASCIIEncoding.UTF8.GetString(treecatCachedItemIDs));
            }

            // Algorithm to sort what's imported into the TREECAT(GMAIL) account
            // All the user's item IDs to import into TREECAT (itemsToImport variable)
            for (int i = 0; itemsToImport.PoshmarkIDs.Count > i; i++) 
            {
                // Found item ID in "itemsToImport"
                var itemToImport = itemsToImport.PoshmarkIDs[i];

                // All the user's cached items from POSHMARK
                foreach (var item in userPoshmarkCachedItems) 
                {
                    // Compare against the already TREECAT cached item IDs if these exist
                    if (treecatCachedItemIDs != null)
                    {
                        // Add item ID to the list when algorithm finds an ID that matches 
                        // an ID from "itemsToImport" and it's not already in the TreeCat cache
                        if (itemToImport == item.ID && treecatItemIDs[i] != item.ID)
                        {
                            listToCache.Add(item.ID); // Import in the cache
                        }
                    } 
                    else if (treecatCachedItemIDs == null)
                    {
                        // Add item ID to the list when algorithm finds an ID that matches an ID from "itemsToImport"
                        if (itemToImport == item.ID)
                        {
                            listToCache.Add(item.ID); // Import in the cache
                        }
                    }
                }
            }

            // Add all user's TreeCat cached items to a list to return them
            for (int i = 0; treecatItemIDs.Count > i; i++)
            {
                foreach (var poshmarkItem in userPoshmarkCachedItems)
                {
                    if (treecatItemIDs[i] == poshmarkItem.ID)
                    {
                        itemsToReturn.Add(poshmarkItem); // Return Item
                    }
                }
            }

            // At this point, we have the list with the verified item IDs we want to import 
            // into the TREECAT CACHE

            if (treecatCachedItemIDs == null && liveCalls < this.liveCallLimit)
            {
                liveCalls += 1;

                await this.cache.SetAsync(
                    googleID,
                    ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(listToCache)),
                    new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                    });
            }
            else if (treecatCachedItemIDs != null)
            {
                // Now, we should store the user's items from the TreeCat cache in a variable
                // Already done (treecatCachedItems var)

                // Then, remove the key from the cache
                await this.cache.RemoveAsync(googleID);

                // After that, add the old items together with the new ones
                // Add old and new IDs together in a JSON format
                //List<string> test = treecatCachedItemIDs;

                listToCache.AddRange(treecatItemIDs);

                // And, finally, import all of them again into the cache with the same CACHE KEY
                await this.cache.SetAsync(
                    googleID,
                    ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(listToCache)),
                    new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                    });
            }

            return itemsToReturn;
        }
    }
}
