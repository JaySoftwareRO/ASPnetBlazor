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

        public PoshmarkLister(IDistributedCache cache, ILogger logger, int liveCallLimit, string accountID)
        {
            this.cache = cache;
            this.logger = logger;
            this.liveCallLimit = liveCallLimit;
            this.accountID = accountID;
        }

        public async Task<List<dynamic>> List()
        {
            PoshmarkClient items = new PoshmarkClient();
            var cachedSellingItems = await this.cache.GetAsync(this.accountID);
            var sellingItems = items.List("ckingsings"); // Random poshmark account name

            if (cachedSellingItems == null && liveCalls < this.liveCallLimit)
            {
                liveCalls += 1; 

                await this.cache.SetAsync(
                    this.accountID,
                    ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(sellingItems)),
                    new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200), 
                    });
            } else if (cachedSellingItems != null)
            {
                sellingItems = JsonConvert.DeserializeObject<List<dynamic>>(
                    ASCIIEncoding.UTF8.GetString(cachedSellingItems));
            }

            PoshmarkItem Item = new PoshmarkItem(); // An item to be populated from the JSON file
            List<dynamic> returnItems = new List<dynamic>();  // List with items to be returned

            foreach (var result in sellingItems)
            {
                Item = new PoshmarkItem();
                PoshmarkClient client = new PoshmarkClient(); // To access the methods in PoshmarkClient

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
                Item.Categories = client.GetCategories(result);
                Item.Colors = client.GetColors(result);
                Item.Images = client.GetImages(result);
                Item.Quantity = client.GetQuantity(result);
                Item.Status = result.inventory.status;
                Item.Date = client.GetCreatedDate(result);
                Item.URL = "https://poshmark.com/listing/" + Item.ProductID;
                Item.HasOffer = client.GetHasOffer(result);

                returnItems.Add(Item);
            }
            return returnItems;
        }
    }
}
