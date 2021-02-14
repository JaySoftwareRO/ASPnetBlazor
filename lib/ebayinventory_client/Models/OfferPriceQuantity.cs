﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace ebayinventory.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// This type is used by the &lt;strong&gt;offers&lt;/strong&gt; container
    /// in a &lt;strong&gt;Bulk Update Price and Quantity&lt;/strong&gt; call
    /// to update the current price and/or quantity of one or more offers
    /// associated with a specific inventory item.
    /// </summary>
    public partial class OfferPriceQuantity
    {
        /// <summary>
        /// Initializes a new instance of the OfferPriceQuantity class.
        /// </summary>
        public OfferPriceQuantity() { }

        /// <summary>
        /// Initializes a new instance of the OfferPriceQuantity class.
        /// </summary>
        public OfferPriceQuantity(int? availableQuantity = default(int?), string offerId = default(string), Amount price = default(Amount))
        {
            AvailableQuantity = availableQuantity;
            OfferId = offerId;
            Price = price;
        }

        /// <summary>
        /// This field is used if the seller wants to modify the current
        /// quantity of the inventory item that will be available for
        /// purchase in the offer (identified by the corresponding offerId
        /// value). Either the availableQuantity field or the price container
        /// is required, but not necessarily both.
        /// </summary>
        [JsonProperty(PropertyName = "availableQuantity")]
        public int? AvailableQuantity { get; set; }

        /// <summary>
        /// This field is the unique identifier of the offer. If an offers
        /// container is used to update one or more offers associated to a
        /// specific inventory item, the offerId value is required in order
        /// to identify the offer to update with a modified price and/or
        /// quantity. The seller can run a getOffers call (passing in the
        /// correct SKU value as a query parameter) to retrieve offerId
        /// values for offers associated with the SKU.
        /// </summary>
        [JsonProperty(PropertyName = "offerId")]
        public string OfferId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public Amount Price { get; set; }

    }
}
