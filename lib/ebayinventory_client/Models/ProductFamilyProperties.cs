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
    /// This type is used to specify the details of a motor vehicle that is
    /// compatible with the inventory item specified through the SKU value in
    /// the call URI.
    /// </summary>
    public partial class ProductFamilyProperties
    {
        /// <summary>
        /// Initializes a new instance of the ProductFamilyProperties class.
        /// </summary>
        public ProductFamilyProperties() { }

        /// <summary>
        /// Initializes a new instance of the ProductFamilyProperties class.
        /// </summary>
        public ProductFamilyProperties(string engine = default(string), string make = default(string), string model = default(string), string trim = default(string), string year = default(string))
        {
            Engine = engine;
            Make = make;
            Model = model;
            Trim = trim;
            Year = year;
        }

        /// <summary>
        /// This field indicates the specifications of the engine, including
        /// its size, block type, and fuel type. An example is 2.7L V6 gas
        /// DOHC naturally aspirated. This field is conditionally required,
        /// but should be supplied if known/applicable.
        /// </summary>
        [JsonProperty(PropertyName = "engine")]
        public string Engine { get; set; }

        /// <summary>
        /// This field indicates the make of the vehicle (e.g. Toyota). This
        /// field is always required to identify a motor vehicle.
        /// </summary>
        [JsonProperty(PropertyName = "make")]
        public string Make { get; set; }

        /// <summary>
        /// This field indicates the model of the vehicle (e.g. Camry). This
        /// field is always required to identify a motor vehicle.
        /// </summary>
        [JsonProperty(PropertyName = "model")]
        public string Model { get; set; }

        /// <summary>
        /// This field indicates the trim of the vehicle (e.g. 2-door Coupe).
        /// This field is conditionally required, but should be supplied if
        /// known/applicable.
        /// </summary>
        [JsonProperty(PropertyName = "trim")]
        public string Trim { get; set; }

        /// <summary>
        /// This field indicates the year of the vehicle (e.g. 2016). This
        /// field is always required to identify a motor vehicle.
        /// </summary>
        [JsonProperty(PropertyName = "year")]
        public string Year { get; set; }

    }
}
