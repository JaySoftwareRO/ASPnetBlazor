﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace ebayinventory.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// This type is used by the &lt;strong&gt;intervals&lt;/strong&gt;
    /// container to define the opening and closing times of a store's
    /// working day. Local time (in Military format) is used, with the
    /// following format: &lt;code&gt;hh:mm:ss&lt;/code&gt;.
    /// </summary>
    public partial class Interval
    {
        /// <summary>
        /// Initializes a new instance of the Interval class.
        /// </summary>
        public Interval() { }

        /// <summary>
        /// Initializes a new instance of the Interval class.
        /// </summary>
        public Interval(string close = default(string), string open = default(string))
        {
            Close = close;
            Open = open;
        }

        /// <summary>
        /// The close value is actually the time that the store closes. Local
        /// time (in Military format) is used. So, if a store closed at 8 PM
        /// local time, the close time would look like the following:
        /// 20:00:00. This field is conditionally required if the intervals
        /// container is used to specify working hours or special hours for a
        /// store. This field is returned if set for the store location.
        /// </summary>
        [JsonProperty(PropertyName = "close")]
        public string Close { get; set; }

        /// <summary>
        /// The open value is actually the time that the store opens. Local
        /// time (in Military format) is used. So, if a store opens at 9 AM
        /// local time, the close time would look like the following:
        /// 09:00:00. This field is conditionally required if the intervals
        /// container is used to specify working hours or special hours for a
        /// store. This field is returned if set for the store location.
        /// </summary>
        [JsonProperty(PropertyName = "open")]
        public string Open { get; set; }

    }
}
