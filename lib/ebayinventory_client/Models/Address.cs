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
    /// This type is used to define the physical address of an inventory
    /// location.
    /// </summary>
    public partial class Address
    {
        /// <summary>
        /// Initializes a new instance of the Address class.
        /// </summary>
        public Address() { }

        /// <summary>
        /// Initializes a new instance of the Address class.
        /// </summary>
        public Address(string addressLine1 = default(string), string addressLine2 = default(string), string city = default(string), string country = default(string), string county = default(string), string postalCode = default(string), string stateOrProvince = default(string))
        {
            AddressLine1 = addressLine1;
            AddressLine2 = addressLine2;
            City = city;
            Country = country;
            County = county;
            PostalCode = postalCode;
            StateOrProvince = stateOrProvince;
        }

        /// <summary>
        /// The first line of a street address. This field is required for
        /// store inventory locations that will be holding In-Store Pickup
        /// inventory. A street address is not required if the inventory
        /// location is not holding In-Store Pickup Inventory. This field
        /// will be returned if defined for an inventory location. Max
        /// length: 128
        /// </summary>
        [JsonProperty(PropertyName = "addressLine1")]
        public string AddressLine1 { get; set; }

        /// <summary>
        /// The second line of a street address. This field can be used for
        /// additional address information, such as a suite or apartment
        /// number. A street address is not required if the inventory
        /// location is not holding In-Store Pickup Inventory. This field
        /// will be returned if defined for an inventory location. Max
        /// length: 128
        /// </summary>
        [JsonProperty(PropertyName = "addressLine2")]
        public string AddressLine2 { get; set; }

        /// <summary>
        /// The city in which the inventory location resides. This field is
        /// required for store inventory locations that will be holding
        /// In-Store Pickup inventory. For warehouse locations, this field is
        /// technically optional, as a postalCode can be used instead of
        /// city/stateOrProvince pair, and then the city is just derived from
        /// this postal/zip code. This field is returned if defined for an
        /// inventory location. Max length: 128
        /// </summary>
        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        /// <summary>
        /// The country in which the address resides, represented as
        /// two-letter ISO 3166 country code. For example, US represents the
        /// United States, and DE represents Germany. Max length: 2 For
        /// implementation help, refer to &lt;a
        /// href='https://developer.ebay.com/devzone/rest/api-ref/inventory/types/CountryCodeEnum".html'&gt;eBay
        /// API documentation&lt;/a&gt;
        /// </summary>
        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }

        /// <summary>
        /// The county in which the address resides. This field is returned if
        /// defined for an inventory location.
        /// </summary>
        [JsonProperty(PropertyName = "county")]
        public string County { get; set; }

        /// <summary>
        /// The postal/zip code of the address. eBay uses postal codes to
        /// surface In-Store Pickup items within the vicinity of a buyer's
        /// location, and it also user postal codes (origin and destination)
        /// to estimate shipping costs when the seller uses calculated
        /// shipping. A city/stateOrProvince pair can be used instead of a
        /// postalCode value, and then the postal code is just derived from
        /// the city and state/province. This field is returned if defined
        /// for an inventory location. Max length: 16
        /// </summary>
        [JsonProperty(PropertyName = "postalCode")]
        public string PostalCode { get; set; }

        /// <summary>
        /// The state/province in which the inventory location resides. This
        /// field is required for store inventory locations that will be
        /// holding In-Store Pickup inventory. For warehouse locations, this
        /// field is technically optional, as a postalCode can be used
        /// instead of city/stateOrProvince pair, and then the state or
        /// province is just derived from this postal/zip code. Max length:
        /// 128
        /// </summary>
        [JsonProperty(PropertyName = "stateOrProvince")]
        public string StateOrProvince { get; set; }

    }
}
