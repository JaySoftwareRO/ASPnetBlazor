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
    /// This type contains an array of suggested category tree nodes that are
    /// considered by eBay to most closely correspond to the keywords
    /// provided in a query string, from a specified category tree.
    /// </summary>
    public partial class CategorySuggestionResponse
    {
        /// <summary>
        /// Initializes a new instance of the CategorySuggestionResponse class.
        /// </summary>
        public CategorySuggestionResponse() { }

        /// <summary>
        /// Initializes a new instance of the CategorySuggestionResponse class.
        /// </summary>
        public CategorySuggestionResponse(IList<CategorySuggestion> categorySuggestions = default(IList<CategorySuggestion>), string categoryTreeId = default(string), string categoryTreeVersion = default(string))
        {
            CategorySuggestions = categorySuggestions;
            CategoryTreeId = categoryTreeId;
            CategoryTreeVersion = categoryTreeVersion;
        }

        /// <summary>
        /// Contains details about one or more suggested categories that
        /// correspond to the provided keywords. The array of suggested
        /// categories is sorted in order of eBay's confidence of the
        /// relevance of each category (the first category is the most
        /// relevant). Important: This call is not supported in the Sandbox
        /// environment. It will return a response payload in which the
        /// categoryName fields contain random or boilerplate text regardless
        /// of the query submitted.
        /// </summary>
        [JsonProperty(PropertyName = "categorySuggestions")]
        public IList<CategorySuggestion> CategorySuggestions { get; set; }

        /// <summary>
        /// The unique identifier of the eBay category tree from which
        /// suggestions are returned.
        /// </summary>
        [JsonProperty(PropertyName = "categoryTreeId")]
        public string CategoryTreeId { get; set; }

        /// <summary>
        /// The version of the category tree identified by categoryTreeId.
        /// It's a good idea to cache this value for comparison so you can
        /// determine if this category tree has been modified in subsequent
        /// calls.
        /// </summary>
        [JsonProperty(PropertyName = "categoryTreeVersion")]
        public string CategoryTreeVersion { get; set; }

    }
}
