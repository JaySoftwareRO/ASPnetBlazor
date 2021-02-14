﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace ebayinventory.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// This type is used by the &lt;strong&gt;listing&lt;/strong&gt;
    /// container in the &lt;strong&gt;getOffer&lt;/strong&gt; and
    /// &lt;strong&gt;getOffers&lt;/strong&gt; calls to provide the eBay
    /// listing ID, the listing status, and quantity sold for the offer. The
    /// &lt;strong&gt;listing&lt;/strong&gt; container is only returned for
    /// published offers, and is not returned for unpublished offers.
    /// </summary>
    public partial class ListingDetails
    {
        /// <summary>
        /// Initializes a new instance of the ListingDetails class.
        /// </summary>
        public ListingDetails() { }

        /// <summary>
        /// Initializes a new instance of the ListingDetails class.
        /// </summary>
        public ListingDetails(string listingId = default(string), string listingStatus = default(string), int? soldQuantity = default(int?))
        {
            ListingId = listingId;
            ListingStatus = listingStatus;
            SoldQuantity = soldQuantity;
        }

        /// <summary>
        /// The unique identifier of the eBay listing that is associated with
        /// the published offer.
        /// </summary>
        [JsonProperty(PropertyName = "listingId")]
        public string ListingId { get; set; }

        /// <summary>
        /// The enumeration value returned in this field indicates the status
        /// of the listing that is associated with the published offer. For
        /// implementation help, refer to &lt;a
        /// href='https://developer.ebay.com/devzone/rest/api-ref/inventory/types/ListingStatusEnum.html'&gt;eBay
        /// API documentation&lt;/a&gt;
        /// </summary>
        [JsonProperty(PropertyName = "listingStatus")]
        public string ListingStatus { get; set; }

        /// <summary>
        /// This integer value indicates the quantity of the product that has
        /// been sold for the published offer.
        /// </summary>
        [JsonProperty(PropertyName = "soldQuantity")]
        public int? SoldQuantity { get; set; }

    }
}
