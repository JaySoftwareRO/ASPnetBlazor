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
    /// This type is used to update the total "ship-to-home"  quantity for one
    /// or more inventory items and/or to update the price and/or quantity of
    /// one or more specific offers associated with one or more inventory
    /// items.
    /// </summary>
    public partial class PriceQuantity
    {
        /// <summary>
        /// Initializes a new instance of the PriceQuantity class.
        /// </summary>
        public PriceQuantity() { }

        /// <summary>
        /// Initializes a new instance of the PriceQuantity class.
        /// </summary>
        public PriceQuantity(IList<OfferPriceQuantity> offers = default(IList<OfferPriceQuantity>), ShipToLocationAvailability shipToLocationAvailability = default(ShipToLocationAvailability), string sku = default(string))
        {
            Offers = offers;
            ShipToLocationAvailability = shipToLocationAvailability;
            Sku = sku;
        }

        /// <summary>
        /// This container is needed if the seller is updating the price
        /// and/or quantity of one or more published offers, and a successful
        /// call will actually update the active eBay listing with the
        /// revised price and/or available quantity. This call is not
        /// designed to work with unpublished offers. For unpublished offers,
        /// the seller should use the updateOffer call to update the
        /// available quantity and/or price. If the seller is also using the
        /// shipToLocationAvailability container and sku field to update the
        /// total 'ship-to-home' quantity of the inventory item, the SKU
        /// value associated with the corresponding offerId value(s) must be
        /// the same as the corresponding sku value that is passed in, or an
        /// error will occur. A separate (OfferPriceQuantity) node is
        /// required for each offer being updated.
        /// </summary>
        [JsonProperty(PropertyName = "offers")]
        public IList<OfferPriceQuantity> Offers { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "shipToLocationAvailability")]
        public ShipToLocationAvailability ShipToLocationAvailability { get; set; }

        /// <summary>
        /// This is the seller-defined SKU value of the inventory item whose
        /// total 'ship-to-home' quantity will be updated. This field is only
        /// required when the seller is updating the total quantity of an
        /// inventory item using the shipToLocationAvailability container. If
        /// the seller is updating the price and/or quantity of one or more
        /// specific offers, one or more offerId values are used instead, and
        /// the sku value is not needed. If the seller wants to update the
        /// price and/or quantity of one or more offers, and also wants to
        /// update the total 'ship-to-home' quantity of the corresponding
        /// inventory item, the SKU value associated with the offerId
        /// value(s) must be the same as the corresponding sku value that is
        /// passed in, or an error will occur. Max Length: 50
        /// </summary>
        [JsonProperty(PropertyName = "sku")]
        public string Sku { get; set; }

    }
}
