﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace ebayinventory.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This type is use by the base response payload of the
    /// &lt;strong&gt;bulkUpdatePriceQuantity&lt;/strong&gt; call. The
    /// &lt;strong&gt;bulkUpdatePriceQuantity&lt;/strong&gt; call response
    /// will return an HTTP status code, offer ID, and SKU value for each
    /// offer/inventory item being updated, as well as an
    /// &lt;strong&gt;errors&lt;/strong&gt; and/or
    /// &lt;strong&gt;warnings&lt;/strong&gt; container if any errors or
    /// warnings are triggered while trying to update those offers/inventory
    /// items.
    /// </summary>
    public partial class BulkPriceQuantityResponse
    {
        /// <summary>
        /// Initializes a new instance of the BulkPriceQuantityResponse class.
        /// </summary>
        public BulkPriceQuantityResponse() { }

        /// <summary>
        /// Initializes a new instance of the BulkPriceQuantityResponse class.
        /// </summary>
        public BulkPriceQuantityResponse(IList<PriceQuantityResponse> responses = default(IList<PriceQuantityResponse>))
        {
            Responses = responses;
        }

        /// <summary>
        /// This container will return an HTTP status code, offer ID, and SKU
        /// value for each offer/inventory item being updated, as well as an
        /// errors and/or warnings container if any errors or warnings are
        /// triggered while trying to update those offers/inventory items.
        /// </summary>
        [JsonProperty(PropertyName = "responses")]
        public IList<PriceQuantityResponse> Responses { get; set; }

    }
}
