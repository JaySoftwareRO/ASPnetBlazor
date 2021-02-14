﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace ebaytaxonomy.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This type contains information about all nodes of a specified eBay
    /// category tree.
    /// </summary>
    public partial class CategoryTree
    {
        /// <summary>
        /// Initializes a new instance of the CategoryTree class.
        /// </summary>
        public CategoryTree() { }

        /// <summary>
        /// Initializes a new instance of the CategoryTree class.
        /// </summary>
        public CategoryTree(IList<string> applicableMarketplaceIds = default(IList<string>), string categoryTreeId = default(string), string categoryTreeVersion = default(string), CategoryTreeNode rootCategoryNode = default(CategoryTreeNode))
        {
            ApplicableMarketplaceIds = applicableMarketplaceIds;
            CategoryTreeId = categoryTreeId;
            CategoryTreeVersion = categoryTreeVersion;
            RootCategoryNode = rootCategoryNode;
        }

        /// <summary>
        /// A list of one or more identifiers of the eBay marketplaces that
        /// use this category tree.
        /// </summary>
        [JsonProperty(PropertyName = "applicableMarketplaceIds")]
        public IList<string> ApplicableMarketplaceIds { get; set; }

        /// <summary>
        /// The unique identifier of this eBay category tree.
        /// </summary>
        [JsonProperty(PropertyName = "categoryTreeId")]
        public string CategoryTreeId { get; set; }

        /// <summary>
        /// The version of this category tree. It's a good idea to cache this
        /// value for comparison so you can determine if this category tree
        /// has been modified in subsequent calls.
        /// </summary>
        [JsonProperty(PropertyName = "categoryTreeVersion")]
        public string CategoryTreeVersion { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "rootCategoryNode")]
        public CategoryTreeNode RootCategoryNode { get; set; }

    }
}
