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
    /// This type is used to express the Global Positioning System (GPS)
    /// latitude and longitude coordinates of an inventory location.
    /// </summary>
    public partial class GeoCoordinates
    {
        /// <summary>
        /// Initializes a new instance of the GeoCoordinates class.
        /// </summary>
        public GeoCoordinates() { }

        /// <summary>
        /// Initializes a new instance of the GeoCoordinates class.
        /// </summary>
        public GeoCoordinates(double? latitude = default(double?), double? longitude = default(double?))
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        /// <summary>
        /// The latitude (North-South) component of the geographic coordinate.
        /// This field is required if a geoCoordinates container is used.
        /// This field is returned if geographical coordinates are set for
        /// the inventory location.
        /// </summary>
        [JsonProperty(PropertyName = "latitude")]
        public double? Latitude { get; set; }

        /// <summary>
        /// The longitude (East-West) component of the geographic coordinate.
        /// This field is required if a geoCoordinates container is used.
        /// This field is returned if geographical coordinates are set for
        /// the inventory location.
        /// </summary>
        [JsonProperty(PropertyName = "longitude")]
        public double? Longitude { get; set; }

    }
}
