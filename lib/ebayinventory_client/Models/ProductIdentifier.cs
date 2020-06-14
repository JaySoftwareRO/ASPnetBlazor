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
    /// This type is used to identify a motor vehicle that is compatible with
    /// the corresponding inventory item (the SKU that is passed in as part
    /// of the call URI). The motor vehicle can be identified through an eBay
    /// Product ID or a K-Type value. The &lt;strong&gt;gtin&lt;/strong&gt;
    /// field (for inputting Global Trade Item Numbers) is for future use
    /// only. If a motor vehicle is found in the eBay product catalog, the
    /// motor vehicle properties (engine, make, model, trim, and year) will
    /// automatically get picked up for that motor
    /// vehicle.&lt;br/&gt;&lt;br/&gt;&lt;span class="tablenote"&gt;
    /// &lt;strong&gt;Note:&lt;/strong&gt; Currently, parts compatibility is
    /// only applicable for motor vehicles, but it is possible that the
    /// Product Compatibility feature is expanded to other (non-vehicle)
    /// products in the future.&lt;/span&gt;
    /// </summary>
    public partial class ProductIdentifier
    {
        /// <summary>
        /// Initializes a new instance of the ProductIdentifier class.
        /// </summary>
        public ProductIdentifier() { }

        /// <summary>
        /// Initializes a new instance of the ProductIdentifier class.
        /// </summary>
        public ProductIdentifier(string epid = default(string), string gtin = default(string), string ktype = default(string))
        {
            Epid = epid;
            Gtin = gtin;
            Ktype = ktype;
        }

        /// <summary>
        /// This field can be used if the seller already knows the eBay
        /// catalog product ID (ePID) associated with the motor vehicle that
        /// is to be added to the compatible product list. If this eBay
        /// catalog product ID is found in the eBay product catalog, the
        /// motor vehicle properties (e.g. make, model, year, engine, and
        /// trim) will automatically get picked up for that motor vehicle.
        /// </summary>
        [JsonProperty(PropertyName = "epid")]
        public string Epid { get; set; }

        /// <summary>
        /// This field can be used if the seller knows the Global Trade Item
        /// Number for the motor vehicle that is to be added to the
        /// compatible product list. If this GTIN value is found in the eBay
        /// product catalog, the motor vehicle properties (e.g. make, model,
        /// year, engine, and trim will automatically get picked up for that
        /// motor vehicle. Note: This field is for future use.
        /// </summary>
        [JsonProperty(PropertyName = "gtin")]
        public string Gtin { get; set; }

        /// <summary>
        /// This field can be used if the seller knows the K Type Number for
        /// the motor vehicle that is to be added to the compatible product
        /// list. If this K Type value is found in the eBay product catalog,
        /// the motor vehicle properties (e.g. make, model, year, engine, and
        /// trim) will automatically get picked up for that motor vehicle.
        /// Only the DE, UK, and AU sites support the use of K Type Numbers.
        /// </summary>
        [JsonProperty(PropertyName = "ktype")]
        public string Ktype { get; set; }

    }
}
