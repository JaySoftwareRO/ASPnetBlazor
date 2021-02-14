﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace ebayinventory.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// This type is used by the request payload of the
    /// &lt;strong&gt;publishByInventoryItemGroup&lt;/strong&gt; call. The
    /// identifier of the inventory item group to publish and the eBay
    /// marketplace where the listing will be published is needed in the
    /// request payload.
    /// </summary>
    public partial class PublishByInventoryItemGroupRequest
    {
        /// <summary>
        /// Initializes a new instance of the
        /// PublishByInventoryItemGroupRequest class.
        /// </summary>
        public PublishByInventoryItemGroupRequest() { }

        /// <summary>
        /// Initializes a new instance of the
        /// PublishByInventoryItemGroupRequest class.
        /// </summary>
        public PublishByInventoryItemGroupRequest(string inventoryItemGroupKey = default(string), string marketplaceId = default(string))
        {
            InventoryItemGroupKey = inventoryItemGroupKey;
            MarketplaceId = marketplaceId;
        }

        /// <summary>
        /// This is the unique identifier of the inventory item group. All
        /// unpublished offers associated with this inventory item group will
        /// be published as a multiple-variation listing if the
        /// publishByInventoryItemGroup call is successful. The
        /// inventoryItemGroupKey identifier is automatically generated by
        /// eBay once an inventory item group is created. To retrieve an
        /// inventoryItemGroupKey value, you can use the getInventoryItem
        /// call to retrieve an inventory item that is known to be in the
        /// inventory item group to publish, and then look for the inventory
        /// item group identifier under the groupIds container in the
        /// response of that call.
        /// </summary>
        [JsonProperty(PropertyName = "inventoryItemGroupKey")]
        public string InventoryItemGroupKey { get; set; }

        /// <summary>
        /// This is the unique identifier of the eBay site on which the
        /// multiple-variation listing will be published. The marketPlaceId
        /// enumeration values are found in MarketplaceIdEnum. For
        /// implementation help, refer to &lt;a
        /// href='https://developer.ebay.com/devzone/rest/api-ref/inventory/types/MarketplaceEnum.html'&gt;eBay
        /// API documentation&lt;/a&gt;
        /// </summary>
        [JsonProperty(PropertyName = "marketplaceId")]
        public string MarketplaceId { get; set; }

    }
}
