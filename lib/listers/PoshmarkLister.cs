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
    public class PoshmarkLister
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

        public async Task<List<dynamic>> List()
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

            PoshmarkItem Item = new PoshmarkItem(); // An item to be populated from the JSON file
            List<dynamic> returnItems = new List<dynamic>();  // List with items to be returned

            foreach (var result in sellingItems)
            {
                Item = new PoshmarkItem();

                Item.ProductID = result.id;
                Item.Title = result.title;
                Item.Price = result.price;
                Item.OriginalPrice = result.original_price;
                Item.Size = result.size;
                Item.Brand = result.brand;
                Item.Description = result.description;
                Item.Shares = result.aggregates.shares;
                Item.Comments = result.aggregates.comments;
                Item.Likes = result.aggregates.likes;
                Item.Categories = items.GetCategories(result);
                Item.Colors = items.GetColors(result);
                Item.Images = items.GetImages(result);
                Item.Quantity = items.GetQuantity(result);
                Item.Status = result.inventory.status;
                Item.Date = items.GetCreatedDate(result);
                Item.URL = "https://poshmark.com/listing/" + Item.ProductID;
                Item.HasOffer = items.GetHasOffer(result);

                returnItems.Add(Item);
            }
            return returnItems;
        }
    }
}
