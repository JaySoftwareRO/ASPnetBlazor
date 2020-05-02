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
    /// This type is used by the
    /// &lt;strong&gt;compatibilityProperties&lt;/strong&gt; array that is
    /// returned in the
    /// &lt;strong&gt;getCompatibilityProperties&lt;/strong&gt; call. The
    /// &lt;strong&gt;compatibilityProperties&lt;/strong&gt; container
    /// consists of an array of all compatible vehicle properties applicable
    /// to the specified eBay marketplace and eBay category ID.
    /// </summary>
    public partial class CompatibilityProperty
    {
        /// <summary>
        /// Initializes a new instance of the CompatibilityProperty class.
        /// </summary>
        public CompatibilityProperty() { }

        /// <summary>
        /// Initializes a new instance of the CompatibilityProperty class.
        /// </summary>
        public CompatibilityProperty(string name = default(string), string localizedName = default(string))
        {
            Name = name;
            LocalizedName = localizedName;
        }

        /// <summary>
        /// This is the actual name of the compatible vehicle property as it
        /// is known on the specified eBay marketplace and in the eBay
        /// category. This is the string value that should be used in the
        /// compatibility_property and filter query parameters of a
        /// getCompatibilityPropertyValues request URI. Typical vehicle
        /// properties are 'Make', 'Model', 'Year', 'Engine', and 'Trim', but
        /// will vary based on the eBay marketplace and the eBay category.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// This is the localized name of the compatible vehicle property. The
        /// language that is used will depend on the user making the call, or
        /// based on the language specified if the Content-Language HTTP
        /// header is used. In some instances, the string value in this field
        /// may be the same as the string in the corresponding name field.
        /// </summary>
        [JsonProperty(PropertyName = "localizedName")]
        public string LocalizedName { get; set; }

    }
}
