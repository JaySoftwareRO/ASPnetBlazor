﻿using ebayws;
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
        private const string MarketplaceUSA = "EBAY_US";

        IDistributedCache cache;
        ILogger logger;
        int liveCallLimit;
        int liveCalls = 0;
        ITokenGetter tokenGetter;
        string accountID;

        public EbayLister(IDistributedCache cache, ILogger logger, int liveCallLimit, string accountID, ITokenGetter tokenGetter)
        {
            this.cache = cache;
            this.logger = logger;
            this.liveCallLimit = liveCallLimit;
            this.tokenGetter = tokenGetter;
            this.accountID = accountID;
        }
        public async Task<List<Item>> List()
        {
            // TODO: vladi: all of these should be configurable

            // Define the endpoint (e.g., the Sandbox Gateway URI)
            String endpoint = "https://api.ebay.com/wsapi";

            // Define the query string parameters.
            String queryString = "?callname=GetMyeBaySelling"
                                + "&siteid=0"
                                + "&appid=VladIova-Treecat-SBX-6bce464fb-92785135"
                                + "&version=1149"
                                + "&Routing=new";

            String requestURL = endpoint + queryString; // "https://api.ebay.com/wsapi";


            ebayws.eBayAPIInterfaceClient client = new ebayws.eBayAPIInterfaceClient();
            client.Endpoint.Address = new EndpointAddress(requestURL);



            var cachedSellingItems = await this.cache.GetAsync(this.accountID);
            GetMyeBaySellingResponse sellingItems = new GetMyeBaySellingResponse();

            if (cachedSellingItems == null && liveCalls < this.liveCallLimit)
            {
                liveCalls += 1;

                using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
                {
                    var httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers["X-EBAY-API-IAF-TOKEN"] = await tokenGetter.GetToken();

                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                    sellingItems = client.GetMyeBaySellingAsync(null, new GetMyeBaySellingRequestType()
                    {
                        Version = "1149",
                        ActiveList = new ItemListCustomizationType() { Sort = ItemSortTypeCodeType.TimeLeft, Pagination = new PaginationType() { EntriesPerPage = 3, PageNumber = 1 } }
                    }).GetAwaiter().GetResult();
                }

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
                sellingItems = JsonConvert.DeserializeObject<GetMyeBaySellingResponse>(
                    ASCIIEncoding.UTF8.GetString(cachedSellingItems));
            }

            List<Item> result = new List<Item>();

            foreach (var item in sellingItems.GetMyeBaySellingResponse1.ActiveList.ItemArray)
            {
                result.Add(new Item()
                {
                    Title = item.Title,
                    ID = item.ItemID,
                    Description = "BFRST-NPY",
                    Price = item.BuyItNowPrice.Value,
                    Status = "BFRST-NPY",
                    Stock = item.QuantityAvailable,
                    MainImageURL = item.PictureDetails.GalleryURL
                });
            }

            return result;
        }
    }
}
