﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace ebayinventory.Models
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Microsoft.Rest;
    using Microsoft.Rest.Serialization;

    /// <summary>
    /// This type is used to express expected listing fees that the seller may
    /// incur for one or more unpublished offers, as well as any eBay-related
    /// promotional discounts being applied toward a specific fee. These fees
    /// are the expected cumulative fees per eBay marketplace (which is
    /// indicated in the &lt;strong&gt;marketplaceId&lt;/strong&gt; field).
    /// </summary>
    public partial class Fee
    {
        /// <summary>
        /// Initializes a new instance of the Fee class.
        /// </summary>
        public Fee() { }

        /// <summary>
        /// Initializes a new instance of the Fee class.
        /// </summary>
        public Fee(Amount amount = default(Amount), string feeType = default(string), Amount promotionalDiscount = default(Amount))
        {
            Amount = amount;
            FeeType = feeType;
            PromotionalDiscount = promotionalDiscount;
        }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "amount")]
        public Amount Amount { get; set; }

        /// <summary>
        /// The value returned in this field indicates the type of listing fee
        /// that the seller may incur if one or more unpublished offers
        /// (offers are specified in the call request) are published on the
        /// marketplace specified in the marketplaceId field. Applicable
        /// listing fees will often include things such as InsertionFee or
        /// SubtitleFee, but many fee types will get returned even when they
        /// are 0.0. See the Standard selling fees help page for more
        /// information on listing fees.
        /// </summary>
        [JsonProperty(PropertyName = "feeType")]
        public string FeeType { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "promotionalDiscount")]
        public Amount PromotionalDiscount { get; set; }

    }
}
