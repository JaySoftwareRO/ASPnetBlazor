﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace ebayinventory.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// This type is used to specify the total 'ship-to-home' quantity of the
    /// inventory item that will be available for purchase through one or
    /// more published offers.
    /// </summary>
    public partial class ShipToLocationAvailability
    {
        /// <summary>
        /// Initializes a new instance of the ShipToLocationAvailability class.
        /// </summary>
        public ShipToLocationAvailability() { }

        /// <summary>
        /// Initializes a new instance of the ShipToLocationAvailability class.
        /// </summary>
        public ShipToLocationAvailability(int? quantity = default(int?))
        {
            Quantity = quantity;
        }

        /// <summary>
        /// This container is used to set the total 'ship-to-home' quantity of
        /// the inventory item that will be available for purchase through
        /// one or more published offers. This container is not immediately
        /// required, but 'ship-to-home' quantity must be set before an offer
        /// of the inventory item can be published. If an existing inventory
        /// item is being updated, and the 'ship-to-home' quantity already
        /// exists for the inventory item record, this container should be
        /// included again, even if the value is not changing, or the
        /// available quantity data will be lost.
        /// </summary>
        [JsonProperty(PropertyName = "quantity")]
        public int? Quantity { get; set; }

    }
}
