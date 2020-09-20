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

        public PoshmarkLister(IDistributedCache cache, ILogger logger, int liveCallLimit, string accountID, ITokenGetter tokenGetter)
        {
            this.cache = cache;
            this.logger = logger;
            this.liveCallLimit = liveCallLimit;
            this.accountID = accountID;
            this.tokenGetter = tokenGetter;
        }

        public async Task<List<Item>> List()
        {
            PoshmarkClient items = new PoshmarkClient();
            var cachedSellingItems = await this.cache.GetAsync(this.accountID);

            List<dynamic> sellingItems = new List<dynamic>();

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
                sellingItems = JsonConvert.DeserializeObject<List<dynamic>>(
                    ASCIIEncoding.UTF8.GetString(cachedSellingItems));
            }

            List<Item> result = new List<Item>();  // List with items to be returned

            foreach (var item in sellingItems)
            {
                try
                {
                    var Item = new Item();

                    Item.ID = item.id;
                    Item.Title = item.title;
                    Item.Price = item.price;
                    Item.Description = item.description;
                    Item.Status = item.inventory.status;
                    Item.Stock = items.GetQuantity(item);
                    Item.MainImageURL = items.GetMainImageURL(item);
                    //Item.Size = result.size;
                    Item.Brand = item.brand;
                    //Item.Shares = item.aggregates.shares;
                    //Item.Comments = item.aggregates.comments;
                    //Item.Likes = item.aggregates.likes;
                    //Item.Categories = items.GetCategories(item);
                    //Item.Colors = items.GetColors(item);
                    Item.Date = items.GetCreatedDate(item);
                    Item.URL = "https://poshmark.com/listing/" + Item.ID;
                    Item.HasOffer = items.GetHasOffer(item);

                    result.Add(Item);
                } 
                catch (NullReferenceException e)
                {
                    logger.LogInformation("Error listing eBay items: " + e.ToString());
                }
            }

            return result;
        }
    }
}
