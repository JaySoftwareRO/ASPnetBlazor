using ebayinventory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace lib.listers
{
    public class EbayLister: Lister
    {
        // https://developer.ebay.com/api-docs/commerce/taxonomy/static/supportedmarketplaces.html
        private const string MarketplaceUSA = "EBAY_US";

        IDistributedCache cache;
        ILogger logger;
        int liveCallLimit;
        int liveCalls = 0;
        ITokenGetter tokenGetter;

        public EbayLister(IDistributedCache cache, ILogger logger, int liveCallLimit, ITokenGetter tokenGetter)
        {
            this.cache = cache;
            this.logger = logger;
            this.liveCallLimit = liveCallLimit;
            this.tokenGetter = tokenGetter;
        }
        public async Task<List<Item>> List()
        {
            string eBayOAuthToken = this.tokenGetter.Get();

            HttpClient httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip
            });
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {eBayOAuthToken}");
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));

            EbayinventoryClient client = new EbayinventoryClient(new TokenCredentials(eBayOAuthToken));
            client.BaseUri = new Uri("https://api.ebay.com/sell/inventory/v1");


            var inventoryProducts = await client.GetInventoryItemsAsync();
            List<Item> result = new List<Item>();

            foreach (var inventoryProduct in inventoryProducts.InventoryItemsProperty)
            {
                var items = await client.GetOffersAsync(inventoryProduct.Sku);

                foreach (var offer in items.OffersProperty)
                {
                    result.Add(new Item()
                    {
                        Title = inventoryProduct.Product.Title,
                        ID = offer.Sku,
                        Description = offer.ListingDescription,
                        Price = Double.Parse(offer.PricingSummary.Price.Value),
                        Status = offer.Status,
                        Stock = offer.AvailableQuantity == null ? 0 : (int)offer.AvailableQuantity,
                    });
                }
            }

            return result;
        }
    }
}
