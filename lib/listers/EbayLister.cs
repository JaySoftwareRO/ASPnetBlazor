using ebayws;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace lib.listers
{
    public class EbayLister : Lister
    {
        // https://developer.ebay.com/api-docs/commerce/taxonomy/static/supportedmarketplaces.html
        // private const string MarketplaceUSA = "EBAY_US";

        // TODO: vladi: all of these should be configurable
        // Define the endpoint (e.g., the Sandbox Gateway URI)
        private static string endpoint = "https://api.ebay.com/wsapi";

        // Define the query string parameters.
        private static string queryString = "?callname=GetMyeBaySelling"
                            + "&siteid=0"
                            + "&appid=VladIova-Treecat-SBX-6bce464fb-92785135"
                            + "&version=1149"
                            + "&Routing=new";

        String requestURL = endpoint + queryString; // "https://api.ebay.com/wsapi";

        IDistributedCache cache;
        ILogger logger;
        int liveCallLimit;
        int liveCalls = 0;
        ITokenGetter tokenGetter;
        string accountID;

        public EbayLister(IDistributedCache cache, ILogger logger, int liveCallLimit, ITokenGetter tokenGetter)
        {
            this.cache = cache;
            this.logger = logger;
            this.liveCallLimit = liveCallLimit;
            this.tokenGetter = tokenGetter;
            this.accountID = tokenGetter.GetUserID().Result;
        }
        public async Task<List<Item>> List()
        {
            eBayAPIInterfaceClient client = new eBayAPIInterfaceClient();
            client.Endpoint.Address = new EndpointAddress(requestURL);

            var cachedSellingItems = await this.cache.GetAsync(this.accountID);
            GetMyeBaySellingResponse sellingItems = new GetMyeBaySellingResponse();

            if (cachedSellingItems != null)
            {
                sellingItems = JsonConvert.DeserializeObject<GetMyeBaySellingResponse>(ASCIIEncoding.UTF8.GetString(cachedSellingItems));

                if (sellingItems.GetMyeBaySellingResponse1 == null)
                {
                    await this.cache.RemoveAsync(this.accountID);
                    cachedSellingItems = null;
                }
            }

            if (cachedSellingItems == null && liveCalls < this.liveCallLimit)
            {
                liveCalls += 1;

                // TODO: dispose of the OperationContextScope properly
                var httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers["X-EBAY-API-IAF-TOKEN"] = await tokenGetter.GetToken();

                using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
                {
                    try
                    {
                        OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                        sellingItems = client.GetMyeBaySellingAsync(null, new GetMyeBaySellingRequestType()
                        {
                            // TODO: should not be hardcoded
                            Version = "1149",
                            ActiveList = new ItemListCustomizationType() { Sort = ItemSortTypeCodeType.TimeLeft, Pagination = new PaginationType() { EntriesPerPage = 3, PageNumber = 1 } }
                        }).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError($"error getting ebay items {ex}");
                    }
                }

                await this.cache.SetAsync(
                    this.accountID,
                    ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(sellingItems)),
                    new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                    });
            }

            List<Item> result = new List<Item>();

            foreach (var item in sellingItems.GetMyeBaySellingResponse1.ActiveList.ItemArray)
            {
                try
                {
                    var treecatItem = new Item();

                    treecatItem.ID = item.ItemID;
                    treecatItem.Provisioner = "eBay";
                    treecatItem.Title = item.Title;
                    treecatItem.Price = item.BuyItNowPrice.Value;
                    treecatItem.Description = "BFRST-NPY";
                    treecatItem.Status = item.SellingStatus.ListingStatus.ToString();
                    treecatItem.Stock = item.QuantityAvailable;
                    treecatItem.MainImageURL = item.PictureDetails.GalleryURL;
                    treecatItem.Size = "BFRST-NPY";
                    treecatItem.Brand = "BFRST-NPY";
                    // Ebay has no shares/likes/comments
                    // Item.Categories.Add("BFRST-NPY");
                    // Item.Colors.Add("BFRST-NPY");
                    treecatItem.Date = item.ListingDetails.StartTime.ToString().Substring(0, 10);
                    treecatItem.URL = item.ListingDetails.ViewItemURL;
                    treecatItem.HasOffer = item.SellingStatus.BidCount > 0 ? "Yes" : "No";

                    result.Add(treecatItem);
                }
                catch (NullReferenceException e)
                {
                    logger.LogInformation("Error listing eBay items: " + e.ToString());
                }
            }

            return result;
        }

        public async Task<List<string>> ImportTreecatIDs(dynamic itemsToImport, List<Item> userEbayCachedItems)
        {
            List<string> treecatIDs = new List<string>();
            for (int i = 0; itemsToImport.EbayIDs.Count > i; i++)
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
                foreach (var ebayItem in userEbayCachedItems)
                {
                    if (treecatByteItems != null)
                    {
                        foreach (var treecatID in treecatIDs)
                        {
                            var treecatGUID = await this.cache.GetAsync(treecatID);
                            Item treecatItem = new Item();

                            treecatItem = JsonConvert.DeserializeObject<Item>(
                                    ASCIIEncoding.UTF8.GetString(treecatGUID));

                            if (itemsToImport.EbayIDs[i] == ebayItem.ID)
                            {
                                if (ebayItem.ID != treecatItem.ID)
                                {
                                    // Insert GUID Key to Ebay Item value
                                    await this.cache.SetAsync(
                                        treecatItemID.ToString(),
                                        ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(ebayItem)),
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
                        if (itemsToImport.EbayIDs[i] == ebayItem.ID)
                        {
                            await this.cache.SetAsync(
                                treecatItemID.ToString(),
                                ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(ebayItem)),
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
