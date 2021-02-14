﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace ebayinventory.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This type is used to display the expected listing fees for each
    /// unpublished offer specified in the request of the
    /// &lt;strong&gt;getListingFees&lt;/strong&gt; call.
    /// </summary>
    public partial class FeeSummary
    {
        /// <summary>
        /// Initializes a new instance of the FeeSummary class.
        /// </summary>
        public FeeSummary() { }

        /// <summary>
        /// Initializes a new instance of the FeeSummary class.
        /// </summary>
        public FeeSummary(IList<Fee> fees = default(IList<Fee>), string marketplaceId = default(string), IList<Error> warnings = default(IList<Error>))
        {
            Fees = fees;
            MarketplaceId = marketplaceId;
            Warnings = warnings;
        }

        /// <summary>
        /// This container is an array of listing fees that can be expected to
        /// be applied to an offer on the specified eBay marketplace
        /// (marketplaceId value). Many fee types will get returned even when
        /// they are 0.0. See the Standard selling fees help page for more
        /// information on listing fees.
        /// </summary>
        [JsonProperty(PropertyName = "fees")]
        public IList<Fee> Fees { get; set; }

        /// <summary>
        /// This is the unique identifier of the eBay site for which listing
        /// fees for the offer are applicable. For implementation help, refer
        /// to &lt;a
        /// href='https://developer.ebay.com/devzone/rest/api-ref/inventory/types/MarketplaceEnum.html'&gt;eBay
        /// API documentation&lt;/a&gt;
        /// </summary>
        [JsonProperty(PropertyName = "marketplaceId")]
        public string MarketplaceId { get; set; }

        /// <summary>
        /// This container will contain an array of errors and/or warnings
        /// when a call is made, and errors and/or warnings occur.
        /// </summary>
        [JsonProperty(PropertyName = "warnings")]
        public IList<Error> Warnings { get; set; }

    }
}
