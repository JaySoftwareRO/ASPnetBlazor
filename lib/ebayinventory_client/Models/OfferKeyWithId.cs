﻿// Code generated by Microsoft (R) AutoRest Code Generator 0.16.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace ebayinventory.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// This type is used by the &lt;strong&gt;getListingFees&lt;/strong&gt;
    /// call to indicate the unpublished offer(s) for which expected listing
    /// fees will be retrieved. The user passes in one or more
    /// &lt;strong&gt;offerId&lt;/strong&gt; values (a maximum of 250). See
    /// the &lt;a href="https://pages.ebay.com/help/sell/fees.html"
    /// target="_blank"&gt;Standard selling fees&lt;/a&gt; help page for more
    /// information on listing fees.
    /// </summary>
    public partial class OfferKeyWithId
    {
        /// <summary>
        /// Initializes a new instance of the OfferKeyWithId class.
        /// </summary>
        public OfferKeyWithId() { }

        /// <summary>
        /// Initializes a new instance of the OfferKeyWithId class.
        /// </summary>
        public OfferKeyWithId(string offerId = default(string))
        {
            OfferId = offerId;
        }

        /// <summary>
        /// The unique identifier of an unpublished offer for which expected
        /// listing fees will be retrieved. One to 250 offerId values can be
        /// passed in to the offers container for one getListingFees call.
        /// Errors will occur if offerId values representing published offers
        /// are passed in.
        /// </summary>
        [JsonProperty(PropertyName = "offerId")]
        public string OfferId { get; set; }

    }
}
