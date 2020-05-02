﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace ebaytaxonomy.Models
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Microsoft.Rest;
    using Microsoft.Rest.Serialization;

    /// <summary>
    /// The base response type of the
    /// &lt;strong&gt;getCompatibilityPropertyValues&lt;/strong&gt; method.
    /// </summary>
    public partial class GetCompatibilityPropertyValuesResponse
    {
        /// <summary>
        /// Initializes a new instance of the
        /// GetCompatibilityPropertyValuesResponse class.
        /// </summary>
        public GetCompatibilityPropertyValuesResponse() { }

        /// <summary>
        /// Initializes a new instance of the
        /// GetCompatibilityPropertyValuesResponse class.
        /// </summary>
        public GetCompatibilityPropertyValuesResponse(IList<CompatibilityPropertyValue> compatibilityPropertyValues = default(IList<CompatibilityPropertyValue>))
        {
            CompatibilityPropertyValues = compatibilityPropertyValues;
        }

        /// <summary>
        /// This array contains all compatible vehicle property values that
        /// match the specified eBay marketplace, specified eBay category,
        /// and filters in the request. If the compatibility_property
        /// parameter value in the request is 'Trim', each value returned in
        /// each value field will be a different vehicle trim, applicable to
        /// any filters that are set in the filter query parameter of the
        /// request, and also based on the eBay marketplace and category
        /// specified in the call request.
        /// </summary>
        [JsonProperty(PropertyName = "compatibilityPropertyValues")]
        public IList<CompatibilityPropertyValue> CompatibilityPropertyValues { get; set; }

    }
}
