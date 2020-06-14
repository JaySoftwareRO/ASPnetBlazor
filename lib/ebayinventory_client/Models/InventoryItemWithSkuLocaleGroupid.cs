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

    public partial class InventoryItemWithSkuLocaleGroupid
    {
        /// <summary>
        /// Initializes a new instance of the
        /// InventoryItemWithSkuLocaleGroupid class.
        /// </summary>
        public InventoryItemWithSkuLocaleGroupid() { }

        /// <summary>
        /// Initializes a new instance of the
        /// InventoryItemWithSkuLocaleGroupid class.
        /// </summary>
        public InventoryItemWithSkuLocaleGroupid(Availability availability = default(Availability), string condition = default(string), string conditionDescription = default(string), IList<string> groupIds = default(IList<string>), IList<string> inventoryItemGroupKeys = default(IList<string>), string locale = default(string), PackageWeightAndSize packageWeightAndSize = default(PackageWeightAndSize), Product product = default(Product), string sku = default(string))
        {
            Availability = availability;
            Condition = condition;
            ConditionDescription = conditionDescription;
            GroupIds = groupIds;
            InventoryItemGroupKeys = inventoryItemGroupKeys;
            Locale = locale;
            PackageWeightAndSize = packageWeightAndSize;
            Product = product;
            Sku = sku;
        }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "availability")]
        public Availability Availability { get; set; }

        /// <summary>
        /// This enumeration value indicates the condition of the item.
        /// Supported item condition values will vary by eBay site and
        /// category. To see which item condition values that a particular
        /// eBay category supports, use the getItemConditionPolicies method
        /// of the Metadata API. This method returns condition ID values that
        /// map to the enumeration values defined in the ConditionEnum type.
        /// The Item condition ID and name values topic in the Selling
        /// Integration Guide has a table that maps condition ID values to
        /// ConditionEnum values. The getItemConditionPolicies call reference
        /// page has more information. Since the condition of an inventory
        /// item must be specified before being published in an offer, this
        /// field is always returned in the 'Get' calls for SKUs that are
        /// part of a published offer. If a SKU is not part of a published
        /// offer, this container will only be returned if set for the
        /// inventory item. For implementation help, refer to &lt;a
        /// href='https://developer.ebay.com/devzone/rest/api-ref/inventory/types/ConditionEnum.html'&gt;eBay
        /// API documentation&lt;/a&gt;
        /// </summary>
        [JsonProperty(PropertyName = "condition")]
        public string Condition { get; set; }

        /// <summary>
        /// This string field is used by the seller to more clearly describe
        /// the condition of used items, or items that are not 'Brand New',
        /// 'New with tags', or 'New in box'. The ConditionDescription field
        /// is available for all categories. If the ConditionDescription
        /// field is used with an item in a new condition (Condition IDs
        /// 1000-1499), eBay will simply ignore this field if included, and
        /// eBay will return a warning message to the user. This field should
        /// only be used to further clarify the condition of the used item.
        /// It should not be used for branding, promotions, shipping,
        /// returns, payment or other information unrelated to the condition
        /// of the item. Make sure that the condition value, condition
        /// description, listing description, and the item's pictures do not
        /// contradict one another. Max length/: 1000.
        /// </summary>
        [JsonProperty(PropertyName = "conditionDescription")]
        public string ConditionDescription { get; set; }

        /// <summary>
        /// This array is returned if the inventory item is associated with
        /// any inventory item group(s). The value(s) returned in this array
        /// are the unique identifier(s) of the inventory item group(s). This
        /// array is not returned if the inventory item is not associated
        /// with any inventory item groups.
        /// </summary>
        [JsonProperty(PropertyName = "groupIds")]
        public IList<string> GroupIds { get; set; }

        /// <summary>
        /// This array is returned if the inventory item is associated with
        /// any inventory item group(s). The value(s) returned in this array
        /// are the unique identifier(s) of the inventory item's variation in
        /// a multiple-variation listing. This array is not returned if the
        /// inventory item is not associated with any inventory item groups.
        /// </summary>
        [JsonProperty(PropertyName = "inventoryItemGroupKeys")]
        public IList<string> InventoryItemGroupKeys { get; set; }

        /// <summary>
        /// This field is for future use only. For implementation help, refer
        /// to &lt;a
        /// href='https://developer.ebay.com/devzone/rest/api-ref/inventory/types/LocaleEnum.html'&gt;eBay
        /// API documentation&lt;/a&gt;
        /// </summary>
        [JsonProperty(PropertyName = "locale")]
        public string Locale { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "packageWeightAndSize")]
        public PackageWeightAndSize PackageWeightAndSize { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "product")]
        public Product Product { get; set; }

        /// <summary>
        /// The seller-defined Stock-Keeping Unit (SKU) of the inventory item.
        /// The seller should have a unique SKU value for every product that
        /// they sell.
        /// </summary>
        [JsonProperty(PropertyName = "sku")]
        public string Sku { get; set; }

    }
}
