﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace ebayinventory.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// This type is used to specify the weight (and the unit used to measure
    /// that weight) of a shipping package. The
    /// &lt;strong&gt;weight&lt;/strong&gt; container is conditionally
    /// required if the seller will be offering calculated shipping rates to
    /// determine shipping cost, or is using flat-rate costs, but charging a
    /// weight surcharge. See the &lt;a
    /// href="https://pages.ebay.com/help/pay/calculated-shipping.html"
    /// target="_blank"&gt;Calculated shipping&lt;/a&gt; help page for more
    /// information on calculated shipping.
    /// </summary>
    public partial class Weight
    {
        /// <summary>
        /// Initializes a new instance of the Weight class.
        /// </summary>
        public Weight() { }

        /// <summary>
        /// Initializes a new instance of the Weight class.
        /// </summary>
        public Weight(string unit = default(string), double? value = default(double?))
        {
            Unit = unit;
            Value = value;
        }

        /// <summary>
        /// The unit of measurement used to specify the weight of a shipping
        /// package. Both the unit and value fields are required if the
        /// weight container is used. If the English system of measurement is
        /// being used, the applicable values for weight units are POUND and
        /// OUNCE. If the metric system of measurement is being used, the
        /// applicable values for weight units are KILOGRAM and GRAM. The
        /// metric system is used by most countries outside of the US. For
        /// implementation help, refer to &lt;a
        /// href='https://developer.ebay.com/devzone/rest/api-ref/inventory/types/WeightUnitOfMeasureEnum.html'&gt;eBay
        /// API documentation&lt;/a&gt;
        /// </summary>
        [JsonProperty(PropertyName = "unit")]
        public string Unit { get; set; }

        /// <summary>
        /// The actual weight (in the measurement unit specified in the unit
        /// field) of the shipping package. Both the unit and value fields
        /// are required if the weight container is used. If a shipping
        /// package weighed 20.5 ounces, the container would look as follows:
        /// &amp;quot;weight&amp;quot;: {  &amp;quot;value&amp;quot;: 20.5,
        /// &amp;quot;unit&amp;quot;: &amp;quot;OUNCE&amp;quot;  }
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public double? Value { get; set; }

    }
}
