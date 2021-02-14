﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace ebayinventory.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// This type is used by the &lt;strong&gt;fulfillmentTime&lt;/strong&gt;
    /// container of the
    /// &lt;strong&gt;PickupAtLocationAvailability&lt;/strong&gt; type to
    /// specify how soon orders will be ready for In-Store pickup after the
    /// purchase has occurred.
    /// </summary>
    public partial class TimeDuration
    {
        /// <summary>
        /// Initializes a new instance of the TimeDuration class.
        /// </summary>
        public TimeDuration() { }

        /// <summary>
        /// Initializes a new instance of the TimeDuration class.
        /// </summary>
        public TimeDuration(string unit = default(string), int? value = default(int?))
        {
            Unit = unit;
            Value = value;
        }

        /// <summary>
        /// This enumeration value indicates the time unit used to specify the
        /// fulfillment time, such as HOUR. For implementation help, refer to
        /// &lt;a
        /// href='https://developer.ebay.com/devzone/rest/api-ref/inventory/types/TimeDurationUnitEnum.html'&gt;eBay
        /// API documentation&lt;/a&gt;
        /// </summary>
        [JsonProperty(PropertyName = "unit")]
        public string Unit { get; set; }

        /// <summary>
        /// The integer value in this field, along with the time unit in the
        /// unit field, will indicate how soon after an In-Store Pickup
        /// purchase can the buyer pick up the item at the designated store
        /// location. If the value of this field is 4, and the value of the
        /// unit field is HOUR, then the fulfillment time for the In-Store
        /// Pickup order is four hours, which means that the buyer will be
        /// able to pick up the item at the store four hours after the
        /// transaction took place.
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public int? Value { get; set; }

    }
}
