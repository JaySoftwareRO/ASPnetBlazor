﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace ebayinventory.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This type is used by the base response of the
    /// &lt;strong&gt;getOffers&lt;/strong&gt; call, and it is an array of
    /// one or more of the seller's offers, along with pagination data.
    /// </summary>
    public partial class Offers
    {
        /// <summary>
        /// Initializes a new instance of the Offers class.
        /// </summary>
        public Offers() { }

        /// <summary>
        /// Initializes a new instance of the Offers class.
        /// </summary>
        public Offers(string href = default(string), int? limit = default(int?), string next = default(string), IList<EbayOfferDetailsWithAll> offersProperty = default(IList<EbayOfferDetailsWithAll>), string prev = default(string), int? size = default(int?), int? total = default(int?))
        {
            Href = href;
            Limit = limit;
            Next = next;
            OffersProperty = offersProperty;
            Prev = prev;
            Size = size;
            Total = total;
        }

        /// <summary>
        /// This is the URL to the current page of offers.
        /// </summary>
        [JsonProperty(PropertyName = "href")]
        public string Href { get; set; }

        /// <summary>
        /// This integer value is the number of offers that will be displayed
        /// on each results page.
        /// </summary>
        [JsonProperty(PropertyName = "limit")]
        public int? Limit { get; set; }

        /// <summary>
        /// This is the URL to the next page of offers. This field will only
        /// be returned if there are additional offers to view.
        /// </summary>
        [JsonProperty(PropertyName = "next")]
        public string Next { get; set; }

        /// <summary>
        /// This container is an array of one or more of the seller's offers
        /// for the SKU value that is passed in through the required sku
        /// query parameter. Note: Currently, the Inventory API does not
        /// support the same SKU across multiple eBay marketplaces, so the
        /// getOffers call will only return one offer. Max Occurs: 25
        /// </summary>
        [JsonProperty(PropertyName = "offers")]
        public IList<EbayOfferDetailsWithAll> OffersProperty { get; set; }

        /// <summary>
        /// This is the URL to the previous page of offers. This field will
        /// only be returned if there are previous offers to view.
        /// </summary>
        [JsonProperty(PropertyName = "prev")]
        public string Prev { get; set; }

        /// <summary>
        /// This integer value indicates the number of offers being displayed
        /// on the current page of results. This number will generally be the
        /// same as the limit value if there are additional pages of results
        /// to view. Note: Currently, the Inventory API does not support the
        /// same SKU across multiple eBay marketplaces, so the Get Offers
        /// call will only return one offer, so this value should always be 1.
        /// </summary>
        [JsonProperty(PropertyName = "size")]
        public int? Size { get; set; }

        /// <summary>
        /// This integer value is the total number of offers that exist for
        /// the specified SKU value. Based on this number and on the limit
        /// value, the seller may have to toggle through multiple pages to
        /// view all offers. Note: Currently, the Inventory API does not
        /// support the same SKU across multiple eBay marketplaces, so the
        /// Get Offers call will only return one offer, so this value should
        /// always be 1.
        /// </summary>
        [JsonProperty(PropertyName = "total")]
        public int? Total { get; set; }

    }
}
