using ebayws;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using lib.cache;

namespace lib.listers
{
    public class EbayLister : Lister
    {
        // https://developer.ebay.com/api-docs/commerce/taxonomy/static/supportedmarketplaces.html
        // private const string MarketplaceUSA = "EBAY_US";

        // TODO: vladi: all of these should be configurable
        // Define the endpoint (e.g., the Sandbox Gateway URI)
        private static string endpoint = "https://api.ebay.com/wsapi";

        // Define the query string parameters.
        private static string queryString = "?callname=GetMyeBaySelling"
                            + "&siteid=0"
                            + "&appid=VladIova-Treecat-SBX-6bce464fb-92785135"
                            + "&version=1149"
                            + "&Routing=new";

        String requestURL = endpoint + queryString; // "https://api.ebay.com/wsapi";

        IExtendedDistributedCache cache;
        ILogger logger;
        int liveCallLimit;
        int liveCalls = 0;
        ITokenGetter tokenGetter;
        string accountID;

        public EbayLister(IExtendedDistributedCache cache, ILogger logger, int liveCallLimit, ITokenGetter tokenGetter)
        {
            this.cache = cache;
            this.logger = logger;
            this.liveCallLimit = liveCallLimit;
            this.tokenGetter = tokenGetter;
            this.accountID = tokenGetter.GetUserID().Result;
        }
        public async Task<List<Item>> List()
        {
            eBayAPIInterfaceClient client = new eBayAPIInterfaceClient();
            client.Endpoint.Address = new EndpointAddress(requestURL);

            var cachedSellingItems = await this.cache.GetAsync(this.accountID);
            GetMyeBaySellingResponse sellingItems = new GetMyeBaySellingResponse();

            if (cachedSellingItems != null)
            {
                sellingItems = JsonConvert.DeserializeObject<GetMyeBaySellingResponse>(ASCIIEncoding.UTF8.GetString(cachedSellingItems));

                if (sellingItems.GetMyeBaySellingResponse1 == null || sellingItems.GetMyeBaySellingResponse1.ActiveList == null)
                {
                    await this.cache.RemoveAsync(this.accountID);
                    cachedSellingItems = null;
                }
            }

            if (cachedSellingItems == null && liveCalls < this.liveCallLimit)
            {
                liveCalls += 1;

                // TODO: dispose of the OperationContextScope properly
                var httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers["X-EBAY-API-IAF-TOKEN"] = await tokenGetter.GetToken();

                using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
                {
                    try
                    {
                        OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                        sellingItems = client.GetMyeBaySellingAsync(null, new GetMyeBaySellingRequestType()
                        {
                            // TODO: should not be hardcoded
                            Version = "1149",
                            ActiveList = new ItemListCustomizationType() { Sort = ItemSortTypeCodeType.TimeLeft, Pagination = new PaginationType() { EntriesPerPage = 3, PageNumber = 1 } }
                        }).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError($"error getting ebay items {ex}");
                    }
                }

                await this.cache.SetAsync(
                    this.accountID,
                    ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(sellingItems)),
                    new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                    });
            }

            List<Item> result = new List<Item>();

            if (sellingItems.GetMyeBaySellingResponse1 == null || sellingItems.GetMyeBaySellingResponse1.ActiveList == null)
            {
                return result;
            }

            foreach (var ebayItem in sellingItems.GetMyeBaySellingResponse1.ActiveList.ItemArray)
            {
                try
                {
                    var treecatItem = new Item();

                    treecatItem.ID = ebayItem.ItemID;
                    treecatItem.Provisioner = "eBay";
                    treecatItem.Title = ebayItem.Title;

                    if (ebayItem.BuyItNowPrice != null)
                    {
                        treecatItem.BuyItNowPrice = new AmountType();
                        treecatItem.BuyItNowPrice.currencyID = ebayItem.BuyItNowPrice.currencyID;
                        treecatItem.Price = ebayItem.BuyItNowPrice.Value;
                    }

                    treecatItem.Description = ebayItem.Description;
                    treecatItem.Status = ebayItem.SellingStatus.ListingStatus.ToString();
                    treecatItem.Stock = ebayItem.QuantityAvailable;
                    treecatItem.MainImageURL = ebayItem.PictureDetails.GalleryURL;

                    if (ebayItem.SKU != null)
                    {
                        treecatItem.SKU = ebayItem.SKU;
                    }

                    if (ebayItem.ApplyBuyerProtection != null)
                    {
                        treecatItem.ApplyBuyerProtection = new BuyerProtectionDetailsType();
                        treecatItem.ApplyBuyerProtection.Any = ebayItem.ApplyBuyerProtection.Any;
                        treecatItem.ApplyBuyerProtection.BuyerProtectionSource = ebayItem.ApplyBuyerProtection.BuyerProtectionSource;
                        treecatItem.ApplyBuyerProtection.BuyerProtectionSourceSpecified = ebayItem.ApplyBuyerProtection.BuyerProtectionSourceSpecified;
                        treecatItem.ApplyBuyerProtection.BuyerProtectionStatus = ebayItem.ApplyBuyerProtection.BuyerProtectionStatus;
                        treecatItem.ApplyBuyerProtection.BuyerProtectionStatusSpecified = ebayItem.ApplyBuyerProtection.BuyerProtectionStatusSpecified;
                    }

                    if (ebayItem.AttributeArray != null)
                    {
                        treecatItem.AttributeArray = new AttributeType[ebayItem.AttributeArray.Length];
                        for (int i = 0; ebayItem.AttributeArray.Length > i; i++)
                        {
                            treecatItem.AttributeArray[i] = new AttributeType();
                            treecatItem.AttributeArray[i].Any = ebayItem.AttributeArray[i].Any;
                            treecatItem.AttributeArray[i].attributeID = ebayItem.AttributeArray[i].attributeID;
                            treecatItem.AttributeArray[i].attributeIDSpecified = ebayItem.AttributeArray[i].attributeIDSpecified;

                            if (ebayItem.AttributeArray[i].attributeLabel != null)
                            {
                                treecatItem.AttributeArray[i].attributeLabel = ebayItem.AttributeArray[i].attributeLabel;
                            }

                            if (ebayItem.AttributeArray[i].Value != null)
                            {
                                treecatItem.AttributeArray[i].Value = new ValType[ebayItem.AttributeArray[i].Value.Length];
                                for (int y = 0; ebayItem.AttributeArray[i].Value.Length > y; y++)
                                {
                                    treecatItem.AttributeArray[i] = new AttributeType();
                                    treecatItem.AttributeArray[i].Value[y].Any = ebayItem.AttributeArray[i].Value[y].Any;
                                    treecatItem.AttributeArray[i].Value[y].SuggestedValueLiteral = ebayItem.AttributeArray[i].Value[y].SuggestedValueLiteral;
                                    treecatItem.AttributeArray[i].Value[y].ValueID = ebayItem.AttributeArray[i].Value[y].ValueID;
                                    treecatItem.AttributeArray[i].Value[y].ValueIDSpecified = ebayItem.AttributeArray[i].Value[y].ValueIDSpecified;

                                    if (ebayItem.AttributeArray[i].Value[y].ValueLiteral != null)
                                    {
                                        treecatItem.AttributeArray[i].Value[y].ValueLiteral = ebayItem.AttributeArray[i].Value[y].ValueLiteral;
                                    }
                                }
                            }
                        }
                    }

                    treecatItem.AutoPay = ebayItem.AutoPay;
                    treecatItem.AutoPaySpecified = ebayItem.AutoPaySpecified;
                    treecatItem.AvailableForPickupDropOff = ebayItem.AvailableForPickupDropOff;
                    treecatItem.AvailableForPickupDropOffSpecified = ebayItem.AvailableForPickupDropOffSpecified;

                    if (ebayItem.BestOfferDetails != null)
                    {
                        treecatItem.BestOfferDetails = new BestOfferDetailsType();
                        if (ebayItem.BestOfferDetails.BestOffer != null)
                        {
                            treecatItem.BestOfferDetails.BestOffer = new AmountType();
                            treecatItem.BestOfferDetails.BestOffer.currencyID = ebayItem.BestOfferDetails.BestOffer.currencyID;
                            treecatItem.BestOfferDetails.BestOffer.Value = ebayItem.BestOfferDetails.BestOffer.Value;
                        }

                        treecatItem.BestOfferDetails.BestOfferCount = ebayItem.BestOfferDetails.BestOfferCount;
                        treecatItem.BestOfferDetails.BestOfferCountSpecified = ebayItem.BestOfferDetails.BestOfferCountSpecified;
                        treecatItem.BestOfferDetails.BestOfferEnabled = ebayItem.BestOfferDetails.BestOfferEnabled;
                        treecatItem.BestOfferDetails.BestOfferEnabledSpecified = ebayItem.BestOfferDetails.BestOfferEnabledSpecified;
                        treecatItem.BestOfferDetails.BestOfferStatus = ebayItem.BestOfferDetails.BestOfferStatus;
                        treecatItem.BestOfferDetails.BestOfferStatusSpecified = ebayItem.BestOfferDetails.BestOfferStatusSpecified;
                        treecatItem.BestOfferDetails.BestOfferType = ebayItem.BestOfferDetails.BestOfferType;
                        treecatItem.BestOfferDetails.BestOfferTypeSpecified = ebayItem.BestOfferDetails.BestOfferTypeSpecified;
                        treecatItem.BestOfferDetails.NewBestOffer = ebayItem.BestOfferDetails.NewBestOffer;
                        treecatItem.BestOfferDetails.NewBestOfferSpecified = ebayItem.BestOfferDetails.NewBestOfferSpecified;
                    }

                    treecatItem.BestOfferEnabled = ebayItem.BestOfferEnabled;
                    treecatItem.BestOfferEnabledSpecified = ebayItem.BestOfferEnabledSpecified;

                    if (ebayItem.BiddingDetails != null)
                    {
                        treecatItem.BiddingDetails = new BiddingDetailsType();
                        treecatItem.BiddingDetails.Any = ebayItem.BiddingDetails.Any;
                        treecatItem.BiddingDetails.BidAssistant = ebayItem.BiddingDetails.BidAssistant;
                        treecatItem.BiddingDetails.BidAssistantSpecified = ebayItem.BiddingDetails.BidAssistantSpecified;

                        if (ebayItem.BiddingDetails.ConvertedMaxBid != null)
                        {
                            treecatItem.BiddingDetails.ConvertedMaxBid = new AmountType();
                            treecatItem.BiddingDetails.ConvertedMaxBid.currencyID = ebayItem.BiddingDetails.ConvertedMaxBid.currencyID;
                            treecatItem.BiddingDetails.ConvertedMaxBid.Value = ebayItem.BiddingDetails.ConvertedMaxBid.Value;
                        }

                        if (ebayItem.BiddingDetails.MaxBid != null)
                        {
                            treecatItem.BiddingDetails.MaxBid = new AmountType();
                            treecatItem.BiddingDetails.MaxBid.currencyID = ebayItem.BiddingDetails.MaxBid.currencyID;
                            treecatItem.BiddingDetails.MaxBid.Value = ebayItem.BiddingDetails.MaxBid.Value;
                        }

                        treecatItem.BiddingDetails.QuantityBid = ebayItem.BiddingDetails.QuantityBid;
                        treecatItem.BiddingDetails.QuantityBidSpecified = ebayItem.BiddingDetails.QuantityBidSpecified;
                        treecatItem.BiddingDetails.QuantityWon = ebayItem.BiddingDetails.QuantityWon;
                        treecatItem.BiddingDetails.QuantityWonSpecified = ebayItem.BiddingDetails.QuantityWonSpecified;
                        treecatItem.BiddingDetails.Winning = ebayItem.BiddingDetails.Winning;
                        treecatItem.BiddingDetails.WinningSpecified = ebayItem.BiddingDetails.WinningSpecified;
                    }

                    treecatItem.BidGroupItem = ebayItem.BidGroupItem;
                    treecatItem.BidGroupItemSpecified = ebayItem.BidGroupItemSpecified;

                    if (ebayItem.BusinessSellerDetails != null)
                    {
                        treecatItem.BusinessSellerDetails = new BusinessSellerDetailsType();
                        if (ebayItem.BusinessSellerDetails.Address != null)
                        {
                            treecatItem.BusinessSellerDetails.Address = new AddressType();
                            if (ebayItem.BusinessSellerDetails.Address.AddressAttribute != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.AddressAttribute = new AddressAttributeType[ebayItem.BusinessSellerDetails.Address.AddressAttribute.Length];
                                for (int i = 0; ebayItem.BusinessSellerDetails.Address.AddressAttribute.Length > i; i++)
                                {
                                    treecatItem.BusinessSellerDetails.Address.AddressAttribute[i] = new AddressAttributeType();
                                    treecatItem.BusinessSellerDetails.Address.AddressAttribute[i].type = ebayItem.BusinessSellerDetails.Address.AddressAttribute[i].type;
                                    treecatItem.BusinessSellerDetails.Address.AddressAttribute[i].typeSpecified = ebayItem.BusinessSellerDetails.Address.AddressAttribute[i].typeSpecified;

                                    if (ebayItem.BusinessSellerDetails.Address.AddressAttribute[i].Value != null)
                                    {
                                        treecatItem.BusinessSellerDetails.Address.AddressAttribute[i].Value = ebayItem.BusinessSellerDetails.Address.AddressAttribute[i].Value;
                                    }
                                }
                            }

                            if (ebayItem.BusinessSellerDetails.Address.AddressID != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.AddressID = ebayItem.BusinessSellerDetails.Address.AddressID;
                            }

                            treecatItem.BusinessSellerDetails.Address.AddressOwner = ebayItem.BusinessSellerDetails.Address.AddressOwner;
                            treecatItem.BusinessSellerDetails.Address.AddressOwnerSpecified = ebayItem.BusinessSellerDetails.Address.AddressOwnerSpecified;
                            treecatItem.BusinessSellerDetails.Address.AddressRecordType = ebayItem.BusinessSellerDetails.Address.AddressRecordType;
                            treecatItem.BusinessSellerDetails.Address.AddressRecordTypeSpecified = ebayItem.BusinessSellerDetails.Address.AddressRecordTypeSpecified;
                            treecatItem.BusinessSellerDetails.Address.AddressStatus = ebayItem.BusinessSellerDetails.Address.AddressStatus;
                            treecatItem.BusinessSellerDetails.Address.AddressStatusSpecified = ebayItem.BusinessSellerDetails.Address.AddressStatusSpecified;
                            treecatItem.BusinessSellerDetails.Address.AddressUsage = ebayItem.BusinessSellerDetails.Address.AddressUsage;
                            treecatItem.BusinessSellerDetails.Address.AddressUsageSpecified = ebayItem.BusinessSellerDetails.Address.AddressUsageSpecified;
                            treecatItem.BusinessSellerDetails.Address.Any = ebayItem.BusinessSellerDetails.Address.Any;

                            if (ebayItem.BusinessSellerDetails.Address.CityName != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.CityName = ebayItem.BusinessSellerDetails.Address.CityName;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.CompanyName != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.CompanyName = ebayItem.BusinessSellerDetails.Address.CompanyName;
                            }

                            treecatItem.BusinessSellerDetails.Address.Country = ebayItem.BusinessSellerDetails.Address.Country;

                            if (ebayItem.BusinessSellerDetails.Address.CountryName != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.CountryName = ebayItem.BusinessSellerDetails.Address.CountryName;
                            }

                            treecatItem.BusinessSellerDetails.Address.CountrySpecified = ebayItem.BusinessSellerDetails.Address.CountrySpecified;

                            if (ebayItem.BusinessSellerDetails.Address.County != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.County = ebayItem.BusinessSellerDetails.Address.County;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.ExternalAddressID != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.ExternalAddressID = ebayItem.BusinessSellerDetails.Address.ExternalAddressID;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.FirstName != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.FirstName = ebayItem.BusinessSellerDetails.Address.FirstName;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.InternationalName != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.InternationalName = ebayItem.BusinessSellerDetails.Address.InternationalName;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.InternationalStateAndCity != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.InternationalStateAndCity = ebayItem.BusinessSellerDetails.Address.InternationalStateAndCity;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.InternationalStreet != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.InternationalStreet = ebayItem.BusinessSellerDetails.Address.InternationalStreet;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.LastName != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.LastName = ebayItem.BusinessSellerDetails.Address.LastName;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.Name != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.Name = ebayItem.BusinessSellerDetails.Address.Name;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.Phone != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.Phone = ebayItem.BusinessSellerDetails.Address.Phone;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.Phone2 != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.Phone2 = ebayItem.BusinessSellerDetails.Address.Phone2;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.PhoneAreaOrCityCode != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.PhoneAreaOrCityCode = ebayItem.BusinessSellerDetails.Address.PhoneAreaOrCityCode;
                            }

                            treecatItem.BusinessSellerDetails.Address.PhoneCountryCode = ebayItem.BusinessSellerDetails.Address.PhoneCountryCode;
                            treecatItem.BusinessSellerDetails.Address.PhoneCountryCodeSpecified = ebayItem.BusinessSellerDetails.Address.PhoneCountryCodeSpecified;

                            if (ebayItem.BusinessSellerDetails.Address.PhoneCountryPrefix != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.PhoneCountryPrefix = ebayItem.BusinessSellerDetails.Address.PhoneCountryPrefix;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.PhoneLocalNumber != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.PhoneLocalNumber = ebayItem.BusinessSellerDetails.Address.PhoneLocalNumber;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.PostalCode != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.PostalCode = ebayItem.BusinessSellerDetails.Address.PostalCode;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.ReferenceID != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.ReferenceID = ebayItem.BusinessSellerDetails.Address.ReferenceID;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.StateOrProvince != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.StateOrProvince = ebayItem.BusinessSellerDetails.Address.StateOrProvince;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.Street != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.Street = ebayItem.BusinessSellerDetails.Address.Street;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.Street1 != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.Street1 = ebayItem.BusinessSellerDetails.Address.Street1;
                            }

                            if (ebayItem.BusinessSellerDetails.Address.Street2 != null)
                            {
                                treecatItem.BusinessSellerDetails.Address.Street2 = ebayItem.BusinessSellerDetails.Address.Street2;
                            }
                        }

                        if (ebayItem.BusinessSellerDetails.AdditionalContactInformation != null)
                        {
                            treecatItem.BusinessSellerDetails.AdditionalContactInformation = ebayItem.BusinessSellerDetails.AdditionalContactInformation;
                        }

                        treecatItem.BusinessSellerDetails.Any = ebayItem.BusinessSellerDetails.Any;

                        if (ebayItem.BusinessSellerDetails.Email != null)
                        {
                            treecatItem.BusinessSellerDetails.Email = ebayItem.BusinessSellerDetails.Email;
                        }

                        if (ebayItem.BusinessSellerDetails.Fax != null)
                        {
                            treecatItem.BusinessSellerDetails.Fax = ebayItem.BusinessSellerDetails.Fax;
                        }

                        treecatItem.BusinessSellerDetails.LegalInvoice = ebayItem.BusinessSellerDetails.LegalInvoice;
                        treecatItem.BusinessSellerDetails.LegalInvoiceSpecified = ebayItem.BusinessSellerDetails.LegalInvoiceSpecified;

                        if (ebayItem.BusinessSellerDetails.TermsAndConditions != null)
                        {
                            treecatItem.BusinessSellerDetails.TermsAndConditions = ebayItem.BusinessSellerDetails.TermsAndConditions;
                        }

                        if (ebayItem.BusinessSellerDetails.TradeRegistrationNumber != null)
                        {
                            treecatItem.BusinessSellerDetails.TradeRegistrationNumber = ebayItem.BusinessSellerDetails.TradeRegistrationNumber;
                        }

                        if (ebayItem.BusinessSellerDetails.VATDetails != null)
                        {
                            treecatItem.BusinessSellerDetails.VATDetails = new VATDetailsType();
                            treecatItem.BusinessSellerDetails.VATDetails.Any = ebayItem.BusinessSellerDetails.VATDetails.Any;
                            treecatItem.BusinessSellerDetails.VATDetails.BusinessSeller = ebayItem.BusinessSellerDetails.VATDetails.BusinessSeller;
                            treecatItem.BusinessSellerDetails.VATDetails.BusinessSellerSpecified = ebayItem.BusinessSellerDetails.VATDetails.BusinessSellerSpecified;
                            treecatItem.BusinessSellerDetails.VATDetails.RestrictedToBusiness = ebayItem.BusinessSellerDetails.VATDetails.RestrictedToBusiness;
                            treecatItem.BusinessSellerDetails.VATDetails.RestrictedToBusinessSpecified = ebayItem.BusinessSellerDetails.VATDetails.RestrictedToBusinessSpecified;

                            if (ebayItem.BusinessSellerDetails.VATDetails.VATID != null)
                            {
                                treecatItem.BusinessSellerDetails.VATDetails.VATID = ebayItem.BusinessSellerDetails.VATDetails.VATID;
                            }

                            treecatItem.BusinessSellerDetails.VATDetails.VATPercent = ebayItem.BusinessSellerDetails.VATDetails.VATPercent;
                            treecatItem.BusinessSellerDetails.VATDetails.VATPercentSpecified = ebayItem.BusinessSellerDetails.VATDetails.VATPercentSpecified;

                            if (ebayItem.BusinessSellerDetails.VATDetails.VATSite != null)
                            {
                                treecatItem.BusinessSellerDetails.VATDetails.VATSite = ebayItem.BusinessSellerDetails.VATDetails.VATSite;
                            }
                        }
                    }

                    if (ebayItem.BuyerGuaranteePrice != null)
                    {
                        treecatItem.BuyerGuaranteePrice = new AmountType();
                        treecatItem.BuyerGuaranteePrice.currencyID = ebayItem.BuyerGuaranteePrice.currencyID;
                        treecatItem.BuyerGuaranteePrice.Value = ebayItem.BuyerGuaranteePrice.Value;
                    }

                    treecatItem.BuyerProtection = ebayItem.BuyerProtection;
                    treecatItem.BuyerProtectionSpecified = ebayItem.BuyerProtectionSpecified;


                    if (ebayItem.BuyerRequirementDetails != null)
                    {
                        treecatItem.BuyerRequirementDetails = new BuyerRequirementDetailsType();
                        treecatItem.BuyerRequirementDetails.Any = ebayItem.BuyerRequirementDetails.Any;

                        if (ebayItem.BuyerRequirementDetails.MaximumItemRequirements != null)
                        {
                            treecatItem.BuyerRequirementDetails.MaximumItemRequirements = new MaximumItemRequirementsType();
                            treecatItem.BuyerRequirementDetails.MaximumItemRequirements.MaximumItemCount = ebayItem.BuyerRequirementDetails.MaximumItemRequirements.MaximumItemCount;
                            treecatItem.BuyerRequirementDetails.MaximumItemRequirements.MaximumItemCountSpecified = ebayItem.BuyerRequirementDetails.MaximumItemRequirements.MaximumItemCountSpecified;
                            treecatItem.BuyerRequirementDetails.MaximumItemRequirements.MinimumFeedbackScore = ebayItem.BuyerRequirementDetails.MaximumItemRequirements.MinimumFeedbackScore;
                            treecatItem.BuyerRequirementDetails.MaximumItemRequirements.MinimumFeedbackScoreSpecified = ebayItem.BuyerRequirementDetails.MaximumItemRequirements.MinimumFeedbackScoreSpecified;
                        }

                        if (ebayItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo != null)
                        {
                            treecatItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo = new MaximumUnpaidItemStrikesInfoType();
                            treecatItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.Any = ebayItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.Any;
                            treecatItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.Count = ebayItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.Count;
                            treecatItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.CountSpecified = ebayItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.CountSpecified;
                            treecatItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.Period = ebayItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.Period;
                            treecatItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.PeriodSpecified = ebayItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.PeriodSpecified;
                        }

                        treecatItem.BuyerRequirementDetails.ShipToRegistrationCountry = ebayItem.BuyerRequirementDetails.ShipToRegistrationCountry;
                        treecatItem.BuyerRequirementDetails.ShipToRegistrationCountrySpecified = ebayItem.BuyerRequirementDetails.ShipToRegistrationCountrySpecified;
                        treecatItem.BuyerRequirementDetails.ZeroFeedbackScore = ebayItem.BuyerRequirementDetails.ZeroFeedbackScore;
                        treecatItem.BuyerRequirementDetails.ZeroFeedbackScoreSpecified = ebayItem.BuyerRequirementDetails.ZeroFeedbackScoreSpecified;
                    }


                    treecatItem.BuyerResponsibleForShipping = ebayItem.BuyerResponsibleForShipping;
                    treecatItem.BuyerResponsibleForShippingSpecified = ebayItem.BuyerResponsibleForShippingSpecified;

                    treecatItem.CategoryMappingAllowed = ebayItem.CategoryMappingAllowed;

                    if (ebayItem.CeilingPrice != null)
                    {
                        treecatItem.CeilingPrice = new AmountType();
                        treecatItem.CeilingPrice.currencyID = ebayItem.CeilingPrice.currencyID;
                        treecatItem.CeilingPrice.Value = ebayItem.CeilingPrice.Value;
                    }

                    if (ebayItem.Charity != null)
                    {
                        treecatItem.Charity = new CharityType();

                        if (ebayItem.Charity.CharityID != null)
                        {
                            treecatItem.Charity.CharityID = ebayItem.Charity.CharityID;
                        }

                        if (ebayItem.Charity.CharityName != null)
                        {
                            treecatItem.Charity.CharityName = ebayItem.Charity.CharityName;
                        }

                        if (ebayItem.Charity.LogoURL != null)
                        {
                            treecatItem.Charity.LogoURL = ebayItem.Charity.LogoURL;
                        }

                        if (ebayItem.Charity.Mission != null)
                        {
                            treecatItem.Charity.Mission = ebayItem.Charity.Mission;
                        }

                        treecatItem.Charity.Any = ebayItem.Charity.Any;
                        treecatItem.Charity.CharityListing = ebayItem.Charity.CharityListing;
                        treecatItem.Charity.CharityListingSpecified = ebayItem.Charity.CharityListingSpecified;
                        treecatItem.Charity.CharityNumber = ebayItem.Charity.CharityNumber;
                        treecatItem.Charity.CharityNumberSpecified = ebayItem.Charity.CharityNumberSpecified;
                        treecatItem.Charity.DonationPercent = ebayItem.Charity.DonationPercent;
                        treecatItem.Charity.DonationPercentSpecified = ebayItem.Charity.DonationPercentSpecified;
                        treecatItem.Charity.StatusSpecified = ebayItem.Charity.StatusSpecified;
                    }

                    if (ebayItem.ClassifiedAdPayPerLeadFee != null)
                    {
                        treecatItem.ClassifiedAdPayPerLeadFee = new AmountType();
                        treecatItem.ClassifiedAdPayPerLeadFee.currencyID = ebayItem.ClassifiedAdPayPerLeadFee.currencyID;
                        treecatItem.ClassifiedAdPayPerLeadFee.Value = ebayItem.ClassifiedAdPayPerLeadFee.Value;
                    }

                    if (ebayItem.ConditionDefinition != null)
                    {
                        treecatItem.ConditionDefinition = ebayItem.ConditionDefinition;
                    }

                    if (ebayItem.ConditionDescription != null)
                    {
                        treecatItem.ConditionDescription = ebayItem.ConditionDescription;
                    }

                    if (ebayItem.ConditionDisplayName != null)
                    {
                        treecatItem.ConditionDisplayName = ebayItem.ConditionDisplayName;
                    }

                    treecatItem.ConditionID = ebayItem.ConditionID;
                    treecatItem.ConditionIDSpecified = ebayItem.ConditionIDSpecified;
                    treecatItem.Country = ebayItem.Country;
                    treecatItem.CountrySpecified = ebayItem.CountrySpecified;

                    if (ebayItem.CrossBorderTrade != null)
                    {
                        treecatItem.CrossBorderTrade = ebayItem.CrossBorderTrade;
                    }

                    if (ebayItem.CrossPromotion != null)
                    {
                        treecatItem.CrossPromotions = new CrossPromotionsType();

                        if (ebayItem.CrossPromotion.ItemID != null)
                        {
                            treecatItem.CrossPromotions.ItemID = ebayItem.CrossPromotion.ItemID;
                        }

                        treecatItem.CrossPromotions.Any = ebayItem.CrossPromotion.Any;
                        treecatItem.CrossPromotions.PrimaryScheme = ebayItem.CrossPromotion.PrimaryScheme;
                        treecatItem.CrossPromotions.PrimarySchemeSpecified = ebayItem.CrossPromotion.PrimarySchemeSpecified;

                        if (ebayItem.CrossPromotion.PromotedItem != null)
                        {
                            treecatItem.CrossPromotions.PromotedItem = new PromotedItemType[ebayItem.CrossPromotion.PromotedItem.Length];
                            for (int i = 0; ebayItem.CrossPromotion.PromotedItem.Length > i; i++)
                            {
                                treecatItem.CrossPromotions.PromotedItem[i] = new PromotedItemType();
                                treecatItem.CrossPromotions.PromotedItem[i].Any = ebayItem.CrossPromotion.PromotedItem[i].Any;

                                if (ebayItem.CrossPromotion.PromotedItem[i].ItemID != null)
                                {
                                    treecatItem.CrossPromotions.PromotedItem[i].ItemID = ebayItem.CrossPromotion.PromotedItem[i].ItemID;
                                }

                                treecatItem.CrossPromotions.PromotedItem[i].ListingType = ebayItem.CrossPromotion.PromotedItem[i].ListingType;
                                treecatItem.CrossPromotions.PromotedItem[i].ListingTypeSpecified = ebayItem.CrossPromotion.PromotedItem[i].ListingTypeSpecified;

                                if (ebayItem.CrossPromotion.PromotedItem[i].PictureURL != null)
                                {
                                    treecatItem.CrossPromotions.PromotedItem[i].PictureURL = ebayItem.CrossPromotion.PromotedItem[i].PictureURL;
                                }

                                treecatItem.CrossPromotions.PromotedItem[i].Position = ebayItem.CrossPromotion.PromotedItem[i].Position;
                                treecatItem.CrossPromotions.PromotedItem[i].PositionSpecified = ebayItem.CrossPromotion.PromotedItem[i].PositionSpecified;
                                treecatItem.CrossPromotions.PromotedItem[i].SelectionType = ebayItem.CrossPromotion.PromotedItem[i].SelectionType;
                                treecatItem.CrossPromotions.PromotedItem[i].SelectionTypeSpecified = ebayItem.CrossPromotion.PromotedItem[i].SelectionTypeSpecified;

                                if (ebayItem.CrossPromotion.PromotedItem[i].TimeLeft != null)
                                {
                                    treecatItem.CrossPromotions.PromotedItem[i].TimeLeft = ebayItem.CrossPromotion.PromotedItem[i].TimeLeft;
                                }

                                if (ebayItem.CrossPromotion.PromotedItem[i].Title != null)
                                {
                                    treecatItem.CrossPromotions.PromotedItem[i].Title = ebayItem.CrossPromotion.PromotedItem[i].Title;
                                }

                                if (ebayItem.CrossPromotion.PromotedItem[i].PromotionDetails != null)
                                {
                                    treecatItem.CrossPromotions.PromotedItem[i].PromotionDetails = new PromotionDetailsType[ebayItem.CrossPromotion.PromotedItem[i].PromotionDetails.Length];
                                    for (int x = 0; ebayItem.CrossPromotion.PromotedItem[i].PromotionDetails.Length > x; x++)
                                    {
                                        treecatItem.CrossPromotions.PromotedItem[i] = new PromotedItemType();
                                        treecatItem.CrossPromotions.PromotedItem[i].PromotionDetails[x].Any = ebayItem.CrossPromotion.PromotedItem[i].PromotionDetails[x].Any;
                                        treecatItem.CrossPromotions.PromotedItem[i].PromotionDetails[x].BidCount = ebayItem.CrossPromotion.PromotedItem[i].PromotionDetails[x].BidCount;
                                        treecatItem.CrossPromotions.PromotedItem[i].PromotionDetails[x].BidCountSpecified = ebayItem.CrossPromotion.PromotedItem[i].PromotionDetails[x].BidCountSpecified;

                                        if (ebayItem.CrossPromotion.PromotedItem[i].PromotionDetails[x].ConvertedPromotionPrice != null)
                                        {
                                            treecatItem.CrossPromotions.PromotedItem[i].PromotionDetails[x].ConvertedPromotionPrice = new AmountType();
                                            treecatItem.CrossPromotions.PromotedItem[i].PromotionDetails[x].ConvertedPromotionPrice.currencyID = ebayItem.CrossPromotion.PromotedItem[i].PromotionDetails[x].ConvertedPromotionPrice.currencyID;
                                            treecatItem.CrossPromotions.PromotedItem[i].PromotionDetails[x].ConvertedPromotionPrice.Value = ebayItem.CrossPromotion.PromotedItem[i].PromotionDetails[x].ConvertedPromotionPrice.Value;
                                        }

                                        if (ebayItem.CrossPromotion.PromotedItem[i].PromotionDetails[x].PromotionPrice != null)
                                        {
                                            treecatItem.CrossPromotions.PromotedItem[i].PromotionDetails[x].PromotionPrice = new AmountType();
                                            treecatItem.CrossPromotions.PromotedItem[i].PromotionDetails[x].PromotionPrice.currencyID = ebayItem.CrossPromotion.PromotedItem[i].PromotionDetails[x].PromotionPrice.currencyID;
                                            treecatItem.CrossPromotions.PromotedItem[i].PromotionDetails[x].PromotionPrice.Value = ebayItem.CrossPromotion.PromotedItem[i].PromotionDetails[x].PromotionPrice.Value;
                                        }

                                        treecatItem.CrossPromotions.PromotedItem[i].PromotionDetails[x].PromotionPriceType = ebayItem.CrossPromotion.PromotedItem[i].PromotionDetails[x].PromotionPriceType;
                                        treecatItem.CrossPromotions.PromotedItem[i].PromotionDetails[x].PromotionPriceTypeSpecified = ebayItem.CrossPromotion.PromotedItem[i].PromotionDetails[x].PromotionPriceTypeSpecified;
                                    }
                                }
                            }
                        }
                    }

                    if (ebayItem.CrossPromotion != null)
                    {
                        treecatItem.CrossPromotions = new CrossPromotionsType();

                        if (ebayItem.CrossPromotion.SellerID != null)
                        {
                            treecatItem.CrossPromotions.SellerID = ebayItem.CrossPromotion.SellerID;
                        }

                        if (ebayItem.CrossPromotion.StoreName != null)
                        {
                            treecatItem.CrossPromotions.StoreName = ebayItem.CrossPromotion.StoreName;
                        }

                        treecatItem.CrossPromotions.ShippingDiscount = ebayItem.CrossPromotion.ShippingDiscount;
                        treecatItem.CrossPromotions.PromotionMethod = ebayItem.CrossPromotion.PromotionMethod;
                        treecatItem.CrossPromotions.PromotionMethodSpecified = ebayItem.CrossPromotion.PromotionMethodSpecified;
                    }

                    treecatItem.Currency = ebayItem.Currency;
                    treecatItem.CurrencySpecified = ebayItem.CurrencySpecified;
                    treecatItem.DescriptionReviseMode = ebayItem.DescriptionReviseMode;
                    treecatItem.DescriptionReviseModeSpecified = ebayItem.DescriptionReviseModeSpecified;

                    if (ebayItem.DigitalGoodInfo != null)
                    {
                        treecatItem.DigitalGoodInfo = new DigitalGoodInfoType();
                        treecatItem.DigitalGoodInfo.Any = ebayItem.DigitalGoodInfo.Any;
                        treecatItem.DigitalGoodInfo.DigitalDelivery = ebayItem.DigitalGoodInfo.DigitalDelivery;
                        treecatItem.DigitalGoodInfo.DigitalDeliverySpecified = ebayItem.DigitalGoodInfo.DigitalDeliverySpecified;
                    }

                    treecatItem.DisableBuyerRequirements = ebayItem.DisableBuyerRequirements;
                    treecatItem.DisableBuyerRequirementsSpecified = ebayItem.DisableBuyerRequirementsSpecified;

                    if (ebayItem.DiscountPriceInfo != null)
                    {
                        treecatItem.DiscountPriceInfo = new DiscountPriceInfoType();

                        if (ebayItem.DiscountPriceInfo.MadeForOutletComparisonPrice != null)
                        {
                            treecatItem.DiscountPriceInfo.MadeForOutletComparisonPrice = new AmountType();
                            treecatItem.DiscountPriceInfo.MadeForOutletComparisonPrice.currencyID = ebayItem.DiscountPriceInfo.MadeForOutletComparisonPrice.currencyID;
                            treecatItem.DiscountPriceInfo.MadeForOutletComparisonPrice.Value = ebayItem.DiscountPriceInfo.MadeForOutletComparisonPrice.Value;
                        }

                        if (ebayItem.DiscountPriceInfo.MinimumAdvertisedPrice != null)
                        {
                            treecatItem.DiscountPriceInfo.MinimumAdvertisedPrice = new AmountType();
                            treecatItem.DiscountPriceInfo.MinimumAdvertisedPrice.currencyID = ebayItem.DiscountPriceInfo.MinimumAdvertisedPrice.currencyID;
                            treecatItem.DiscountPriceInfo.MinimumAdvertisedPrice.Value = ebayItem.DiscountPriceInfo.MinimumAdvertisedPrice.Value;
                        }

                        if (ebayItem.DiscountPriceInfo.OriginalRetailPrice != null)
                        {
                            treecatItem.DiscountPriceInfo.OriginalRetailPrice = new AmountType();
                            treecatItem.DiscountPriceInfo.OriginalRetailPrice.currencyID = ebayItem.DiscountPriceInfo.OriginalRetailPrice.currencyID;
                            treecatItem.DiscountPriceInfo.OriginalRetailPrice.Value = ebayItem.DiscountPriceInfo.OriginalRetailPrice.Value;
                        }

                        treecatItem.DiscountPriceInfo.Any = ebayItem.DiscountPriceInfo.Any;
                        treecatItem.DiscountPriceInfo.MinimumAdvertisedPriceExposure = ebayItem.DiscountPriceInfo.MinimumAdvertisedPriceExposure;
                        treecatItem.DiscountPriceInfo.MinimumAdvertisedPriceExposureSpecified = ebayItem.DiscountPriceInfo.MinimumAdvertisedPriceExposureSpecified;
                        treecatItem.DiscountPriceInfo.PricingTreatment = ebayItem.DiscountPriceInfo.PricingTreatment;
                        treecatItem.DiscountPriceInfo.PricingTreatmentSpecified = ebayItem.DiscountPriceInfo.PricingTreatmentSpecified;
                        treecatItem.DiscountPriceInfo.SoldOffeBay = ebayItem.DiscountPriceInfo.SoldOffeBay;
                        treecatItem.DiscountPriceInfo.SoldOneBay = ebayItem.DiscountPriceInfo.SoldOneBay;
                    }

                    treecatItem.DispatchTimeMax = ebayItem.DispatchTimeMax;
                    treecatItem.DispatchTimeMaxSpecified = ebayItem.DispatchTimeMaxSpecified;

                    if (ebayItem.Distance != null)
                    {
                        treecatItem.Distance = new DistanceType();

                        if (ebayItem.Distance.DistanceUnit != null)
                        {
                            treecatItem.Distance.DistanceUnit = ebayItem.Distance.DistanceUnit;
                        }

                        treecatItem.Distance.Any = ebayItem.Distance.Any;
                        treecatItem.Distance.DistanceMeasurement = ebayItem.Distance.DistanceMeasurement;
                    }

                    if (ebayItem.eBayNotes != null)
                    {
                        treecatItem.eBayNotes = ebayItem.eBayNotes;
                    }

                    treecatItem.eBayPlus = ebayItem.eBayPlus;
                    treecatItem.eBayPlusEligible = ebayItem.eBayPlusEligible;
                    treecatItem.eBayPlusEligibleSpecified = ebayItem.eBayPlusEligibleSpecified;
                    treecatItem.eBayPlusSpecified = ebayItem.eBayPlusSpecified;
                    treecatItem.EligibleForPickupDropOff = ebayItem.EligibleForPickupDropOff;
                    treecatItem.EligibleForPickupDropOffSpecified = ebayItem.EligibleForPickupDropOffSpecified;
                    treecatItem.eMailDeliveryAvailable = ebayItem.eMailDeliveryAvailable;
                    treecatItem.eMailDeliveryAvailableSpecified = ebayItem.eMailDeliveryAvailableSpecified;

                    if (ebayItem.ExtendedSellerContactDetails != null)
                    {
                        treecatItem.ExtendedContactDetails = new ExtendedContactDetailsType();

                        if (ebayItem.ExtendedSellerContactDetails.ContactHoursDetails != null)
                        {
                            treecatItem.ExtendedContactDetails.ContactHoursDetails = new ContactHoursDetailsType();
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Any = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Any;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours1AnyTime = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours1AnyTime;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours1AnyTimeSpecified = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours1AnyTimeSpecified;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours1Days = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours1Days;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours1DaysSpecified = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours1DaysSpecified;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours1From = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours1From;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours1FromSpecified = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours1FromSpecified;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours1To = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours1To;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours1ToSpecified = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours1ToSpecified;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours2AnyTime = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours2AnyTime;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours2AnyTimeSpecified = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours2AnyTimeSpecified;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours2Days = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours2Days;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours2DaysSpecified = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours2DaysSpecified;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours2From = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours2From;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours2FromSpecified = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours2FromSpecified;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours2To = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours2To;
                            treecatItem.ExtendedContactDetails.ContactHoursDetails.Hours2ToSpecified = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.Hours2ToSpecified;

                            if (ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.TimeZoneID != null)
                            {
                                treecatItem.ExtendedContactDetails.ContactHoursDetails.TimeZoneID = ebayItem.ExtendedSellerContactDetails.ContactHoursDetails.TimeZoneID;
                            }

                            if (ebayItem.ExtendedSellerContactDetails.PayPerLeadPhoneNumber != null)
                            {
                                treecatItem.ExtendedContactDetails.PayPerLeadPhoneNumber = ebayItem.ExtendedSellerContactDetails.PayPerLeadPhoneNumber;
                            }
                        }

                        treecatItem.ExtendedContactDetails.Any = ebayItem.ExtendedSellerContactDetails.Any;
                        treecatItem.ExtendedContactDetails.ClassifiedAdContactByEmailEnabled = ebayItem.ExtendedSellerContactDetails.ClassifiedAdContactByEmailEnabled;
                        treecatItem.ExtendedContactDetails.ClassifiedAdContactByEmailEnabledSpecified = ebayItem.ExtendedSellerContactDetails.ClassifiedAdContactByEmailEnabledSpecified;
                    }

                    if (ebayItem.FloorPrice != null)
                    {
                        treecatItem.FloorPrice = new AmountType();
                        treecatItem.FloorPrice.currencyID = ebayItem.FloorPrice.currencyID;
                        treecatItem.FloorPrice.Value = ebayItem.FloorPrice.Value;
                    }

                    if (ebayItem.FreeAddedCategory != null)
                    {
                        treecatItem.FreeAddedCategory = new CategoryType();
                        treecatItem.FreeAddedCategory.Any = ebayItem.FreeAddedCategory.Any;
                        treecatItem.FreeAddedCategory.AutoPayEnabled = ebayItem.FreeAddedCategory.AutoPayEnabled;
                        treecatItem.FreeAddedCategory.AutoPayEnabledSpecified = ebayItem.FreeAddedCategory.AutoPayEnabledSpecified;
                        treecatItem.FreeAddedCategory.B2BVATEnabled = ebayItem.FreeAddedCategory.B2BVATEnabled;
                        treecatItem.FreeAddedCategory.B2BVATEnabledSpecified = ebayItem.FreeAddedCategory.B2BVATEnabledSpecified;
                        treecatItem.FreeAddedCategory.BestOfferEnabled = ebayItem.FreeAddedCategory.BestOfferEnabled;
                        treecatItem.FreeAddedCategory.BestOfferEnabledSpecified = ebayItem.FreeAddedCategory.BestOfferEnabledSpecified;
                        treecatItem.FreeAddedCategory.CatalogEnabled = ebayItem.FreeAddedCategory.CatalogEnabled;
                        treecatItem.FreeAddedCategory.CatalogEnabledSpecified = ebayItem.FreeAddedCategory.CatalogEnabledSpecified;
                        treecatItem.FreeAddedCategory.CategoryLevel = ebayItem.FreeAddedCategory.CategoryLevel;
                        treecatItem.FreeAddedCategory.CategoryLevelSpecified = ebayItem.FreeAddedCategory.CategoryLevelSpecified;

                        if (ebayItem.FreeAddedCategory.CategoryID != null)
                        {
                            treecatItem.FreeAddedCategory.CategoryID = ebayItem.FreeAddedCategory.CategoryID;
                        }

                        if (ebayItem.FreeAddedCategory.CategoryName != null)
                        {
                            treecatItem.FreeAddedCategory.CategoryName = ebayItem.FreeAddedCategory.CategoryName;
                        }

                        if (ebayItem.FreeAddedCategory.CategoryParentID != null)
                        {
                            treecatItem.FreeAddedCategory.CategoryParentID = ebayItem.FreeAddedCategory.CategoryParentID;
                        }

                        if (ebayItem.FreeAddedCategory.CategoryParentName != null)
                        {
                            treecatItem.FreeAddedCategory.CategoryParentName = ebayItem.FreeAddedCategory.CategoryParentName;
                        }

                        if (ebayItem.FreeAddedCategory.CharacteristicsSets != null)
                        {
                            treecatItem.FreeAddedCategory.CharacteristicsSets = new CharacteristicsSetType[ebayItem.FreeAddedCategory.CharacteristicsSets.Length];
                            for (int i = 0; ebayItem.FreeAddedCategory.CharacteristicsSets.Length > i; i++)
                            {
                                treecatItem.FreeAddedCategory.CharacteristicsSets[i] = new CharacteristicsSetType();
                                treecatItem.FreeAddedCategory.CharacteristicsSets[i].Any = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Any;
                                treecatItem.FreeAddedCategory.CharacteristicsSets[i].AttributeSetID = ebayItem.FreeAddedCategory.CharacteristicsSets[i].AttributeSetID;
                                treecatItem.FreeAddedCategory.CharacteristicsSets[i].AttributeSetIDSpecified = ebayItem.FreeAddedCategory.CharacteristicsSets[i].AttributeSetIDSpecified;

                                if (ebayItem.FreeAddedCategory.CharacteristicsSets[i].AttributeSetVersion != null)
                                {
                                    treecatItem.FreeAddedCategory.CharacteristicsSets[i].AttributeSetVersion = ebayItem.FreeAddedCategory.CharacteristicsSets[i].AttributeSetVersion;
                                }

                                if (ebayItem.FreeAddedCategory.CharacteristicsSets[i].Name != null)
                                {
                                    treecatItem.FreeAddedCategory.CharacteristicsSets[i].Name = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Name;
                                }

                                if (ebayItem.FreeAddedCategory.CharacteristicsSets != null)
                                {
                                    treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics = new CharacteristicType[ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics.Length];
                                    for (int x = 0; ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics.Length > x; x++)
                                    {
                                        treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x] = new CharacteristicType();
                                        treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].Any = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].Any;
                                        treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].AttributeID = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].AttributeID;
                                        if (ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].DateFormat != null)
                                        {
                                            treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].DateFormat = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].DateFormat;
                                        }
                                        if (ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].DisplaySequence != null)
                                        {
                                            treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].DisplaySequence = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].DisplaySequence;
                                        }
                                        if (ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].DisplayUOM != null)
                                        {
                                            treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].DisplayUOM = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].DisplayUOM;
                                        }
                                        treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].SortOrder = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].SortOrder;
                                        treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].SortOrderSpecified = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].SortOrderSpecified;

                                        if (ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].Label != null)
                                        {
                                            treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].Label = new LabelType();
                                            if (ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].Label.Name != null)
                                            {
                                                treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].Label.Name = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].Label.Name;
                                            }

                                            treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].Label.Any = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].Label.Any;
                                            treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].Label.visible = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].Label.visible;
                                            treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].Label.visibleSpecified = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].Label.visibleSpecified;
                                        }

                                        if (ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList != null)
                                        {
                                            treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList = new ValType[ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList.Length];
                                            for (int z = 0; ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList.Length > z; z++)
                                            {
                                                treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList[z] = new ValType();
                                                if (ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList[z].SuggestedValueLiteral != null)
                                                {
                                                    treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList[z].SuggestedValueLiteral = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList[z].SuggestedValueLiteral;
                                                }

                                                if (ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList[z].ValueLiteral != null)
                                                {
                                                    treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList[z].ValueLiteral = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList[z].ValueLiteral;
                                                }

                                                treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList[z].Any = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList[z].Any;
                                                treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList[z].ValueID = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList[z].ValueID;
                                                treecatItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList[z].ValueIDSpecified = ebayItem.FreeAddedCategory.CharacteristicsSets[i].Characteristics[x].ValueList[z].ValueIDSpecified;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }


                    if (ebayItem.FreeAddedCategory != null)
                    {
                        treecatItem.FreeAddedCategory = new CategoryType();

                        if (ebayItem.FreeAddedCategory.Keywords != null)
                        {
                            treecatItem.FreeAddedCategory.Keywords = ebayItem.FreeAddedCategory.Keywords;
                        }

                        treecatItem.FreeAddedCategory.Expired = ebayItem.FreeAddedCategory.Expired;
                        treecatItem.FreeAddedCategory.ExpiredSpecified = ebayItem.FreeAddedCategory.ExpiredSpecified;
                        treecatItem.FreeAddedCategory.IntlAutosFixedCat = ebayItem.FreeAddedCategory.IntlAutosFixedCat;
                        treecatItem.FreeAddedCategory.IntlAutosFixedCatSpecified = ebayItem.FreeAddedCategory.IntlAutosFixedCatSpecified;
                        treecatItem.FreeAddedCategory.LeafCategory = ebayItem.FreeAddedCategory.LeafCategory;
                        treecatItem.FreeAddedCategory.LeafCategorySpecified = ebayItem.FreeAddedCategory.LeafCategorySpecified;
                        treecatItem.FreeAddedCategory.LSD = ebayItem.FreeAddedCategory.LSD;
                        treecatItem.FreeAddedCategory.LSDSpecified = ebayItem.FreeAddedCategory.LSDSpecified;
                        treecatItem.FreeAddedCategory.NumOfItems = ebayItem.FreeAddedCategory.NumOfItems;
                        treecatItem.FreeAddedCategory.NumOfItemsSpecified = ebayItem.FreeAddedCategory.NumOfItemsSpecified;
                        treecatItem.FreeAddedCategory.ORPA = ebayItem.FreeAddedCategory.ORPA;
                        treecatItem.FreeAddedCategory.ORPASpecified = ebayItem.FreeAddedCategory.ORPASpecified;
                        treecatItem.FreeAddedCategory.ORRA = ebayItem.FreeAddedCategory.ORRA;
                        treecatItem.FreeAddedCategory.ORRASpecified = ebayItem.FreeAddedCategory.ORRASpecified;
                        treecatItem.FreeAddedCategory.ProductSearchPageAvailable = ebayItem.FreeAddedCategory.ProductSearchPageAvailable;
                        treecatItem.FreeAddedCategory.ProductSearchPageAvailableSpecified = ebayItem.FreeAddedCategory.ProductSearchPageAvailableSpecified;
                        treecatItem.FreeAddedCategory.SellerGuaranteeEligible = ebayItem.FreeAddedCategory.SellerGuaranteeEligible;
                        treecatItem.FreeAddedCategory.SellerGuaranteeEligibleSpecified = ebayItem.FreeAddedCategory.SellerGuaranteeEligibleSpecified;
                        treecatItem.FreeAddedCategory.Virtual = ebayItem.FreeAddedCategory.Virtual;
                        treecatItem.FreeAddedCategory.VirtualSpecified = ebayItem.FreeAddedCategory.VirtualSpecified;

                        if (ebayItem.FreeAddedCategory.ProductFinderIDs != null)
                        {
                            treecatItem.FreeAddedCategory.ProductFinderIDs = new ExtendedProductFinderIDType[ebayItem.FreeAddedCategory.ProductFinderIDs.Length];
                            for (int i = 0; ebayItem.FreeAddedCategory.ProductFinderIDs.Length > i; i++)
                            {
                                treecatItem.FreeAddedCategory.ProductFinderIDs[i] = new ExtendedProductFinderIDType();
                                treecatItem.FreeAddedCategory.ProductFinderIDs[i].ProductFinderBuySide = ebayItem.FreeAddedCategory.ProductFinderIDs[i].ProductFinderBuySide;
                                treecatItem.FreeAddedCategory.ProductFinderIDs[i].ProductFinderBuySideSpecified = ebayItem.FreeAddedCategory.ProductFinderIDs[i].ProductFinderBuySideSpecified;
                                treecatItem.FreeAddedCategory.ProductFinderIDs[i].ProductFinderID = ebayItem.FreeAddedCategory.ProductFinderIDs[i].ProductFinderID;
                                treecatItem.FreeAddedCategory.ProductFinderIDs[i].ProductFinderIDSpecified = ebayItem.FreeAddedCategory.ProductFinderIDs[i].ProductFinderIDSpecified;
                            }
                        }
                    }

                    if (ebayItem.GroupCategoryID != null)
                    {
                        treecatItem.GroupCategoryID = ebayItem.GroupCategoryID;
                    }

                    treecatItem.GetItFast = ebayItem.GetItFast;
                    treecatItem.GetItFastSpecified = ebayItem.GetItFastSpecified;
                    treecatItem.HideFromSearch = ebayItem.HideFromSearch;
                    treecatItem.HideFromSearchSpecified = ebayItem.HideFromSearchSpecified;
                    treecatItem.HitCount = ebayItem.HitCount;
                    treecatItem.HitCounter = ebayItem.HitCounter;
                    treecatItem.HitCounterSpecified = ebayItem.HitCounterSpecified;
                    treecatItem.HitCountSpecified = ebayItem.HitCountSpecified;
                    treecatItem.IgnoreQuantity = ebayItem.IgnoreQuantity;
                    treecatItem.IgnoreQuantitySpecified = ebayItem.IgnoreQuantitySpecified;
                    treecatItem.IncludeRecommendations = ebayItem.IncludeRecommendations;
                    treecatItem.IntegratedMerchantCreditCardEnabled = ebayItem.IntegratedMerchantCreditCardEnabled;
                    treecatItem.IntegratedMerchantCreditCardEnabledSpecified = ebayItem.IntegratedMerchantCreditCardEnabledSpecified;
                    treecatItem.InventoryTrackingMethod = ebayItem.InventoryTrackingMethod;
                    treecatItem.InventoryTrackingMethodSpecified = ebayItem.InventoryTrackingMethodSpecified;
                    treecatItem.IsIntermediatedShippingEligible = ebayItem.IsIntermediatedShippingEligible;
                    treecatItem.IsIntermediatedShippingEligibleSpecified = ebayItem.IsIntermediatedShippingEligibleSpecified;
                    treecatItem.IsSecureDescription = ebayItem.IsSecureDescription;
                    treecatItem.IsSecureDescriptionSpecified = ebayItem.IsSecureDescriptionSpecified;
                    treecatItem.ItemCompatibilityCount = ebayItem.ItemCompatibilityCount;
                    treecatItem.ItemCompatibilityCountSpecified = ebayItem.ItemCompatibilityCountSpecified;

                    if (ebayItem.ItemCompatibilityList != null)
                    {
                        treecatItem.ItemCompatibilityList = new ItemCompatibilityListType();

                        treecatItem.ItemCompatibilityList.Any = ebayItem.ItemCompatibilityList.Any;
                        treecatItem.ItemCompatibilityList.ReplaceAll = ebayItem.ItemCompatibilityList.ReplaceAll;
                        treecatItem.ItemCompatibilityList.ReplaceAllSpecified = ebayItem.ItemCompatibilityList.ReplaceAllSpecified;

                        if (ebayItem.ItemCompatibilityList.Compatibility != null)
                        {
                            treecatItem.ItemCompatibilityList.Compatibility = new ItemCompatibilityType[ebayItem.ItemCompatibilityList.Compatibility.Length];
                            for (int i = 0; ebayItem.ItemCompatibilityList.Compatibility.Length > i; i++)
                            {
                                treecatItem.ItemCompatibilityList.Compatibility[i] = new ItemCompatibilityType();
                                if (ebayItem.ItemCompatibilityList.Compatibility[i].CompatibilityNotes != null)
                                {
                                    treecatItem.ItemCompatibilityList.Compatibility[i].CompatibilityNotes = ebayItem.ItemCompatibilityList.Compatibility[i].CompatibilityNotes;
                                }

                                treecatItem.ItemCompatibilityList.Compatibility[i].Any = ebayItem.ItemCompatibilityList.Compatibility[i].Any;
                                treecatItem.ItemCompatibilityList.Compatibility[i].Delete = ebayItem.ItemCompatibilityList.Compatibility[i].Delete;
                                treecatItem.ItemCompatibilityList.Compatibility[i].DeleteSpecified = ebayItem.ItemCompatibilityList.Compatibility[i].DeleteSpecified;

                                if (ebayItem.ItemCompatibilityList.Compatibility[i].NameValueList != null)
                                {
                                    treecatItem.ItemCompatibilityList.Compatibility[i].NameValueList = new NameValueListType[ebayItem.ItemCompatibilityList.Compatibility[i].NameValueList.Length];
                                    for (int y = 0; ebayItem.ItemCompatibilityList.Compatibility[i].NameValueList.Length > y; y++)
                                    {
                                        treecatItem.ItemCompatibilityList.Compatibility[i].NameValueList[y] = new NameValueListType();
                                        treecatItem.ItemCompatibilityList.Compatibility[i].NameValueList[y].Any = ebayItem.ItemCompatibilityList.Compatibility[i].NameValueList[y].Any;
                                        treecatItem.ItemCompatibilityList.Compatibility[i].NameValueList[y].Source = ebayItem.ItemCompatibilityList.Compatibility[i].NameValueList[y].Source;
                                        treecatItem.ItemCompatibilityList.Compatibility[i].NameValueList[y].SourceSpecified = ebayItem.ItemCompatibilityList.Compatibility[i].NameValueList[y].SourceSpecified;

                                        if (ebayItem.ItemCompatibilityList.Compatibility[i].NameValueList[y].Name != null)
                                        {
                                            treecatItem.ItemCompatibilityList.Compatibility[i].NameValueList[y].Name = ebayItem.ItemCompatibilityList.Compatibility[i].NameValueList[y].Name;
                                        }

                                        if (ebayItem.ItemCompatibilityList.Compatibility[i].NameValueList[y].Value != null)
                                        {
                                            treecatItem.ItemCompatibilityList.Compatibility[i].NameValueList[y].Value = ebayItem.ItemCompatibilityList.Compatibility[i].NameValueList[y].Value;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (ebayItem.ItemPolicyViolation != null)
                    {
                        treecatItem.ItemPolicyViolation = new ItemPolicyViolationType();

                        treecatItem.ItemPolicyViolation.Any = ebayItem.ItemPolicyViolation.Any;
                        treecatItem.ItemPolicyViolation.PolicyID = ebayItem.ItemPolicyViolation.PolicyID;
                        treecatItem.ItemPolicyViolation.PolicyIDSpecified = ebayItem.ItemPolicyViolation.PolicyIDSpecified;

                        if (ebayItem.ItemPolicyViolation.PolicyText != null)
                        {
                            treecatItem.ItemPolicyViolation.PolicyText = ebayItem.ItemPolicyViolation.PolicyText;
                        }
                    }

                    if (ebayItem.ItemSpecifics != null)
                    {
                        treecatItem.ItemSpecifics = new NameValueListType[ebayItem.ItemSpecifics.Length];
                        for (int i = 0; ebayItem.ItemSpecifics.Length > i; i++)
                        {
                            treecatItem.ItemSpecifics[i] = new NameValueListType();
                            treecatItem.ItemSpecifics[i].Any = ebayItem.ItemSpecifics[i].Any;
                            treecatItem.ItemSpecifics[i].Source = ebayItem.ItemSpecifics[i].Source;
                            treecatItem.ItemSpecifics[i].SourceSpecified = ebayItem.ItemSpecifics[i].SourceSpecified;

                            if (ebayItem.ItemSpecifics[i].Name != null)
                            {
                                treecatItem.ItemSpecifics[i].Name = ebayItem.ItemSpecifics[i].Name;
                            }

                            if (ebayItem.ItemSpecifics[i].Value != null)
                            {
                                treecatItem.ItemSpecifics[i].Value = ebayItem.ItemSpecifics[i].Value;
                            }
                        }
                    }

                    treecatItem.LeadCount = ebayItem.LeadCount;
                    treecatItem.LeadCountSpecified = ebayItem.LeadCountSpecified;
                    treecatItem.LimitedWarrantyEligible = ebayItem.LimitedWarrantyEligible;
                    treecatItem.LimitedWarrantyEligibleSpecified = ebayItem.LimitedWarrantyEligibleSpecified;

                    if (ebayItem.ListingDesigner != null)
                    {
                        treecatItem.ListingDesigner = new ListingDesignerType();
                        treecatItem.ListingDesigner.Any = ebayItem.ListingDesigner.Any;
                        treecatItem.ListingDesigner.LayoutID = ebayItem.ListingDesigner.LayoutID;
                        treecatItem.ListingDesigner.LayoutIDSpecified = ebayItem.ListingDesigner.LayoutIDSpecified;
                        treecatItem.ListingDesigner.OptimalPictureSize = ebayItem.ListingDesigner.OptimalPictureSize;
                        treecatItem.ListingDesigner.OptimalPictureSizeSpecified = ebayItem.ListingDesigner.OptimalPictureSizeSpecified;
                        treecatItem.ListingDesigner.ThemeID = ebayItem.ListingDesigner.ThemeID;
                        treecatItem.ListingDesigner.ThemeIDSpecified = ebayItem.ListingDesigner.ThemeIDSpecified;
                    }

                    if (ebayItem.ListingDetails != null)
                    {
                        treecatItem.ListingDetails = new ListingDetailsType();
                        treecatItem.ListingDetails.Adult = ebayItem.ListingDetails.Adult;
                        treecatItem.ListingDetails.AdultSpecified = ebayItem.ListingDetails.AdultSpecified;
                        treecatItem.ListingDetails.Any = ebayItem.ListingDetails.Any;
                        treecatItem.ListingDetails.BindingAuction = ebayItem.ListingDetails.BindingAuction;
                        treecatItem.ListingDetails.BindingAuctionSpecified = ebayItem.ListingDetails.BindingAuctionSpecified;
                        treecatItem.ListingDetails.BuyItNowAvailable = ebayItem.ListingDetails.BuyItNowAvailable;
                        treecatItem.ListingDetails.BuyItNowAvailableSpecified = ebayItem.ListingDetails.BuyItNowAvailableSpecified;
                        treecatItem.ListingDetails.CheckoutEnabled = ebayItem.ListingDetails.CheckoutEnabled;
                        treecatItem.ListingDetails.CheckoutEnabledSpecified = ebayItem.ListingDetails.CheckoutEnabledSpecified;
                        treecatItem.ListingDetails.EndingReason = ebayItem.ListingDetails.EndingReason;
                        treecatItem.ListingDetails.EndingReasonSpecified = ebayItem.ListingDetails.EndingReasonSpecified;
                        treecatItem.ListingDetails.EndTime = ebayItem.ListingDetails.EndTime;
                        treecatItem.ListingDetails.EndTime = ebayItem.ListingDetails.EndTime;
                        treecatItem.ListingDetails.EndTimeSpecified = ebayItem.ListingDetails.EndTimeSpecified;
                        treecatItem.ListingDetails.HasPublicMessages = ebayItem.ListingDetails.HasPublicMessages;
                        treecatItem.ListingDetails.HasPublicMessagesSpecified = ebayItem.ListingDetails.HasPublicMessagesSpecified;
                        treecatItem.ListingDetails.HasReservePrice = ebayItem.ListingDetails.HasReservePrice;
                        treecatItem.ListingDetails.HasReservePriceSpecified = ebayItem.ListingDetails.HasReservePriceSpecified;
                        treecatItem.ListingDetails.HasUnansweredQuestions = ebayItem.ListingDetails.HasUnansweredQuestions;
                        treecatItem.ListingDetails.HasUnansweredQuestionsSpecified = ebayItem.ListingDetails.HasUnansweredQuestionsSpecified;
                        treecatItem.ListingDetails.PayPerLeadEnabled = ebayItem.ListingDetails.PayPerLeadEnabled;
                        treecatItem.ListingDetails.PayPerLeadEnabledSpecified = ebayItem.ListingDetails.PayPerLeadEnabledSpecified;
                        treecatItem.ListingDetails.SellerBusinessType = ebayItem.ListingDetails.SellerBusinessType;
                        treecatItem.ListingDetails.SellerBusinessTypeSpecified = ebayItem.ListingDetails.SellerBusinessTypeSpecified;
                        treecatItem.ListingDetails.StartTime = ebayItem.ListingDetails.StartTime;
                        treecatItem.ListingDetails.StartTimeSpecified = ebayItem.ListingDetails.StartTimeSpecified;

                        if (ebayItem.ListingDetails.BestOfferAutoAcceptPrice != null)
                        {
                            treecatItem.ListingDetails.BestOfferAutoAcceptPrice = new AmountType();
                            treecatItem.ListingDetails.BestOfferAutoAcceptPrice.currencyID = ebayItem.ListingDetails.BestOfferAutoAcceptPrice.currencyID;
                            treecatItem.ListingDetails.BestOfferAutoAcceptPrice.Value = ebayItem.ListingDetails.BestOfferAutoAcceptPrice.Value;
                        }

                        if (ebayItem.ListingDetails.ConvertedBuyItNowPrice != null)
                        {
                            treecatItem.ListingDetails.ConvertedBuyItNowPrice = new AmountType();
                            treecatItem.ListingDetails.ConvertedBuyItNowPrice.currencyID = ebayItem.ListingDetails.ConvertedBuyItNowPrice.currencyID;
                            treecatItem.ListingDetails.ConvertedBuyItNowPrice.Value = ebayItem.ListingDetails.ConvertedBuyItNowPrice.Value;
                        }

                        if (ebayItem.ListingDetails.ConvertedReservePrice != null)
                        {
                            treecatItem.ListingDetails.ConvertedReservePrice = new AmountType();
                            treecatItem.ListingDetails.ConvertedReservePrice.currencyID = ebayItem.ListingDetails.ConvertedReservePrice.currencyID;
                            treecatItem.ListingDetails.ConvertedReservePrice.Value = ebayItem.ListingDetails.ConvertedReservePrice.Value;
                        }

                        if (ebayItem.ListingDetails.ConvertedStartPrice != null)
                        {
                            treecatItem.ListingDetails.ConvertedStartPrice = new AmountType();
                            treecatItem.ListingDetails.ConvertedStartPrice.currencyID = ebayItem.ListingDetails.ConvertedStartPrice.currencyID;
                            treecatItem.ListingDetails.ConvertedStartPrice.Value = ebayItem.ListingDetails.ConvertedStartPrice.Value;
                        }

                        if (ebayItem.ListingDetails.LocalListingDistance != null)
                        {
                            treecatItem.ListingDetails.LocalListingDistance = ebayItem.ListingDetails.LocalListingDistance;
                        }

                        if (ebayItem.ListingDetails.MinimumBestOfferMessage != null)
                        {
                            treecatItem.ListingDetails.MinimumBestOfferMessage = ebayItem.ListingDetails.MinimumBestOfferMessage;
                        }

                        if (ebayItem.ListingDetails.MinimumBestOfferPrice != null)
                        {
                            treecatItem.ListingDetails.MinimumBestOfferPrice = new AmountType();
                            treecatItem.ListingDetails.MinimumBestOfferPrice.currencyID = ebayItem.ListingDetails.MinimumBestOfferPrice.currencyID;
                            treecatItem.ListingDetails.MinimumBestOfferPrice.Value = ebayItem.ListingDetails.MinimumBestOfferPrice.Value;
                        }

                        if (ebayItem.ListingDetails.RelistedItemID != null)
                        {
                            treecatItem.ListingDetails.RelistedItemID = ebayItem.ListingDetails.RelistedItemID;
                        }

                        if (ebayItem.ListingDetails.SecondChanceOriginalItemID != null)
                        {
                            treecatItem.ListingDetails.SecondChanceOriginalItemID = ebayItem.ListingDetails.SecondChanceOriginalItemID;
                        }

                        if (ebayItem.ListingDetails.TCROriginalItemID != null)
                        {
                            treecatItem.ListingDetails.TCROriginalItemID = ebayItem.ListingDetails.TCROriginalItemID;
                        }

                        if (ebayItem.ListingDetails.ViewItemURL != null)
                        {
                            treecatItem.ListingDetails.ViewItemURL = ebayItem.ListingDetails.ViewItemURL;
                        }

                        if (ebayItem.ListingDetails.ViewItemURLForNaturalSearch != null)
                        {
                            treecatItem.ListingDetails.ViewItemURLForNaturalSearch = ebayItem.ListingDetails.ViewItemURLForNaturalSearch;
                        }
                    }

                    if (ebayItem.ListingDuration != null)
                    {
                        treecatItem.ListingDuration = ebayItem.ListingDuration;
                    }

                    if (ebayItem.Location != null)
                    {
                        treecatItem.Location = ebayItem.Location;
                    }

                    treecatItem.ListingEnhancement = ebayItem.ListingEnhancement;
                    treecatItem.ListingSubtype2 = ebayItem.ListingSubtype2;
                    treecatItem.ListingSubtype2Specified = ebayItem.ListingSubtype2Specified;
                    treecatItem.ListingType = ebayItem.ListingType;
                    treecatItem.ListingTypeSpecified = ebayItem.ListingTypeSpecified;
                    treecatItem.LiveAuction = ebayItem.LiveAuction;
                    treecatItem.LiveAuctionSpecified = ebayItem.LiveAuctionSpecified;
                    treecatItem.LocalListing = ebayItem.LocalListing;
                    treecatItem.LocalListingSpecified = ebayItem.LocalListingSpecified;
                    treecatItem.LocationDefaulted = ebayItem.LocationDefaulted;
                    treecatItem.LocationDefaultedSpecified = ebayItem.LocationDefaultedSpecified;

                    if (ebayItem.LookupAttributeArray != null)
                    {
                        treecatItem.LookupAttributeArray = new LookupAttributeType[ebayItem.LookupAttributeArray.Length];
                        for (int i = 0; ebayItem.LookupAttributeArray.Length > i; i++)
                        {
                            treecatItem.LookupAttributeArray[i] = new LookupAttributeType();
                            treecatItem.LookupAttributeArray[i].Any = ebayItem.LookupAttributeArray[i].Any;

                            if (ebayItem.LookupAttributeArray[i].Name != null)
                            {
                                treecatItem.LookupAttributeArray[i].Name = ebayItem.LookupAttributeArray[i].Name;
                            }

                            if (ebayItem.LookupAttributeArray[i].Value != null)
                            {
                                treecatItem.LookupAttributeArray[i].Value = ebayItem.LookupAttributeArray[i].Value;
                            }
                        }
                    }

                    treecatItem.LotSize = ebayItem.LotSize;
                    treecatItem.LotSizeSpecified = ebayItem.LotSizeSpecified;
                    treecatItem.MechanicalCheckAccepted = ebayItem.MechanicalCheckAccepted;
                    treecatItem.MechanicalCheckAcceptedSpecified = ebayItem.MechanicalCheckAcceptedSpecified;
                    treecatItem.NewLeadCount = ebayItem.NewLeadCount;
                    treecatItem.NewLeadCountSpecified = ebayItem.NewLeadCountSpecified;
                    treecatItem.PartnerCode = ebayItem.PartnerCode;
                    treecatItem.PaymentAllowedSite = ebayItem.PaymentAllowedSite;

                    if (ebayItem.PartnerCode != null)
                    {
                        treecatItem.PartnerCode = ebayItem.PartnerCode;
                    }

                    if (ebayItem.PartnerName != null)
                    {
                        treecatItem.PartnerName = ebayItem.PartnerName;
                    }

                    if (ebayItem.PaymentDetails != null)
                    {
                        treecatItem.PaymentDetails = new PaymentDetailsType();
                        treecatItem.PaymentDetails.Any = ebayItem.PaymentDetails.Any;
                        treecatItem.PaymentDetails.DaysToFullPayment = ebayItem.PaymentDetails.DaysToFullPayment;
                        treecatItem.PaymentDetails.DaysToFullPaymentSpecified = ebayItem.PaymentDetails.DaysToFullPaymentSpecified;

                        if (ebayItem.PaymentDetails.DepositAmount != null)
                        {
                            treecatItem.PaymentDetails.DepositAmount = new AmountType();
                            treecatItem.PaymentDetails.DepositAmount.currencyID = ebayItem.PaymentDetails.DepositAmount.currencyID;
                            treecatItem.PaymentDetails.DepositAmount.Value = ebayItem.PaymentDetails.DepositAmount.Value;
                        }

                        treecatItem.PaymentDetails.DepositType = ebayItem.PaymentDetails.DepositType;
                        treecatItem.PaymentDetails.DepositTypeSpecified = ebayItem.PaymentDetails.DepositTypeSpecified;
                        treecatItem.PaymentDetails.HoursToDeposit = ebayItem.PaymentDetails.HoursToDeposit;
                        treecatItem.PaymentDetails.HoursToDepositSpecified = ebayItem.PaymentDetails.HoursToDepositSpecified;
                    }

                    treecatItem.PaymentMethods = ebayItem.PaymentMethods;

                    if (ebayItem.PayPalEmailAddress != null)
                    {
                        treecatItem.PayPalEmailAddress = ebayItem.PayPalEmailAddress;
                    }

                    if (ebayItem.PickupInStoreDetails != null)
                    {
                        treecatItem.PickupInStoreDetails = new PickupInStoreDetailsType();
                        treecatItem.PickupInStoreDetails.Any = ebayItem.PickupInStoreDetails.Any;
                        treecatItem.PickupInStoreDetails.EligibleForPickupDropOff = ebayItem.PickupInStoreDetails.EligibleForPickupDropOff;
                        treecatItem.PickupInStoreDetails.EligibleForPickupDropOffSpecified = ebayItem.PickupInStoreDetails.EligibleForPickupDropOffSpecified;
                        treecatItem.PickupInStoreDetails.EligibleForPickupInStore = ebayItem.PickupInStoreDetails.EligibleForPickupInStore;
                        treecatItem.PickupInStoreDetails.EligibleForPickupInStoreSpecified = ebayItem.PickupInStoreDetails.EligibleForPickupInStoreSpecified;
                    }

                    if (ebayItem.PictureDetails != null)
                    {
                        treecatItem.PictureDetails = new PictureDetailsType();
                        if (ebayItem.PictureDetails.ExtendedPictureDetails != null)
                        {
                            treecatItem.PictureDetails.ExtendedPictureDetails = new ExtendedPictureDetailsType();
                            treecatItem.PictureDetails.ExtendedPictureDetails.Any = ebayItem.PictureDetails.ExtendedPictureDetails.Any;

                            if (ebayItem.PictureDetails.ExtendedPictureDetails.PictureURLs != null)
                            {
                                treecatItem.PictureDetails.ExtendedPictureDetails.PictureURLs = new PictureURLsType[ebayItem.PictureDetails.ExtendedPictureDetails.PictureURLs.Length];
                                for (int i = 0; ebayItem.PictureDetails.ExtendedPictureDetails.PictureURLs.Length > i; i++)
                                {
                                    treecatItem.PictureDetails.ExtendedPictureDetails.PictureURLs[i] = new PictureURLsType();
                                    treecatItem.PictureDetails.ExtendedPictureDetails.PictureURLs[i].Any = ebayItem.PictureDetails.ExtendedPictureDetails.PictureURLs[i].Any;

                                    if (ebayItem.PictureDetails.ExtendedPictureDetails.PictureURLs[i].eBayPictureURL != null)
                                    {
                                        treecatItem.PictureDetails.ExtendedPictureDetails.PictureURLs[i].eBayPictureURL = ebayItem.PictureDetails.ExtendedPictureDetails.PictureURLs[i].eBayPictureURL;
                                    }

                                    if (ebayItem.PictureDetails.ExtendedPictureDetails.PictureURLs[i].ExternalPictureURL != null)
                                    {
                                        treecatItem.PictureDetails.ExtendedPictureDetails.PictureURLs[i].ExternalPictureURL = ebayItem.PictureDetails.ExtendedPictureDetails.PictureURLs[i].ExternalPictureURL;
                                    }
                                }
                            }
                        }

                        if (ebayItem.PictureDetails.ExternalPictureURL != null)
                        {
                            treecatItem.PictureDetails.ExternalPictureURL = ebayItem.PictureDetails.ExternalPictureURL;
                        }

                        if (ebayItem.PictureDetails.GalleryErrorInfo != null)
                        {
                            treecatItem.PictureDetails.GalleryErrorInfo = ebayItem.PictureDetails.GalleryErrorInfo;
                        }

                        if (ebayItem.PictureDetails.GalleryURL != null)
                        {
                            treecatItem.PictureDetails.GalleryURL = ebayItem.PictureDetails.GalleryURL;
                        }

                        if (ebayItem.PictureDetails.PictureURL != null)
                        {
                            treecatItem.PictureDetails.PictureURL = ebayItem.PictureDetails.PictureURL;
                        }

                        treecatItem.PictureDetails.Any = ebayItem.PictureDetails.Any;
                        treecatItem.PictureDetails.GalleryStatus = ebayItem.PictureDetails.GalleryStatus;
                        treecatItem.PictureDetails.GalleryStatusSpecified = ebayItem.PictureDetails.GalleryStatusSpecified;
                        treecatItem.PictureDetails.GalleryType = ebayItem.PictureDetails.GalleryType;
                        treecatItem.PictureDetails.GalleryTypeSpecified = ebayItem.PictureDetails.GalleryTypeSpecified;
                        treecatItem.PictureDetails.PhotoDisplay = ebayItem.PictureDetails.PhotoDisplay;
                        treecatItem.PictureDetails.PhotoDisplaySpecified = ebayItem.PictureDetails.PhotoDisplaySpecified;
                        treecatItem.PictureDetails.PictureSource = ebayItem.PictureDetails.PictureSource;
                        treecatItem.PictureDetails.PictureSourceSpecified = ebayItem.PictureDetails.PictureSourceSpecified;
                    }

                    if (ebayItem.PostalCode != null)
                    {
                        treecatItem.PostalCode = ebayItem.PostalCode;
                    }

                    if (ebayItem.PrimaryCategory != null)
                    {
                        treecatItem.PrimaryCategory = new CategoryType();
                        treecatItem.PrimaryCategory.Any = ebayItem.PrimaryCategory.Any;
                        treecatItem.PrimaryCategory.AutoPayEnabled = ebayItem.PrimaryCategory.AutoPayEnabled;
                        treecatItem.PrimaryCategory.AutoPayEnabledSpecified = ebayItem.PrimaryCategory.AutoPayEnabledSpecified;
                        treecatItem.PrimaryCategory.B2BVATEnabled = ebayItem.PrimaryCategory.B2BVATEnabled;
                        treecatItem.PrimaryCategory.B2BVATEnabledSpecified = ebayItem.PrimaryCategory.B2BVATEnabledSpecified;
                        treecatItem.PrimaryCategory.BestOfferEnabled = ebayItem.PrimaryCategory.BestOfferEnabled;
                        treecatItem.PrimaryCategory.BestOfferEnabledSpecified = ebayItem.PrimaryCategory.BestOfferEnabledSpecified;
                        treecatItem.PrimaryCategory.CatalogEnabled = ebayItem.PrimaryCategory.CatalogEnabled;
                        treecatItem.PrimaryCategory.CatalogEnabledSpecified = ebayItem.PrimaryCategory.CatalogEnabledSpecified;
                        treecatItem.PrimaryCategory.CategoryLevel = ebayItem.PrimaryCategory.CategoryLevel;
                        treecatItem.PrimaryCategory.CategoryLevelSpecified = ebayItem.PrimaryCategory.CategoryLevelSpecified;

                        if (ebayItem.PrimaryCategory.CategoryID != null)
                        {
                            treecatItem.PrimaryCategory.CategoryID = ebayItem.PrimaryCategory.CategoryID;
                        }

                        if (ebayItem.PrimaryCategory.CategoryName != null)
                        {
                            treecatItem.PrimaryCategory.CategoryName = ebayItem.PrimaryCategory.CategoryName;
                        }

                        if (ebayItem.PrimaryCategory.CategoryParentID != null)
                        {
                            treecatItem.PrimaryCategory.CategoryParentID = ebayItem.PrimaryCategory.CategoryParentID;
                        }

                        if (ebayItem.PrimaryCategory.CategoryParentName != null)
                        {
                            treecatItem.PrimaryCategory.CategoryParentName = ebayItem.PrimaryCategory.CategoryParentName;
                        }

                        if (ebayItem.PrimaryCategory.CharacteristicsSets != null)
                        {
                            treecatItem.PrimaryCategory.CharacteristicsSets = new CharacteristicsSetType[ebayItem.PrimaryCategory.CharacteristicsSets.Length];
                            for (int i = 0; ebayItem.PrimaryCategory.CharacteristicsSets.Length > i; i++)
                            {
                                treecatItem.PrimaryCategory.CharacteristicsSets[i] = new CharacteristicsSetType();
                                treecatItem.PrimaryCategory.CharacteristicsSets[i].Any = ebayItem.PrimaryCategory.CharacteristicsSets[i].Any;
                                treecatItem.PrimaryCategory.CharacteristicsSets[i].AttributeSetID = ebayItem.PrimaryCategory.CharacteristicsSets[i].AttributeSetID;
                                treecatItem.PrimaryCategory.CharacteristicsSets[i].AttributeSetIDSpecified = ebayItem.PrimaryCategory.CharacteristicsSets[i].AttributeSetIDSpecified;

                                if (ebayItem.PrimaryCategory.CharacteristicsSets[i].AttributeSetVersion != null)
                                {
                                    treecatItem.PrimaryCategory.CharacteristicsSets[i].AttributeSetVersion = ebayItem.PrimaryCategory.CharacteristicsSets[i].AttributeSetVersion;
                                }

                                if (ebayItem.PrimaryCategory.CharacteristicsSets[i].Name != null)
                                {
                                    treecatItem.PrimaryCategory.CharacteristicsSets[i].Name = ebayItem.PrimaryCategory.CharacteristicsSets[i].Name;
                                }

                                if (ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics != null)
                                {
                                    treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics = new CharacteristicType[ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics.Length];
                                    for (int x = 0; ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics.Length > x; x++)
                                    {
                                        treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x] = new CharacteristicType();
                                        treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].Any = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].Any;
                                        treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].AttributeID = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].AttributeID;

                                        if (ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].DateFormat != null)
                                        {
                                            treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].DateFormat = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].DateFormat;
                                        }

                                        if (ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].DisplaySequence != null)
                                        {
                                            treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].DisplayUOM = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].DisplaySequence;
                                        }

                                        if (ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].Label != null)
                                        {
                                            treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].Label = new LabelType();
                                            treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].Label.Any = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].Label.Any;
                                            treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].Label.visible = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].Label.visible;
                                            treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].Label.visibleSpecified = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].Label.visibleSpecified;

                                            if (ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].Label.Name != null)
                                            {
                                                treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].Label.Name = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].Label.Name;
                                            }
                                        }

                                        treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].SortOrder = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].SortOrder;
                                        treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].SortOrderSpecified = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].SortOrderSpecified;

                                        if (ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList != null)
                                        {
                                            treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList = new ValType[ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList.Length];
                                            for (int y = 0; ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList.Length > y; y++)
                                            {
                                                treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList[y] = new ValType();
                                                treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList[y].Any = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList[y].Any;
                                                treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList[y].ValueID = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList[y].ValueID;
                                                treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList[y].ValueIDSpecified = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList[y].ValueIDSpecified;

                                                if (ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList[y].SuggestedValueLiteral != null)
                                                {
                                                    treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList[y].SuggestedValueLiteral = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList[y].SuggestedValueLiteral;
                                                }

                                                if (ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList[y].ValueLiteral != null)
                                                {
                                                    treecatItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList[y].ValueLiteral = ebayItem.PrimaryCategory.CharacteristicsSets[i].Characteristics[x].ValueList[y].ValueLiteral;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (ebayItem.PrimaryCategory.Keywords != null)
                        {
                            treecatItem.PrimaryCategory.Keywords = ebayItem.PrimaryCategory.Keywords;
                        }

                        if (ebayItem.PrimaryCategory.ProductFinderIDs != null)
                        {
                            treecatItem.PrimaryCategory.ProductFinderIDs = new ExtendedProductFinderIDType[ebayItem.PrimaryCategory.ProductFinderIDs.Length];
                            for (int y = 0; ebayItem.PrimaryCategory.ProductFinderIDs.Length > y; y++)
                            {
                                treecatItem.PrimaryCategory.ProductFinderIDs[y] = new ExtendedProductFinderIDType();
                                treecatItem.PrimaryCategory.ProductFinderIDs[y].ProductFinderBuySide = ebayItem.PrimaryCategory.ProductFinderIDs[y].ProductFinderBuySide;
                                treecatItem.PrimaryCategory.ProductFinderIDs[y].ProductFinderBuySideSpecified = ebayItem.PrimaryCategory.ProductFinderIDs[y].ProductFinderBuySideSpecified;
                                treecatItem.PrimaryCategory.ProductFinderIDs[y].ProductFinderID = ebayItem.PrimaryCategory.ProductFinderIDs[y].ProductFinderID;
                                treecatItem.PrimaryCategory.ProductFinderIDs[y].ProductFinderIDSpecified = ebayItem.PrimaryCategory.ProductFinderIDs[y].ProductFinderIDSpecified;
                            }
                        }

                        treecatItem.PrimaryCategory.Expired = ebayItem.PrimaryCategory.Expired;
                        treecatItem.PrimaryCategory.ExpiredSpecified = ebayItem.PrimaryCategory.ExpiredSpecified;
                        treecatItem.PrimaryCategory.IntlAutosFixedCat = ebayItem.PrimaryCategory.IntlAutosFixedCat;
                        treecatItem.PrimaryCategory.IntlAutosFixedCatSpecified = ebayItem.PrimaryCategory.IntlAutosFixedCatSpecified;
                        treecatItem.PrimaryCategory.LeafCategory = ebayItem.PrimaryCategory.LeafCategory;
                        treecatItem.PrimaryCategory.LeafCategorySpecified = ebayItem.PrimaryCategory.LeafCategorySpecified;
                        treecatItem.PrimaryCategory.LSD = ebayItem.PrimaryCategory.LSD;
                        treecatItem.PrimaryCategory.LSDSpecified = ebayItem.PrimaryCategory.LSDSpecified;
                        treecatItem.PrimaryCategory.NumOfItems = ebayItem.PrimaryCategory.NumOfItems;
                        treecatItem.PrimaryCategory.NumOfItemsSpecified = ebayItem.PrimaryCategory.NumOfItemsSpecified;
                        treecatItem.PrimaryCategory.ORPA = ebayItem.PrimaryCategory.ORPA;
                        treecatItem.PrimaryCategory.ORPASpecified = ebayItem.PrimaryCategory.ORPASpecified;
                        treecatItem.PrimaryCategory.ORRA = ebayItem.PrimaryCategory.ORRA;
                        treecatItem.PrimaryCategory.ORRASpecified = ebayItem.PrimaryCategory.ORRASpecified;
                        treecatItem.PrimaryCategory.ProductSearchPageAvailable = ebayItem.PrimaryCategory.ProductSearchPageAvailable;
                        treecatItem.PrimaryCategory.ProductSearchPageAvailableSpecified = ebayItem.PrimaryCategory.ProductSearchPageAvailableSpecified;
                        treecatItem.PrimaryCategory.SellerGuaranteeEligible = ebayItem.PrimaryCategory.SellerGuaranteeEligible;
                        treecatItem.PrimaryCategory.SellerGuaranteeEligibleSpecified = ebayItem.PrimaryCategory.SellerGuaranteeEligibleSpecified;
                        treecatItem.PrimaryCategory.Virtual = ebayItem.PrimaryCategory.Virtual;
                        treecatItem.PrimaryCategory.VirtualSpecified = ebayItem.PrimaryCategory.VirtualSpecified;
                    }

                    treecatItem.PrivateListing = ebayItem.PrivateListing;
                    treecatItem.PrivateListingSpecified = ebayItem.PrivateListingSpecified;

                    if (ebayItem.PrivateNotes != null)
                    {
                        treecatItem.PrivateNotes = ebayItem.PrivateNotes;
                    }

                    if (ebayItem.ProductListingDetails != null)
                    {
                        treecatItem.ProductListingDetails = new ProductListingDetailsType();
                        if (ebayItem.ProductListingDetails.BrandMPN != null)
                        {
                            treecatItem.ProductListingDetails.BrandMPN = new BrandMPNType();
                            treecatItem.ProductListingDetails.BrandMPN.Any = ebayItem.ProductListingDetails.BrandMPN.Any;

                            if (ebayItem.ProductListingDetails.BrandMPN.Brand != null)
                            {
                                treecatItem.ProductListingDetails.BrandMPN.Brand = ebayItem.ProductListingDetails.BrandMPN.Brand;
                                treecatItem.Brand = ebayItem.ProductListingDetails.BrandMPN.Brand;
                            }

                            if (ebayItem.ProductListingDetails.BrandMPN.MPN != null)
                            {
                                treecatItem.ProductListingDetails.BrandMPN.MPN = ebayItem.ProductListingDetails.BrandMPN.MPN;
                            }
                        }

                        if (ebayItem.ProductListingDetails.Copyright != null)
                        {
                            treecatItem.ProductListingDetails.Copyright = ebayItem.ProductListingDetails.Copyright;
                        }

                        if (ebayItem.ProductListingDetails.DetailsURL != null)
                        {
                            treecatItem.ProductListingDetails.DetailsURL = ebayItem.ProductListingDetails.DetailsURL;
                        }

                        if (ebayItem.ProductListingDetails.EAN != null)
                        {
                            treecatItem.ProductListingDetails.EAN = ebayItem.ProductListingDetails.EAN;
                        }

                        if (ebayItem.ProductListingDetails.ISBN != null)
                        {
                            treecatItem.ProductListingDetails.ISBN = ebayItem.ProductListingDetails.ISBN;
                        }

                        treecatItem.ProductListingDetails.Any = ebayItem.ProductListingDetails.Any;
                        treecatItem.ProductListingDetails.IncludeeBayProductDetails = ebayItem.ProductListingDetails.IncludeeBayProductDetails;
                        treecatItem.ProductListingDetails.IncludeeBayProductDetailsSpecified = ebayItem.ProductListingDetails.IncludeeBayProductDetailsSpecified;
                        treecatItem.ProductListingDetails.IncludeStockPhotoURL = ebayItem.ProductListingDetails.IncludeStockPhotoURL;
                        treecatItem.ProductListingDetails.IncludeStockPhotoURLSpecified = ebayItem.ProductListingDetails.IncludeStockPhotoURLSpecified;

                        if (ebayItem.ProductListingDetails.NameValueList != null)
                        {
                            treecatItem.ProductListingDetails.NameValueList = new NameValueListType[ebayItem.ProductListingDetails.NameValueList.Length];
                            for (int y = 0; ebayItem.ProductListingDetails.NameValueList.Length > y; y++)
                            {
                                treecatItem.ProductListingDetails.NameValueList[y] = new NameValueListType();
                                if (ebayItem.ProductListingDetails.NameValueList[y].Name != null)
                                {
                                    treecatItem.ProductListingDetails.NameValueList[y].Name = ebayItem.ProductListingDetails.NameValueList[y].Name;
                                }

                                if (ebayItem.ProductListingDetails.NameValueList[y].Value != null)
                                {
                                    treecatItem.ProductListingDetails.NameValueList[y].Value = ebayItem.ProductListingDetails.NameValueList[y].Value;
                                }

                                treecatItem.ProductListingDetails.NameValueList[y].Any = ebayItem.ProductListingDetails.NameValueList[y].Any;
                                treecatItem.ProductListingDetails.NameValueList[y].Source = ebayItem.ProductListingDetails.NameValueList[y].Source;
                                treecatItem.ProductListingDetails.NameValueList[y].SourceSpecified = ebayItem.ProductListingDetails.NameValueList[y].SourceSpecified;
                            }
                        }

                        if (ebayItem.ProductListingDetails.ProductDetailsURL != null)
                        {
                            treecatItem.ProductListingDetails.ProductDetailsURL = ebayItem.ProductListingDetails.ProductDetailsURL;
                        }

                        if (ebayItem.ProductListingDetails.ProductReferenceID != null)
                        {
                            treecatItem.ProductListingDetails.ProductReferenceID = ebayItem.ProductListingDetails.ProductReferenceID;
                        }

                        if (ebayItem.ProductListingDetails.StockPhotoURL != null)
                        {
                            treecatItem.ProductListingDetails.StockPhotoURL = ebayItem.ProductListingDetails.StockPhotoURL;
                        }

                        treecatItem.ProductListingDetails.ReturnSearchResultOnDuplicates = ebayItem.ProductListingDetails.ReturnSearchResultOnDuplicates;
                        treecatItem.ProductListingDetails.ReturnSearchResultOnDuplicatesSpecified = ebayItem.ProductListingDetails.ReturnSearchResultOnDuplicatesSpecified;

                        if (ebayItem.ProductListingDetails.TicketListingDetails != null)
                        {
                            treecatItem.ProductListingDetails.TicketListingDetails = new TicketListingDetailsType();

                            treecatItem.ProductListingDetails.TicketListingDetails.Any = ebayItem.ProductListingDetails.TicketListingDetails.Any;

                            if (ebayItem.ProductListingDetails.TicketListingDetails.EventTitle != null)
                            {
                                treecatItem.ProductListingDetails.TicketListingDetails.EventTitle = ebayItem.ProductListingDetails.TicketListingDetails.EventTitle;
                            }

                            if (ebayItem.ProductListingDetails.TicketListingDetails.PrintedDate != null)
                            {
                                treecatItem.ProductListingDetails.TicketListingDetails.PrintedDate = ebayItem.ProductListingDetails.TicketListingDetails.PrintedDate;
                            }

                            if (ebayItem.ProductListingDetails.TicketListingDetails.PrintedTime != null)
                            {
                                treecatItem.ProductListingDetails.TicketListingDetails.PrintedTime = ebayItem.ProductListingDetails.TicketListingDetails.PrintedTime;
                            }

                            if (ebayItem.ProductListingDetails.TicketListingDetails.Venue != null)
                            {
                                treecatItem.ProductListingDetails.TicketListingDetails.Venue = ebayItem.ProductListingDetails.TicketListingDetails.Venue;
                            }
                        }

                        if (ebayItem.ProductListingDetails.UPC != null)
                        {
                            treecatItem.ProductListingDetails.UPC = ebayItem.ProductListingDetails.UPC;
                        }

                        treecatItem.ProductListingDetails.UseFirstProduct = ebayItem.ProductListingDetails.UseFirstProduct;
                        treecatItem.ProductListingDetails.UseFirstProductSpecified = ebayItem.ProductListingDetails.UseFirstProductSpecified;
                        treecatItem.ProductListingDetails.UseStockPhotoURLAsGallery = ebayItem.ProductListingDetails.UseStockPhotoURLAsGallery;
                        treecatItem.ProductListingDetails.UseStockPhotoURLAsGallerySpecified = ebayItem.ProductListingDetails.UseStockPhotoURLAsGallerySpecified;
                    }

                    treecatItem.ProxyItem = ebayItem.ProxyItem;
                    treecatItem.ProxyItemSpecified = ebayItem.ProxyItemSpecified;
                    treecatItem.Quantity = ebayItem.Quantity;
                    treecatItem.QuantityAvailable = ebayItem.QuantityAvailable;
                    treecatItem.QuantityAvailableHint = ebayItem.QuantityAvailableHint;
                    treecatItem.QuantityAvailableHintSpecified = ebayItem.QuantityAvailableHintSpecified;
                    treecatItem.QuantityAvailableSpecified = ebayItem.QuantityAvailableSpecified;

                    if (ebayItem.QuantityInfo != null)
                    {
                        treecatItem.QuantityInfo = new QuantityInfoType();
                        treecatItem.QuantityInfo.Any = ebayItem.QuantityInfo.Any;
                        treecatItem.QuantityInfo.MinimumRemnantSet = ebayItem.QuantityInfo.MinimumRemnantSet;
                        treecatItem.QuantityInfo.MinimumRemnantSetSpecified = ebayItem.QuantityInfo.MinimumRemnantSetSpecified;
                    }

                    if (ebayItem.QuantityRestrictionPerBuyer != null)
                    {
                        treecatItem.QuantityRestrictionPerBuyer = new QuantityRestrictionPerBuyerInfoType();
                        treecatItem.QuantityRestrictionPerBuyer.MaximumQuantity = ebayItem.QuantityRestrictionPerBuyer.MaximumQuantity;
                        treecatItem.QuantityRestrictionPerBuyer.MaximumQuantitySpecified = ebayItem.QuantityRestrictionPerBuyer.MaximumQuantitySpecified;
                    }

                    treecatItem.QuantitySpecified = ebayItem.QuantitySpecified;
                    treecatItem.QuantityThreshold = ebayItem.QuantityThreshold;
                    treecatItem.QuantityThresholdSpecified = ebayItem.QuantityThresholdSpecified;
                    treecatItem.QuestionCount = ebayItem.QuestionCount;
                    treecatItem.QuestionCountSpecified = ebayItem.QuestionCountSpecified;
                    treecatItem.ReasonHideFromSearch = ebayItem.ReasonHideFromSearch;
                    treecatItem.ReasonHideFromSearchSpecified = ebayItem.ReasonHideFromSearchSpecified;
                    treecatItem.Relisted = ebayItem.Relisted;
                    treecatItem.RelistedSpecified = ebayItem.RelistedSpecified;
                    treecatItem.RelistLink = ebayItem.RelistLink;
                    treecatItem.RelistLinkSpecified = ebayItem.RelistLinkSpecified;
                    treecatItem.RelistParentID = ebayItem.RelistParentID;
                    treecatItem.RelistParentIDSpecified = ebayItem.RelistParentIDSpecified;

                    if (ebayItem.RegionID != null)
                    {
                        treecatItem.RegionID = ebayItem.RegionID;
                    }

                    if (ebayItem.ReservePrice != null)
                    {
                        treecatItem.ReservePrice = new AmountType();
                        treecatItem.ReservePrice.currencyID = ebayItem.ReservePrice.currencyID;
                        treecatItem.ReservePrice.Value = ebayItem.ReservePrice.Value;
                    }

                    if (ebayItem.ReturnPolicy != null)
                    {
                        treecatItem.ReturnPolicy = new ReturnPolicyType();

                        treecatItem.ReturnPolicy.Any = ebayItem.ReturnPolicy.Any;
                        treecatItem.ReturnPolicy.ExtendedHolidayReturns = ebayItem.ReturnPolicy.ExtendedHolidayReturns;
                        treecatItem.ReturnPolicy.ExtendedHolidayReturnsSpecified = ebayItem.ReturnPolicy.ExtendedHolidayReturnsSpecified;

                        if (ebayItem.ReturnPolicy.Description != null)
                        {
                            treecatItem.ReturnPolicy.Description = ebayItem.ReturnPolicy.Description;
                        }

                        if (ebayItem.ReturnPolicy.InternationalRefundOption != null)
                        {
                            treecatItem.ReturnPolicy.InternationalRefundOption = ebayItem.ReturnPolicy.InternationalRefundOption;
                        }

                        if (ebayItem.ReturnPolicy.InternationalReturnsAcceptedOption != null)
                        {
                            treecatItem.ReturnPolicy.InternationalReturnsAcceptedOption = ebayItem.ReturnPolicy.InternationalReturnsAcceptedOption;
                        }

                        if (ebayItem.ReturnPolicy.InternationalReturnsWithinOption != null)
                        {
                            treecatItem.ReturnPolicy.InternationalReturnsWithinOption = ebayItem.ReturnPolicy.InternationalReturnsWithinOption;
                        }

                        if (ebayItem.ReturnPolicy.InternationalShippingCostPaidByOption != null)
                        {
                            treecatItem.ReturnPolicy.InternationalShippingCostPaidByOption = ebayItem.ReturnPolicy.InternationalShippingCostPaidByOption;
                        }

                        if (ebayItem.ReturnPolicy.Refund != null)
                        {
                            treecatItem.ReturnPolicy.Refund = ebayItem.ReturnPolicy.Refund;
                        }

                        if (ebayItem.ReturnPolicy.RefundOption != null)
                        {
                            treecatItem.ReturnPolicy.RefundOption = ebayItem.ReturnPolicy.RefundOption;
                        }

                        if (ebayItem.ReturnPolicy.RestockingFeeValue != null)
                        {
                            treecatItem.ReturnPolicy.RestockingFeeValue = ebayItem.ReturnPolicy.RestockingFeeValue;
                        }

                        if (ebayItem.ReturnPolicy.RestockingFeeValueOption != null)
                        {
                            treecatItem.ReturnPolicy.RestockingFeeValueOption = ebayItem.ReturnPolicy.RestockingFeeValueOption;
                        }

                        if (ebayItem.ReturnPolicy.ReturnsAccepted != null)
                        {
                            treecatItem.ReturnPolicy.ReturnsAccepted = ebayItem.ReturnPolicy.ReturnsAccepted;
                        }

                        if (ebayItem.ReturnPolicy.ReturnsAcceptedOption != null)
                        {
                            treecatItem.ReturnPolicy.ReturnsAcceptedOption = ebayItem.ReturnPolicy.ReturnsAcceptedOption;
                        }

                        if (ebayItem.ReturnPolicy.ReturnsWithin != null)
                        {
                            treecatItem.ReturnPolicy.ReturnsWithin = ebayItem.ReturnPolicy.ReturnsWithin;
                        }

                        if (ebayItem.ReturnPolicy.ReturnsWithinOption != null)
                        {
                            treecatItem.ReturnPolicy.ReturnsWithinOption = ebayItem.ReturnPolicy.ReturnsWithinOption;
                        }

                        if (ebayItem.ReturnPolicy.ShippingCostPaidBy != null)
                        {
                            treecatItem.ReturnPolicy.ShippingCostPaidBy = ebayItem.ReturnPolicy.ShippingCostPaidBy;
                        }

                        if (ebayItem.ReturnPolicy.ShippingCostPaidByOption != null)
                        {
                            treecatItem.ReturnPolicy.ShippingCostPaidByOption = ebayItem.ReturnPolicy.ShippingCostPaidByOption;
                        }

                        if (ebayItem.ReturnPolicy.WarrantyDuration != null)
                        {
                            treecatItem.ReturnPolicy.WarrantyDuration = ebayItem.ReturnPolicy.WarrantyDuration;
                        }

                        if (ebayItem.ReturnPolicy.WarrantyDurationOption != null)
                        {
                            treecatItem.ReturnPolicy.WarrantyDurationOption = ebayItem.ReturnPolicy.WarrantyDurationOption;
                        }

                        if (ebayItem.ReturnPolicy.WarrantyOffered != null)
                        {
                            treecatItem.ReturnPolicy.WarrantyOffered = ebayItem.ReturnPolicy.WarrantyOffered;
                        }

                        if (ebayItem.ReturnPolicy.WarrantyOfferedOption != null)
                        {
                            treecatItem.ReturnPolicy.WarrantyOfferedOption = ebayItem.ReturnPolicy.WarrantyOfferedOption;
                        }

                        if (ebayItem.ReturnPolicy.WarrantyType != null)
                        {
                            treecatItem.ReturnPolicy.WarrantyType = ebayItem.ReturnPolicy.WarrantyType;
                        }

                        if (ebayItem.ReturnPolicy.WarrantyTypeOption != null)
                        {
                            treecatItem.ReturnPolicy.WarrantyTypeOption = ebayItem.ReturnPolicy.WarrantyTypeOption;
                        }
                    }

                    if (ebayItem.ReviseStatus != null)
                    {
                        treecatItem.ReviseStatus = new ReviseStatusType();
                        treecatItem.ReviseStatus.Any = ebayItem.ReviseStatus.Any;
                        treecatItem.ReviseStatus.BuyItNowAdded = ebayItem.ReviseStatus.BuyItNowAdded;
                        treecatItem.ReviseStatus.BuyItNowAddedSpecified = ebayItem.ReviseStatus.BuyItNowAddedSpecified;
                        treecatItem.ReviseStatus.BuyItNowLowered = ebayItem.ReviseStatus.BuyItNowLowered;
                        treecatItem.ReviseStatus.BuyItNowLoweredSpecified = ebayItem.ReviseStatus.BuyItNowLoweredSpecified;
                        treecatItem.ReviseStatus.ItemRevised = ebayItem.ReviseStatus.ItemRevised;
                        treecatItem.ReviseStatus.ReserveLowered = ebayItem.ReviseStatus.ReserveLowered;
                        treecatItem.ReviseStatus.ReserveLoweredSpecified = ebayItem.ReviseStatus.ReserveLoweredSpecified;
                        treecatItem.ReviseStatus.ReserveRemoved = ebayItem.ReviseStatus.ReserveRemoved;
                        treecatItem.ReviseStatus.ReserveRemovedSpecified = ebayItem.ReviseStatus.ReserveRemovedSpecified;
                    }

                    if (ebayItem.SearchDetails != null)
                    {
                        treecatItem.SearchDetails = new SearchDetailsType();
                        treecatItem.SearchDetails.Any = ebayItem.SearchDetails.Any;
                        treecatItem.SearchDetails.BuyItNowEnabled = ebayItem.SearchDetails.BuyItNowEnabled;
                        treecatItem.SearchDetails.BuyItNowEnabledSpecified = ebayItem.SearchDetails.BuyItNowEnabledSpecified;
                        treecatItem.SearchDetails.Picture = ebayItem.SearchDetails.Picture;
                        treecatItem.SearchDetails.PictureSpecified = ebayItem.SearchDetails.PictureSpecified;
                        treecatItem.SearchDetails.RecentListing = ebayItem.SearchDetails.RecentListing;
                        treecatItem.SearchDetails.RecentListingSpecified = ebayItem.SearchDetails.RecentListingSpecified;
                    }

                    if (ebayItem.SecondaryCategory != null)
                    {
                        treecatItem.SecondaryCategory = new CategoryType();
                        treecatItem.SecondaryCategory.Any = ebayItem.SecondaryCategory.Any;
                        treecatItem.SecondaryCategory.AutoPayEnabled = ebayItem.SecondaryCategory.AutoPayEnabled;
                        treecatItem.SecondaryCategory.AutoPayEnabledSpecified = ebayItem.SecondaryCategory.AutoPayEnabledSpecified;
                        treecatItem.SecondaryCategory.B2BVATEnabled = ebayItem.SecondaryCategory.B2BVATEnabled;
                        treecatItem.SecondaryCategory.B2BVATEnabledSpecified = ebayItem.SecondaryCategory.B2BVATEnabledSpecified;
                        treecatItem.SecondaryCategory.BestOfferEnabled = ebayItem.SecondaryCategory.BestOfferEnabled;
                        treecatItem.SecondaryCategory.BestOfferEnabledSpecified = ebayItem.SecondaryCategory.BestOfferEnabledSpecified;
                        treecatItem.SecondaryCategory.CatalogEnabled = ebayItem.SecondaryCategory.CatalogEnabled;
                        treecatItem.SecondaryCategory.CatalogEnabledSpecified = ebayItem.SecondaryCategory.CatalogEnabledSpecified;
                        treecatItem.SecondaryCategory.CategoryLevel = ebayItem.SecondaryCategory.CategoryLevel;
                        treecatItem.SecondaryCategory.CategoryLevelSpecified = ebayItem.SecondaryCategory.CategoryLevelSpecified;

                        if (ebayItem.SecondaryCategory.CategoryID != null)
                        {
                            treecatItem.SecondaryCategory.CategoryID = ebayItem.SecondaryCategory.CategoryID;
                        }

                        if (ebayItem.SecondaryCategory.CategoryName != null)
                        {
                            treecatItem.SecondaryCategory.CategoryName = ebayItem.SecondaryCategory.CategoryName;
                        }

                        if (ebayItem.SecondaryCategory.CategoryParentID != null)
                        {
                            treecatItem.SecondaryCategory.CategoryParentID = ebayItem.SecondaryCategory.CategoryParentID;
                        }

                        if (ebayItem.SecondaryCategory.CategoryParentName != null)
                        {
                            treecatItem.SecondaryCategory.CategoryParentName = ebayItem.SecondaryCategory.CategoryParentName;
                        }

                        if (ebayItem.SecondaryCategory.CharacteristicsSets != null)
                        {
                            treecatItem.SecondaryCategory.CharacteristicsSets = new CharacteristicsSetType[ebayItem.SecondaryCategory.CharacteristicsSets.Length];
                            for (int y = 0; ebayItem.SecondaryCategory.CharacteristicsSets.Length > y; y++)
                            {
                                treecatItem.SecondaryCategory.CharacteristicsSets[y] = new CharacteristicsSetType();
                                treecatItem.SecondaryCategory.CharacteristicsSets[y].Any = ebayItem.SecondaryCategory.CharacteristicsSets[y].Any;
                                treecatItem.SecondaryCategory.CharacteristicsSets[y].AttributeSetID = ebayItem.SecondaryCategory.CharacteristicsSets[y].AttributeSetID;
                                treecatItem.SecondaryCategory.CharacteristicsSets[y].AttributeSetIDSpecified = ebayItem.SecondaryCategory.CharacteristicsSets[y].AttributeSetIDSpecified;

                                if (ebayItem.SecondaryCategory.CharacteristicsSets[y].Name != null)
                                {
                                    treecatItem.SecondaryCategory.CharacteristicsSets[y].Name = ebayItem.SecondaryCategory.CharacteristicsSets[y].Name;
                                }

                                if (ebayItem.SecondaryCategory.CharacteristicsSets[y].AttributeSetVersion != null)
                                {
                                    treecatItem.SecondaryCategory.CharacteristicsSets[y].AttributeSetVersion = ebayItem.SecondaryCategory.CharacteristicsSets[y].AttributeSetVersion;
                                }

                                if (ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics != null)
                                {
                                    treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics = new CharacteristicType[ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics.Length];
                                    for (int x = 0; ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics.Length > x; x++)
                                    {
                                        treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x] = new CharacteristicType();
                                        treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].Any = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].Any;
                                        treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].AttributeID = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].AttributeID;

                                        if (ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].DateFormat != null)
                                        {
                                            treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].DateFormat = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].DateFormat;
                                        }

                                        if (ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].DisplaySequence != null)
                                        {
                                            treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].DisplaySequence = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].DisplaySequence;
                                        }

                                        if (ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].DisplayUOM != null)
                                        {
                                            treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].DisplayUOM = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].DisplayUOM;
                                        }

                                        if (ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].Label != null)
                                        {
                                            treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].Label = new LabelType();
                                            treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].Label.Any = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].Label.Any;
                                            treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].Label.visible = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].Label.visible;
                                            treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].Label.visibleSpecified = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].Label.visibleSpecified;

                                            if (ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].Label.Name != null)
                                            {
                                                treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].Label.Name = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].Label.Name;
                                            }
                                        }

                                        treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].SortOrder = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].SortOrder;
                                        treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].SortOrderSpecified = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].SortOrderSpecified;

                                        if (ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList != null)
                                        {
                                            treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList = new ValType[ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList.Length];
                                            for (int i = 0; ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList.Length > i; i++)
                                            {
                                                treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList[i] = new ValType();
                                                treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList[i].Any = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList[i].Any;
                                                treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList[i].ValueID = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList[i].ValueID;
                                                treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList[i].ValueIDSpecified = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList[i].ValueIDSpecified;

                                                if (ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList[i].SuggestedValueLiteral != null)
                                                {
                                                    treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList[i].SuggestedValueLiteral = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList[i].SuggestedValueLiteral;
                                                }

                                                if (ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList[i].ValueLiteral != null)
                                                {
                                                    treecatItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList[i].ValueLiteral = ebayItem.SecondaryCategory.CharacteristicsSets[y].Characteristics[x].ValueList[i].ValueLiteral;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (ebayItem.SecondaryCategory.Keywords != null)
                        {
                            treecatItem.SecondaryCategory.Keywords = ebayItem.SecondaryCategory.Keywords;
                        }

                        if (ebayItem.SecondaryCategory.ProductFinderIDs != null)
                        {
                            treecatItem.SecondaryCategory.ProductFinderIDs = new ExtendedProductFinderIDType[ebayItem.SecondaryCategory.ProductFinderIDs.Length];
                            for (int y = 0; ebayItem.SecondaryCategory.ProductFinderIDs.Length > y; y++)
                            {
                                treecatItem.SecondaryCategory.ProductFinderIDs[y] = new ExtendedProductFinderIDType();
                                treecatItem.SecondaryCategory.ProductFinderIDs[y].ProductFinderBuySide = ebayItem.SecondaryCategory.ProductFinderIDs[y].ProductFinderBuySide;
                                treecatItem.SecondaryCategory.ProductFinderIDs[y].ProductFinderBuySideSpecified = ebayItem.SecondaryCategory.ProductFinderIDs[y].ProductFinderBuySideSpecified;
                                treecatItem.SecondaryCategory.ProductFinderIDs[y].ProductFinderID = ebayItem.SecondaryCategory.ProductFinderIDs[y].ProductFinderID;
                                treecatItem.SecondaryCategory.ProductFinderIDs[y].ProductFinderIDSpecified = ebayItem.SecondaryCategory.ProductFinderIDs[y].ProductFinderIDSpecified;
                            }
                        }

                        treecatItem.SecondaryCategory.Expired = ebayItem.SecondaryCategory.Expired;
                        treecatItem.SecondaryCategory.ExpiredSpecified = ebayItem.SecondaryCategory.ExpiredSpecified;
                        treecatItem.SecondaryCategory.IntlAutosFixedCat = ebayItem.SecondaryCategory.IntlAutosFixedCat;
                        treecatItem.SecondaryCategory.IntlAutosFixedCatSpecified = ebayItem.SecondaryCategory.IntlAutosFixedCatSpecified;
                        treecatItem.SecondaryCategory.LeafCategory = ebayItem.SecondaryCategory.LeafCategory;
                        treecatItem.SecondaryCategory.LeafCategorySpecified = ebayItem.SecondaryCategory.LeafCategorySpecified;
                        treecatItem.SecondaryCategory.LSD = ebayItem.SecondaryCategory.LSD;
                        treecatItem.SecondaryCategory.LSDSpecified = ebayItem.SecondaryCategory.LSDSpecified;
                        treecatItem.SecondaryCategory.NumOfItems = ebayItem.SecondaryCategory.NumOfItems;
                        treecatItem.SecondaryCategory.NumOfItemsSpecified = ebayItem.SecondaryCategory.NumOfItemsSpecified;
                        treecatItem.SecondaryCategory.ORPA = ebayItem.SecondaryCategory.ORPA;
                        treecatItem.SecondaryCategory.ORPASpecified = ebayItem.SecondaryCategory.ORPASpecified;
                        treecatItem.SecondaryCategory.ORRA = ebayItem.SecondaryCategory.ORRA;
                        treecatItem.SecondaryCategory.ORRASpecified = ebayItem.SecondaryCategory.ORRASpecified;
                        treecatItem.SecondaryCategory.ProductSearchPageAvailable = ebayItem.SecondaryCategory.ProductSearchPageAvailable;
                        treecatItem.SecondaryCategory.ProductSearchPageAvailableSpecified = ebayItem.SecondaryCategory.ProductSearchPageAvailableSpecified;
                        treecatItem.SecondaryCategory.SellerGuaranteeEligible = ebayItem.SecondaryCategory.SellerGuaranteeEligible;
                        treecatItem.SecondaryCategory.SellerGuaranteeEligibleSpecified = ebayItem.SecondaryCategory.SellerGuaranteeEligibleSpecified;
                        treecatItem.SecondaryCategory.Virtual = ebayItem.SecondaryCategory.Virtual;
                        treecatItem.SecondaryCategory.VirtualSpecified = ebayItem.SecondaryCategory.VirtualSpecified;
                    }

                    if (ebayItem.Seller != null)
                    {
                        treecatItem.Seller = new UserType();
                        treecatItem.Seller.AboutMePage = ebayItem.Seller.AboutMePage;
                        treecatItem.Seller.AboutMePageSpecified = ebayItem.Seller.AboutMePageSpecified;
                        treecatItem.Seller.Any = ebayItem.Seller.Any;

                        if (ebayItem.Seller.BiddingSummary != null)
                        {
                            treecatItem.Seller.BiddingSummary = new BiddingSummaryType();
                            treecatItem.Seller.BiddingSummary.Any = ebayItem.Seller.BiddingSummary.Any;
                            treecatItem.Seller.BiddingSummary.BidActivityWithSeller = ebayItem.Seller.BiddingSummary.BidActivityWithSeller;
                            treecatItem.Seller.BiddingSummary.BidActivityWithSellerSpecified = ebayItem.Seller.BiddingSummary.BidActivityWithSellerSpecified;
                            treecatItem.Seller.BiddingSummary.BidRetractions = ebayItem.Seller.BiddingSummary.BidRetractions;
                            treecatItem.Seller.BiddingSummary.BidRetractionsSpecified = ebayItem.Seller.BiddingSummary.BidRetractionsSpecified;
                            treecatItem.Seller.BiddingSummary.BidsToUniqueCategories = ebayItem.Seller.BiddingSummary.BidsToUniqueCategories;
                            treecatItem.Seller.BiddingSummary.BidsToUniqueCategoriesSpecified = ebayItem.Seller.BiddingSummary.BidsToUniqueCategoriesSpecified;
                            treecatItem.Seller.BiddingSummary.BidsToUniqueSellers = ebayItem.Seller.BiddingSummary.BidsToUniqueSellers;
                            treecatItem.Seller.BiddingSummary.BidsToUniqueSellersSpecified = ebayItem.Seller.BiddingSummary.BidsToUniqueSellersSpecified;
                            treecatItem.Seller.BiddingSummary.SummaryDays = ebayItem.Seller.BiddingSummary.SummaryDays;
                            treecatItem.Seller.BiddingSummary.SummaryDaysSpecified = ebayItem.Seller.BiddingSummary.SummaryDaysSpecified;
                            treecatItem.Seller.BiddingSummary.TotalBids = ebayItem.Seller.BiddingSummary.TotalBids;
                            treecatItem.Seller.BiddingSummary.TotalBidsSpecified = ebayItem.Seller.BiddingSummary.TotalBidsSpecified;

                            if (ebayItem.Seller.BiddingSummary.ItemBidDetails != null)
                            {
                                treecatItem.Seller.BiddingSummary.ItemBidDetails = new ItemBidDetailsType[ebayItem.Seller.BiddingSummary.ItemBidDetails.Length];
                                for (int y = 0; ebayItem.Seller.BiddingSummary.ItemBidDetails.Length > y; y++)
                                {
                                    treecatItem.Seller.BiddingSummary.ItemBidDetails[y] = new ItemBidDetailsType();
                                    treecatItem.Seller.BiddingSummary.ItemBidDetails[y].Any = ebayItem.Seller.BiddingSummary.ItemBidDetails[y].Any;
                                    treecatItem.Seller.BiddingSummary.ItemBidDetails[y].BidCount = ebayItem.Seller.BiddingSummary.ItemBidDetails[y].BidCount;
                                    treecatItem.Seller.BiddingSummary.ItemBidDetails[y].BidCountSpecified = ebayItem.Seller.BiddingSummary.ItemBidDetails[y].BidCountSpecified;
                                    treecatItem.Seller.BiddingSummary.ItemBidDetails[y].LastBidTime = ebayItem.Seller.BiddingSummary.ItemBidDetails[y].LastBidTime;
                                    treecatItem.Seller.BiddingSummary.ItemBidDetails[y].LastBidTimeSpecified = ebayItem.Seller.BiddingSummary.ItemBidDetails[y].LastBidTimeSpecified;

                                    if (ebayItem.Seller.BiddingSummary.ItemBidDetails[y].CategoryID != null)
                                    {
                                        treecatItem.Seller.BiddingSummary.ItemBidDetails[y].CategoryID = ebayItem.Seller.BiddingSummary.ItemBidDetails[y].CategoryID;
                                    }

                                    if (ebayItem.Seller.BiddingSummary.ItemBidDetails[y].ItemID != null)
                                    {
                                        treecatItem.Seller.BiddingSummary.ItemBidDetails[y].ItemID = ebayItem.Seller.BiddingSummary.ItemBidDetails[y].ItemID;
                                    }

                                    if (ebayItem.Seller.BiddingSummary.ItemBidDetails[y].SellerID != null)
                                    {
                                        treecatItem.Seller.BiddingSummary.ItemBidDetails[y].SellerID = ebayItem.Seller.BiddingSummary.ItemBidDetails[y].SellerID;
                                    }
                                }
                            }
                        }
                    }

                    if (ebayItem.SellerContactDetails != null)
                    {
                        treecatItem.SellerContactDetails = new AddressType();

                        if (ebayItem.SellerContactDetails.AddressAttribute != null)
                        {
                            treecatItem.SellerContactDetails.AddressAttribute = new AddressAttributeType[ebayItem.SellerContactDetails.AddressAttribute.Length];
                            for (int y = 0; ebayItem.SellerContactDetails.AddressAttribute.Length > y; y++)
                            {
                                treecatItem.SellerContactDetails.AddressAttribute[y] = new AddressAttributeType();
                                treecatItem.SellerContactDetails.AddressAttribute[y].type = ebayItem.SellerContactDetails.AddressAttribute[y].type;
                                treecatItem.SellerContactDetails.AddressAttribute[y].typeSpecified = ebayItem.SellerContactDetails.AddressAttribute[y].typeSpecified;
                                treecatItem.SellerContactDetails.AddressAttribute[y].Value = ebayItem.SellerContactDetails.AddressAttribute[y].Value;
                            }
                        }

                        if (ebayItem.SellerContactDetails.AddressID != null)
                        {
                            treecatItem.SellerContactDetails.AddressID = ebayItem.SellerContactDetails.AddressID;
                        }

                        treecatItem.SellerContactDetails.AddressOwner = ebayItem.SellerContactDetails.AddressOwner;
                        treecatItem.SellerContactDetails.AddressOwnerSpecified = ebayItem.SellerContactDetails.AddressOwnerSpecified;
                        treecatItem.SellerContactDetails.AddressRecordType = ebayItem.SellerContactDetails.AddressRecordType;
                        treecatItem.SellerContactDetails.AddressRecordTypeSpecified = ebayItem.SellerContactDetails.AddressRecordTypeSpecified;
                        treecatItem.SellerContactDetails.AddressStatus = ebayItem.SellerContactDetails.AddressStatus;
                        treecatItem.SellerContactDetails.AddressStatusSpecified = ebayItem.SellerContactDetails.AddressStatusSpecified;
                        treecatItem.SellerContactDetails.AddressUsage = ebayItem.SellerContactDetails.AddressUsage;
                        treecatItem.SellerContactDetails.AddressUsageSpecified = ebayItem.SellerContactDetails.AddressUsageSpecified;
                        treecatItem.SellerContactDetails.Any = ebayItem.SellerContactDetails.Any;

                        if (ebayItem.SellerContactDetails.CityName != null)
                        {
                            treecatItem.SellerContactDetails.CityName = ebayItem.SellerContactDetails.CityName;
                        }

                        if (ebayItem.SellerContactDetails.CompanyName != null)
                        {
                            treecatItem.SellerContactDetails.CompanyName = ebayItem.SellerContactDetails.CompanyName;
                        }

                        if (ebayItem.SellerContactDetails.CountryName != null)
                        {
                            treecatItem.SellerContactDetails.CountryName = ebayItem.SellerContactDetails.CountryName;
                        }

                        if (ebayItem.SellerContactDetails.County != null)
                        {
                            treecatItem.SellerContactDetails.County = ebayItem.SellerContactDetails.County;
                        }

                        if (ebayItem.SellerContactDetails.ExternalAddressID != null)
                        {
                            treecatItem.SellerContactDetails.ExternalAddressID = ebayItem.SellerContactDetails.ExternalAddressID;
                        }

                        if (ebayItem.SellerContactDetails.FirstName != null)
                        {
                            treecatItem.SellerContactDetails.FirstName = ebayItem.SellerContactDetails.FirstName;
                        }

                        if (ebayItem.SellerContactDetails.InternationalName != null)
                        {
                            treecatItem.SellerContactDetails.InternationalName = ebayItem.SellerContactDetails.InternationalName;
                        }

                        if (ebayItem.SellerContactDetails.InternationalStateAndCity != null)
                        {
                            treecatItem.SellerContactDetails.InternationalStateAndCity = ebayItem.SellerContactDetails.InternationalStateAndCity;
                        }

                        if (ebayItem.SellerContactDetails.InternationalStreet != null)
                        {
                            treecatItem.SellerContactDetails.InternationalStreet = ebayItem.SellerContactDetails.InternationalStreet;
                        }

                        if (ebayItem.SellerContactDetails.LastName != null)
                        {
                            treecatItem.SellerContactDetails.LastName = ebayItem.SellerContactDetails.LastName;
                        }

                        if (ebayItem.SellerContactDetails.Name != null)
                        {
                            treecatItem.SellerContactDetails.Name = ebayItem.SellerContactDetails.Name;
                        }

                        if (ebayItem.SellerContactDetails.Phone != null)
                        {
                            treecatItem.SellerContactDetails.Phone = ebayItem.SellerContactDetails.Phone;
                        }

                        if (ebayItem.SellerContactDetails.Phone2 != null)
                        {
                            treecatItem.SellerContactDetails.Phone2 = ebayItem.SellerContactDetails.Phone2;
                        }

                        if (ebayItem.SellerContactDetails.PhoneAreaOrCityCode != null)
                        {
                            treecatItem.SellerContactDetails.PhoneAreaOrCityCode = ebayItem.SellerContactDetails.PhoneAreaOrCityCode;
                        }

                        if (ebayItem.SellerContactDetails.PhoneCountryPrefix != null)
                        {
                            treecatItem.SellerContactDetails.PhoneCountryPrefix = ebayItem.SellerContactDetails.PhoneCountryPrefix;
                        }

                        if (ebayItem.SellerContactDetails.PhoneLocalNumber != null)
                        {
                            treecatItem.SellerContactDetails.PhoneLocalNumber = ebayItem.SellerContactDetails.PhoneLocalNumber;
                        }

                        if (ebayItem.SellerContactDetails.PostalCode != null)
                        {
                            treecatItem.SellerContactDetails.PostalCode = ebayItem.SellerContactDetails.PostalCode;
                        }

                        if (ebayItem.SellerContactDetails.ReferenceID != null)
                        {
                            treecatItem.SellerContactDetails.ReferenceID = ebayItem.SellerContactDetails.ReferenceID;
                        }

                        if (ebayItem.SellerContactDetails.StateOrProvince != null)
                        {
                            treecatItem.SellerContactDetails.StateOrProvince = ebayItem.SellerContactDetails.StateOrProvince;
                        }

                        if (ebayItem.SellerContactDetails.Street != null)
                        {
                            treecatItem.SellerContactDetails.Street = ebayItem.SellerContactDetails.Street;
                        }

                        if (ebayItem.SellerContactDetails.Street1 != null)
                        {
                            treecatItem.SellerContactDetails.Street1 = ebayItem.SellerContactDetails.Street1;
                        }

                        if (ebayItem.SellerContactDetails.Street2 != null)
                        {
                            treecatItem.SellerContactDetails.Street2 = ebayItem.SellerContactDetails.Street2;
                        }
                    }

                    if (ebayItem.SellerProfiles != null)
                    {
                        treecatItem.SellerProfiles = new SellerProfilesType();
                        treecatItem.SellerProfiles.Any = ebayItem.SellerProfiles.Any;

                        if (ebayItem.SellerProfiles.SellerPaymentProfile != null)
                        {
                            treecatItem.SellerProfiles.SellerPaymentProfile = new SellerPaymentProfileType();

                            treecatItem.SellerProfiles.SellerPaymentProfile.Any = ebayItem.SellerProfiles.SellerPaymentProfile.Any;
                            treecatItem.SellerProfiles.SellerPaymentProfile.PaymentProfileID = ebayItem.SellerProfiles.SellerPaymentProfile.PaymentProfileID;
                            treecatItem.SellerProfiles.SellerPaymentProfile.PaymentProfileIDSpecified = ebayItem.SellerProfiles.SellerPaymentProfile.PaymentProfileIDSpecified;

                            if (ebayItem.SellerProfiles.SellerPaymentProfile.PaymentProfileName != null)
                            {
                                treecatItem.SellerProfiles.SellerPaymentProfile.PaymentProfileName = ebayItem.SellerProfiles.SellerPaymentProfile.PaymentProfileName;
                            }
                        }

                        if (ebayItem.SellerProfiles.SellerReturnProfile != null)
                        {
                            treecatItem.SellerProfiles.SellerReturnProfile = new SellerReturnProfileType();
                            treecatItem.SellerProfiles.SellerReturnProfile.Any = ebayItem.SellerProfiles.SellerReturnProfile.Any;
                            treecatItem.SellerProfiles.SellerReturnProfile.ReturnProfileID = ebayItem.SellerProfiles.SellerReturnProfile.ReturnProfileID;
                            treecatItem.SellerProfiles.SellerReturnProfile.ReturnProfileIDSpecified = ebayItem.SellerProfiles.SellerReturnProfile.ReturnProfileIDSpecified;

                            if (ebayItem.SellerProfiles.SellerReturnProfile.ReturnProfileName != null)
                            {
                                treecatItem.SellerProfiles.SellerReturnProfile.ReturnProfileName = ebayItem.SellerProfiles.SellerReturnProfile.ReturnProfileName;
                            }
                        }

                        if (ebayItem.SellerProfiles.SellerShippingProfile != null)
                        {
                            treecatItem.SellerProfiles.SellerShippingProfile = new SellerShippingProfileType();
                            treecatItem.SellerProfiles.SellerShippingProfile.Any = ebayItem.SellerProfiles.SellerShippingProfile.Any;
                            treecatItem.SellerProfiles.SellerShippingProfile.ShippingProfileID = ebayItem.SellerProfiles.SellerShippingProfile.ShippingProfileID;
                            treecatItem.SellerProfiles.SellerShippingProfile.ShippingProfileIDSpecified = ebayItem.SellerProfiles.SellerShippingProfile.ShippingProfileIDSpecified;

                            if (ebayItem.SellerProfiles.SellerShippingProfile.ShippingProfileName != null)
                            {
                                treecatItem.SellerProfiles.SellerShippingProfile.ShippingProfileName = ebayItem.SellerProfiles.SellerShippingProfile.ShippingProfileName;
                            }
                        }
                    }

                    if (ebayItem.SellerProvidedTitle != null)
                    {
                        treecatItem.SellerProvidedTitle = ebayItem.SellerProvidedTitle;
                    }

                    if (ebayItem.SellerVacationNote != null)
                    {
                        treecatItem.SellerVacationNote = ebayItem.SellerVacationNote;
                    }

                    if (ebayItem.SellingStatus != null)
                    {
                        treecatItem.SellingStatus = new SellingStatusType();
                        treecatItem.SellingStatus.AdminEnded = ebayItem.SellingStatus.AdminEnded;
                        treecatItem.SellingStatus.AdminEndedSpecified = ebayItem.SellingStatus.AdminEndedSpecified;
                        treecatItem.SellingStatus.Any = ebayItem.SellingStatus.Any;
                        treecatItem.SellingStatus.BidCount = ebayItem.SellingStatus.BidCount;
                        treecatItem.SellingStatus.BidCountSpecified = ebayItem.SellingStatus.BidCountSpecified;
                        treecatItem.SellingStatus.BidderCount = ebayItem.SellingStatus.BidderCount;
                        treecatItem.SellingStatus.BidderCountSpecified = ebayItem.SellingStatus.BidderCountSpecified;

                        if (ebayItem.SellingStatus.BidIncrement != null)
                        {
                            treecatItem.SellingStatus.BidIncrement = new AmountType();
                            treecatItem.SellingStatus.BidIncrement.currencyID = ebayItem.SellingStatus.BidIncrement.currencyID;
                            treecatItem.SellingStatus.BidIncrement.Value = ebayItem.SellingStatus.BidIncrement.Value;
                        }

                        if (ebayItem.SellingStatus.ConvertedCurrentPrice != null)
                        {
                            treecatItem.SellingStatus.ConvertedCurrentPrice = new AmountType();
                            treecatItem.SellingStatus.ConvertedCurrentPrice.currencyID = ebayItem.SellingStatus.ConvertedCurrentPrice.currencyID;
                            treecatItem.SellingStatus.ConvertedCurrentPrice.Value = ebayItem.SellingStatus.ConvertedCurrentPrice.Value;
                        }

                        if (ebayItem.SellingStatus.CurrentPrice != null)
                        {
                            treecatItem.SellingStatus.CurrentPrice = new AmountType();
                            treecatItem.SellingStatus.CurrentPrice.currencyID = ebayItem.SellingStatus.CurrentPrice.currencyID;
                            treecatItem.SellingStatus.CurrentPrice.Value = ebayItem.SellingStatus.CurrentPrice.Value;
                        }

                        if (ebayItem.SellingStatus.FinalValueFee != null)
                        {
                            treecatItem.SellingStatus.FinalValueFee = new AmountType();
                            treecatItem.SellingStatus.FinalValueFee.currencyID = ebayItem.SellingStatus.FinalValueFee.currencyID;
                            treecatItem.SellingStatus.FinalValueFee.Value = ebayItem.SellingStatus.FinalValueFee.Value;
                        }

                        if (ebayItem.SellingStatus.HighBidder != null)
                        {
                            treecatItem.SellingStatus.HighBidder = new UserType();
                            treecatItem.SellingStatus.HighBidder.AboutMePage = ebayItem.SellingStatus.HighBidder.AboutMePage;
                            treecatItem.SellingStatus.HighBidder.AboutMePageSpecified = ebayItem.SellingStatus.HighBidder.AboutMePageSpecified;
                            treecatItem.SellingStatus.HighBidder.Any = ebayItem.SellingStatus.HighBidder.Any;

                            if (ebayItem.SellingStatus.HighBidder.BiddingSummary != null)
                            {
                                treecatItem.SellingStatus.HighBidder.BiddingSummary = new BiddingSummaryType();
                                treecatItem.SellingStatus.HighBidder.BiddingSummary.Any = ebayItem.SellingStatus.HighBidder.BiddingSummary.Any;
                                treecatItem.SellingStatus.HighBidder.BiddingSummary.BidActivityWithSeller = ebayItem.SellingStatus.HighBidder.BiddingSummary.BidActivityWithSeller;
                                treecatItem.SellingStatus.HighBidder.BiddingSummary.BidActivityWithSellerSpecified = ebayItem.SellingStatus.HighBidder.BiddingSummary.BidActivityWithSellerSpecified;
                                treecatItem.SellingStatus.HighBidder.BiddingSummary.BidRetractions = ebayItem.SellingStatus.HighBidder.BiddingSummary.BidRetractions;
                                treecatItem.SellingStatus.HighBidder.BiddingSummary.BidRetractionsSpecified = ebayItem.SellingStatus.HighBidder.BiddingSummary.BidRetractionsSpecified;
                                treecatItem.SellingStatus.HighBidder.BiddingSummary.BidsToUniqueCategories = ebayItem.SellingStatus.HighBidder.BiddingSummary.BidsToUniqueCategories;
                                treecatItem.SellingStatus.HighBidder.BiddingSummary.BidsToUniqueCategoriesSpecified = ebayItem.SellingStatus.HighBidder.BiddingSummary.BidsToUniqueCategoriesSpecified;
                                treecatItem.SellingStatus.HighBidder.BiddingSummary.BidsToUniqueSellers = ebayItem.SellingStatus.HighBidder.BiddingSummary.BidsToUniqueSellers;
                                treecatItem.SellingStatus.HighBidder.BiddingSummary.BidsToUniqueSellersSpecified = ebayItem.SellingStatus.HighBidder.BiddingSummary.BidsToUniqueSellersSpecified;
                                treecatItem.SellingStatus.HighBidder.BiddingSummary.SummaryDays = ebayItem.SellingStatus.HighBidder.BiddingSummary.SummaryDays;
                                treecatItem.SellingStatus.HighBidder.BiddingSummary.SummaryDaysSpecified = ebayItem.SellingStatus.HighBidder.BiddingSummary.SummaryDaysSpecified;
                                treecatItem.SellingStatus.HighBidder.BiddingSummary.TotalBids = ebayItem.SellingStatus.HighBidder.BiddingSummary.TotalBids;
                                treecatItem.SellingStatus.HighBidder.BiddingSummary.TotalBidsSpecified = ebayItem.SellingStatus.HighBidder.BiddingSummary.TotalBidsSpecified;

                                if (ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails = new ItemBidDetailsType[ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails.Length];
                                    for (int y = 0; ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails.Length > y; y++)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y] = new ItemBidDetailsType();
                                        treecatItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].Any = ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].Any;
                                        treecatItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].BidCount = ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].BidCount;
                                        treecatItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].BidCountSpecified = ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].BidCountSpecified;
                                        treecatItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].LastBidTime = ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].LastBidTime;
                                        treecatItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].LastBidTimeSpecified = ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].LastBidTimeSpecified;

                                        if (ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].CategoryID != null)
                                        {
                                            treecatItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].CategoryID = ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].CategoryID;
                                        }

                                        if (ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].ItemID != null)
                                        {
                                            treecatItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].ItemID = ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].ItemID;
                                        }

                                        if (ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].SellerID != null)
                                        {
                                            treecatItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].SellerID = ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[y].SellerID;
                                        }
                                    }
                                }
                            }

                            if (ebayItem.SellingStatus.HighBidder.BillingEmail != null)
                            {
                                treecatItem.SellingStatus.HighBidder.BillingEmail = ebayItem.SellingStatus.HighBidder.BillingEmail;
                            }

                            treecatItem.SellingStatus.HighBidder.BusinessRole = ebayItem.SellingStatus.HighBidder.BusinessRole;
                            treecatItem.SellingStatus.HighBidder.BusinessRoleSpecified = ebayItem.SellingStatus.HighBidder.BusinessRoleSpecified;

                            if (ebayItem.SellingStatus.HighBidder.BuyerInfo != null)
                            {
                                treecatItem.SellingStatus.HighBidder.BuyerInfo = new BuyerType();

                                treecatItem.SellingStatus.HighBidder.BuyerInfo.Any = ebayItem.SellingStatus.HighBidder.BuyerInfo.Any;

                                if (ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier = new TaxIdentifierType[ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier.Length];
                                    for (int y = 0; ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier.Length > y; y++)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y] = new TaxIdentifierType();
                                        if (ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].ID != null)
                                        {
                                            treecatItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].ID = ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].ID;
                                        }

                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Any = ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Any;
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Type = ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Type;
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].TypeSpecified = ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].TypeSpecified;

                                        if (ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Attribute != null)
                                        {
                                            treecatItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Attribute = new TaxIdentifierAttributeType[ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Attribute.Length];
                                            for (int x = 0; ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Attribute.Length > x; x++)
                                            {
                                                treecatItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Attribute[x] = new TaxIdentifierAttributeType();
                                                treecatItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Attribute[x].name = ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Attribute[x].name;
                                                treecatItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Attribute[x].nameSpecified = ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Attribute[x].nameSpecified;

                                                if (ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Attribute[x].Value != null)
                                                {
                                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Attribute[x].Value = ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[y].Attribute[x].Value;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress = new AddressType();
                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute = new AddressAttributeType[ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute.Length];
                                        for (int y = 0; ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute.Length > y; y++)
                                        {
                                            treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[y] = new AddressAttributeType();
                                            treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[y].type = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[y].type;
                                            treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[y].typeSpecified = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[y].typeSpecified;

                                            if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[y].Value != null)
                                            {
                                                treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[y].Value = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[y].Value;
                                            }
                                        }
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressID != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressID = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressID;
                                    }

                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressOwner = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressOwner;
                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressOwnerSpecified = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressOwnerSpecified;
                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressRecordType = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressRecordType;
                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressRecordTypeSpecified = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressRecordTypeSpecified;
                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressStatus = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressStatus;
                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressStatusSpecified = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressStatusSpecified;
                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressUsage = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressUsage;
                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressUsageSpecified = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressUsageSpecified;
                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Any = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Any;

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CityName != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CityName = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CityName;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CompanyName != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CompanyName = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CompanyName;
                                    }

                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Country = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Country;

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CountryName != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CountryName = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CountryName;
                                    }

                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CountrySpecified = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CountrySpecified;

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.County != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.County = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.County;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.ExternalAddressID != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.ExternalAddressID = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.ExternalAddressID;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.FirstName != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.FirstName = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.FirstName;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalName != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalName = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalName;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalStateAndCity != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalStateAndCity = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalStateAndCity;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalStreet != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalStreet = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalStreet;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.LastName != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.LastName = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.LastName;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Name != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Name = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Name;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Phone != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Phone = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Phone;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Phone2 != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Phone2 = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Phone2;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneAreaOrCityCode != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneAreaOrCityCode = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneAreaOrCityCode;
                                    }

                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneCountryCode = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneCountryCode;
                                    treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneCountryCodeSpecified = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneCountryCodeSpecified;

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneCountryPrefix != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneCountryPrefix = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneCountryPrefix;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneLocalNumber != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneLocalNumber = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneLocalNumber;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PostalCode != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PostalCode = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PostalCode;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.ReferenceID != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.ReferenceID = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.ReferenceID;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.StateOrProvince != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.StateOrProvince = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.StateOrProvince;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street1 != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street1 = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street1;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street2 != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street2 = ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street2;
                                    }
                                }
                            }

                            if (ebayItem.SellingStatus.HighBidder.CharityAffiliations.CharityID != null)
                            {

                                for (int y = 0; ebayItem.SellingStatus.HighBidder.CharityAffiliations.CharityID.Length > y; y++)
                                {
                                    treecatItem.SellingStatus.HighBidder.CharityAffiliations.CharityID[y] = new CharityIDType();
                                    treecatItem.SellingStatus.HighBidder.CharityAffiliations.CharityID[y].type = ebayItem.SellingStatus.HighBidder.CharityAffiliations.CharityID[y].type;
                                    treecatItem.SellingStatus.HighBidder.CharityAffiliations.CharityID[y].Value = ebayItem.SellingStatus.HighBidder.CharityAffiliations.CharityID[y].Value;
                                }
                                treecatItem.SellingStatus.HighBidder.CharityAffiliations.Any = ebayItem.SellingStatus.HighBidder.CharityAffiliations.Any;
                            }

                            treecatItem.SellingStatus.HighBidder.eBayGoodStandingSpecified = ebayItem.SellingStatus.HighBidder.eBayGoodStandingSpecified;
                            treecatItem.SellingStatus.HighBidder.eBayWikiReadOnly = ebayItem.SellingStatus.HighBidder.eBayWikiReadOnly;
                            treecatItem.SellingStatus.HighBidder.eBayWikiReadOnlySpecified = ebayItem.SellingStatus.HighBidder.eBayWikiReadOnlySpecified;
                            treecatItem.SellingStatus.HighBidder.EnterpriseSeller = ebayItem.SellingStatus.HighBidder.EnterpriseSeller;
                            treecatItem.SellingStatus.HighBidder.EnterpriseSellerSpecified = ebayItem.SellingStatus.HighBidder.EnterpriseSellerSpecified;
                            treecatItem.SellingStatus.HighBidder.FeedbackPrivate = ebayItem.SellingStatus.HighBidder.FeedbackPrivate;
                            treecatItem.SellingStatus.HighBidder.FeedbackPrivateSpecified = ebayItem.SellingStatus.HighBidder.FeedbackPrivateSpecified;
                            treecatItem.SellingStatus.HighBidder.FeedbackRatingStar = ebayItem.SellingStatus.HighBidder.FeedbackRatingStar;
                            treecatItem.SellingStatus.HighBidder.FeedbackRatingStarSpecified = ebayItem.SellingStatus.HighBidder.FeedbackRatingStarSpecified;
                            treecatItem.SellingStatus.HighBidder.FeedbackScore = ebayItem.SellingStatus.HighBidder.FeedbackScore;
                            treecatItem.SellingStatus.HighBidder.FeedbackScoreSpecified = ebayItem.SellingStatus.HighBidder.FeedbackScoreSpecified;
                            treecatItem.SellingStatus.HighBidder.IDVerified = ebayItem.SellingStatus.HighBidder.IDVerified;
                            treecatItem.SellingStatus.HighBidder.IDVerifiedSpecified = ebayItem.SellingStatus.HighBidder.IDVerifiedSpecified;

                            if (ebayItem.SellingStatus.HighBidder.Email != null)
                            {
                                treecatItem.SellingStatus.HighBidder.Email = ebayItem.SellingStatus.HighBidder.Email;
                            }

                            if (ebayItem.SellingStatus.HighBidder.EIASToken != null)
                            {
                                treecatItem.SellingStatus.HighBidder.EIASToken = ebayItem.SellingStatus.HighBidder.EIASToken;
                            }

                            if (ebayItem.SellingStatus.HighBidder.Membership != null)
                            {
                                treecatItem.SellingStatus.HighBidder.Membership = new MembershipDetailType[ebayItem.SellingStatus.HighBidder.Membership.Length];
                                for (int y = 0; ebayItem.SellingStatus.HighBidder.Membership.Length > y; y++)
                                {
                                    treecatItem.SellingStatus.HighBidder.Membership[y] = new MembershipDetailType();
                                    treecatItem.SellingStatus.HighBidder.Membership[y].Any = ebayItem.SellingStatus.HighBidder.Membership[y].Any;
                                    treecatItem.SellingStatus.HighBidder.Membership[y].ExpiryDate = ebayItem.SellingStatus.HighBidder.Membership[y].ExpiryDate;
                                    treecatItem.SellingStatus.HighBidder.Membership[y].ExpiryDateSpecified = ebayItem.SellingStatus.HighBidder.Membership[y].ExpiryDateSpecified;
                                    treecatItem.SellingStatus.HighBidder.Membership[y].ProgramName = ebayItem.SellingStatus.HighBidder.Membership[y].ProgramName;
                                    treecatItem.SellingStatus.HighBidder.Membership[y].Site = ebayItem.SellingStatus.HighBidder.Membership[y].Site;
                                    treecatItem.SellingStatus.HighBidder.Membership[y].SiteSpecified = ebayItem.SellingStatus.HighBidder.Membership[y].SiteSpecified;
                                }
                            }

                            treecatItem.SellingStatus.HighBidder.NewUser = ebayItem.SellingStatus.HighBidder.NewUser;
                            treecatItem.SellingStatus.HighBidder.NewUserSpecified = ebayItem.SellingStatus.HighBidder.NewUserSpecified;
                            treecatItem.SellingStatus.HighBidder.PayPalAccountLevel = ebayItem.SellingStatus.HighBidder.PayPalAccountLevel;
                            treecatItem.SellingStatus.HighBidder.PayPalAccountLevelSpecified = ebayItem.SellingStatus.HighBidder.PayPalAccountLevelSpecified;
                            treecatItem.SellingStatus.HighBidder.PayPalAccountStatus = ebayItem.SellingStatus.HighBidder.PayPalAccountStatus;
                            treecatItem.SellingStatus.HighBidder.PayPalAccountStatusSpecified = ebayItem.SellingStatus.HighBidder.PayPalAccountStatusSpecified;
                            treecatItem.SellingStatus.HighBidder.PayPalAccountType = ebayItem.SellingStatus.HighBidder.PayPalAccountType;
                            treecatItem.SellingStatus.HighBidder.PayPalAccountTypeSpecified = ebayItem.SellingStatus.HighBidder.PayPalAccountTypeSpecified;
                            treecatItem.SellingStatus.HighBidder.PositiveFeedbackPercent = ebayItem.SellingStatus.HighBidder.PositiveFeedbackPercent;
                            treecatItem.SellingStatus.HighBidder.PositiveFeedbackPercentSpecified = ebayItem.SellingStatus.HighBidder.PositiveFeedbackPercentSpecified;
                            treecatItem.SellingStatus.HighBidder.QualifiesForSelling = ebayItem.SellingStatus.HighBidder.QualifiesForSelling;
                            treecatItem.SellingStatus.HighBidder.QualifiesForSellingSpecified = ebayItem.SellingStatus.HighBidder.QualifiesForSellingSpecified;

                            if (ebayItem.SellingStatus.HighBidder.RegistrationAddress != null)
                            {
                                treecatItem.SellingStatus.HighBidder.RegistrationAddress = new AddressType();
                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressAttribute != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.AddressAttribute = new AddressAttributeType[ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressAttribute.Length];
                                    for (int y = 0; ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressAttribute.Length > y; y++)
                                    {
                                        treecatItem.SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[y] = new AddressAttributeType();
                                        treecatItem.SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[y].type = ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[y].type;
                                        treecatItem.SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[y].typeSpecified = ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[y].typeSpecified;
                                        treecatItem.SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[y].Value = ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[y].Value;
                                    }
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressID != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.AddressID = ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressID;
                                }

                                treecatItem.SellingStatus.HighBidder.RegistrationAddress.AddressOwner = ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressOwner;
                                treecatItem.SellingStatus.HighBidder.RegistrationAddress.AddressOwnerSpecified = ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressOwnerSpecified;
                                treecatItem.SellingStatus.HighBidder.RegistrationAddress.AddressRecordType = ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressRecordType;
                                treecatItem.SellingStatus.HighBidder.RegistrationAddress.AddressRecordTypeSpecified = ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressRecordTypeSpecified;
                                treecatItem.SellingStatus.HighBidder.RegistrationAddress.AddressStatus = ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressStatus;
                                treecatItem.SellingStatus.HighBidder.RegistrationAddress.AddressStatusSpecified = ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressStatusSpecified;
                                treecatItem.SellingStatus.HighBidder.RegistrationAddress.AddressUsage = ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressUsage;
                                treecatItem.SellingStatus.HighBidder.RegistrationAddress.AddressUsageSpecified = ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressUsageSpecified;
                                treecatItem.SellingStatus.HighBidder.RegistrationAddress.Any = ebayItem.SellingStatus.HighBidder.RegistrationAddress.Any;

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.CityName != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.CityName = ebayItem.SellingStatus.HighBidder.RegistrationAddress.CityName;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.CompanyName != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.CompanyName = ebayItem.SellingStatus.HighBidder.RegistrationAddress.CompanyName;
                                }

                                treecatItem.SellingStatus.HighBidder.RegistrationAddress.Country = ebayItem.SellingStatus.HighBidder.RegistrationAddress.Country;

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.CountryName != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.CountryName = ebayItem.SellingStatus.HighBidder.RegistrationAddress.CountryName;
                                }

                                treecatItem.SellingStatus.HighBidder.RegistrationAddress.CountrySpecified = ebayItem.SellingStatus.HighBidder.RegistrationAddress.CountrySpecified;

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.County != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.County = ebayItem.SellingStatus.HighBidder.RegistrationAddress.County;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.ExternalAddressID != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.ExternalAddressID = ebayItem.SellingStatus.HighBidder.RegistrationAddress.ExternalAddressID;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.FirstName != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.FirstName = ebayItem.SellingStatus.HighBidder.RegistrationAddress.FirstName;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.InternationalName != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.InternationalName = ebayItem.SellingStatus.HighBidder.RegistrationAddress.InternationalName;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.InternationalStateAndCity != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.InternationalStateAndCity = ebayItem.SellingStatus.HighBidder.RegistrationAddress.InternationalStateAndCity;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.InternationalStreet != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.InternationalStreet = ebayItem.SellingStatus.HighBidder.RegistrationAddress.InternationalStreet;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.LastName != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.LastName = ebayItem.SellingStatus.HighBidder.RegistrationAddress.LastName;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.Name != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.Name = ebayItem.SellingStatus.HighBidder.RegistrationAddress.Name;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.Phone != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.Phone = ebayItem.SellingStatus.HighBidder.RegistrationAddress.Phone;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.Phone2 != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.Phone2 = ebayItem.SellingStatus.HighBidder.RegistrationAddress.Phone2;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.PhoneAreaOrCityCode != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.PhoneAreaOrCityCode = ebayItem.SellingStatus.HighBidder.RegistrationAddress.PhoneAreaOrCityCode;
                                }

                                treecatItem.SellingStatus.HighBidder.RegistrationAddress.PhoneCountryCode = ebayItem.SellingStatus.HighBidder.RegistrationAddress.PhoneCountryCode;
                                treecatItem.SellingStatus.HighBidder.RegistrationAddress.PhoneCountryCodeSpecified = ebayItem.SellingStatus.HighBidder.RegistrationAddress.PhoneCountryCodeSpecified;

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.PhoneCountryPrefix != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.PhoneCountryPrefix = ebayItem.SellingStatus.HighBidder.RegistrationAddress.PhoneCountryPrefix;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.PhoneLocalNumber != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.PhoneLocalNumber = ebayItem.SellingStatus.HighBidder.RegistrationAddress.PhoneLocalNumber;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.PostalCode != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.PostalCode = ebayItem.SellingStatus.HighBidder.RegistrationAddress.PostalCode;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.ReferenceID != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.ReferenceID = ebayItem.SellingStatus.HighBidder.RegistrationAddress.ReferenceID;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.StateOrProvince != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.StateOrProvince = ebayItem.SellingStatus.HighBidder.RegistrationAddress.StateOrProvince;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.Street != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.Street = ebayItem.SellingStatus.HighBidder.RegistrationAddress.Street;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.Street1 != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.Street1 = ebayItem.SellingStatus.HighBidder.RegistrationAddress.Street1;
                                }

                                if (ebayItem.SellingStatus.HighBidder.RegistrationAddress.Street2 != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.RegistrationAddress.Street2 = ebayItem.SellingStatus.HighBidder.RegistrationAddress.Street2;
                                }
                            }

                            treecatItem.SellingStatus.HighBidder.RegistrationDate = ebayItem.SellingStatus.HighBidder.RegistrationDate;

                            if (ebayItem.SellingStatus.HighBidder.SellerInfo != null)
                            {
                                treecatItem.SellingStatus.HighBidder.SellerInfo = new SellerType();
                                treecatItem.SellingStatus.HighBidder.SellerInfo.AllowPaymentEdit = ebayItem.SellingStatus.HighBidder.SellerInfo.AllowPaymentEdit;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.Any = ebayItem.SellingStatus.HighBidder.SellerInfo.Any;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.BillingCurrency = ebayItem.SellingStatus.HighBidder.SellerInfo.BillingCurrency;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.BillingCurrencySpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.BillingCurrencySpecified;

                                if (ebayItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails = new CharityAffiliationDetailType[ebayItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails.Length];
                                    for (int y = 0; ebayItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails.Length > y; y++)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[y] = new CharityAffiliationDetailType();
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[y].AffiliationType = ebayItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[y].AffiliationType;
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[y].AffiliationTypeSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[y].AffiliationTypeSpecified;
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[y].Any = ebayItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[y].Any;
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[y].LastUsedTime = ebayItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[y].LastUsedTime;
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[y].LastUsedTimeSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[y].LastUsedTimeSpecified;

                                        if (ebayItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[y].CharityID != null)
                                        {
                                            treecatItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[y].CharityID = ebayItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[y].CharityID;
                                        }
                                    }
                                }

                                treecatItem.SellingStatus.HighBidder.SellerInfo.CharityRegistered = ebayItem.SellingStatus.HighBidder.SellerInfo.CharityRegistered;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.CharityRegisteredSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.CharityRegisteredSpecified;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.CheckoutEnabled = ebayItem.SellingStatus.HighBidder.SellerInfo.CheckoutEnabled;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.CIPBankAccountStored = ebayItem.SellingStatus.HighBidder.SellerInfo.CIPBankAccountStored;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.DomesticRateTable = ebayItem.SellingStatus.HighBidder.SellerInfo.DomesticRateTable;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.DomesticRateTableSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.DomesticRateTableSpecified;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.GoodStanding = ebayItem.SellingStatus.HighBidder.SellerInfo.GoodStanding;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.InternationalRateTable = ebayItem.SellingStatus.HighBidder.SellerInfo.InternationalRateTable;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.InternationalRateTableSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.InternationalRateTableSpecified;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.MerchandizingPref = ebayItem.SellingStatus.HighBidder.SellerInfo.MerchandizingPref;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.MerchandizingPrefSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.MerchandizingPrefSpecified;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.PaisaPayEscrowEMIStatus = ebayItem.SellingStatus.HighBidder.SellerInfo.PaisaPayEscrowEMIStatus;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.PaisaPayEscrowEMIStatusSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.PaisaPayEscrowEMIStatusSpecified;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.PaisaPayStatus = ebayItem.SellingStatus.HighBidder.SellerInfo.PaisaPayStatus;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.PaisaPayStatusSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.PaisaPayStatusSpecified;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.PaymentMethod = ebayItem.SellingStatus.HighBidder.SellerInfo.PaymentMethod;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.PaymentMethodSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.PaymentMethodSpecified;

                                if (ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility = new FeatureEligibilityType();
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.Any = ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.Any;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForAuctionOneDayDuration = ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForAuctionOneDayDuration;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForAuctionOneDayDurationSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForAuctionOneDayDurationSpecified;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForFixedPriceOneDayDuration = ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForFixedPriceOneDayDuration;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForFixedPriceOneDayDurationSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForFixedPriceOneDayDurationSpecified;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNow = ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNow;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowMultiple = ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowMultiple;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowMultipleSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowMultipleSpecified;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowSpecified;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForVariations = ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForVariations;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForVariationsSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForVariationsSpecified;
                                }

                                if (ebayItem.SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo = new IntegratedMerchantCreditCardInfoType();
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo.Any = ebayItem.SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo.Any;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo.SupportedSite = ebayItem.SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo.SupportedSite;
                                }

                                if (ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference = new ProStoresCheckoutPreferenceType();
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.Any = ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.Any;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.CheckoutRedirectProStores = ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.CheckoutRedirectProStores;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.CheckoutRedirectProStoresSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.CheckoutRedirectProStoresSpecified;

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails = new ProStoresDetailsType();
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.Any = ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.Any;
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.Status = ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.Status;
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.StatusSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.StatusSpecified;

                                        if (ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.SellerThirdPartyUsername != null)
                                        {
                                            treecatItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.SellerThirdPartyUsername = ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.SellerThirdPartyUsername;
                                        }

                                        if (ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.StoreName != null)
                                        {
                                            treecatItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.StoreName = ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.StoreName;
                                        }
                                    }
                                }

                                treecatItem.SellingStatus.HighBidder.SellerInfo.QualifiesForB2BVAT = ebayItem.SellingStatus.HighBidder.SellerInfo.QualifiesForB2BVAT;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.RegisteredBusinessSeller = ebayItem.SellingStatus.HighBidder.SellerInfo.RegisteredBusinessSeller;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.RegisteredBusinessSellerSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.RegisteredBusinessSellerSpecified;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.SafePaymentExempt = ebayItem.SellingStatus.HighBidder.SellerInfo.SafePaymentExempt;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.SafePaymentExemptSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SafePaymentExemptSpecified;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.SellerBusinessType = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerBusinessType;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.SellerBusinessTypeSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerBusinessTypeSpecified;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessStatus = ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessStatus;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessStatusSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessStatusSpecified;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.SellerGuaranteeLevel = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerGuaranteeLevel;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.SellerGuaranteeLevelSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerGuaranteeLevelSpecified;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.SellerLevel = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerLevel;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.SellerLevelSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerLevelSpecified;

                                if (ebayItem.SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent = new RecoupmentPolicyConsentType();
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent.Any = ebayItem.SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent.Any;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent.Site = ebayItem.SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent.Site;
                                }

                                if (ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo = new SchedulingInfoType();
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.Any = ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.Any;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledItems = ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledItems;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledItemsSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledItemsSpecified;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledMinutes = ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledMinutes;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledMinutesSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledMinutesSpecified;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MinScheduledMinutes = ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MinScheduledMinutes;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MinScheduledMinutesSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MinScheduledMinutesSpecified;
                                }

                                if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent = new SellereBayPaymentProcessConsentCodeType();
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.Any = ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.Any;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethod = ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethod;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSet = ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSet;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSetSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSetSpecified;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSpecified;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo = ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo;
                                }

                                if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress = new AddressType();
                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute = new AddressAttributeType[ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute.Length];
                                        for (int y = 0; ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute.Length > y; y++)
                                        {
                                            treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[y] = new AddressAttributeType();
                                            treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[y].type = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[y].type;
                                            treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[y].typeSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[y].typeSpecified;

                                            if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[y].Value != null)
                                            {
                                                treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[y].Value = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[y].Value;
                                            }
                                        }
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressID != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressID = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressID;
                                    }

                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressOwner = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressOwner;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressOwnerSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressOwnerSpecified;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressRecordType = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressRecordType;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressRecordTypeSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressRecordTypeSpecified;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressStatus = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressStatus;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressStatusSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressStatusSpecified;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressUsage = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressUsage;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressUsageSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressUsageSpecified;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Any = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Any;

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CityName != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CityName = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CityName;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CompanyName != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CompanyName = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CompanyName;
                                    }

                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Country = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Country;

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CountryName != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CountryName = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CountryName;
                                    }

                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CountrySpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CountrySpecified;

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.County != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.County = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.County;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.ExternalAddressID != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.ExternalAddressID = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.ExternalAddressID;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.FirstName != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.FirstName = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.FirstName;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalName != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalName = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalName;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalStateAndCity != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalStateAndCity = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalStateAndCity;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalStreet != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalStreet = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalStreet;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.LastName != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.LastName = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.LastName;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Name != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Name = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Name;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Phone != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Phone = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Phone;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Phone2 != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Phone2 = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Phone2;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneAreaOrCityCode != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneAreaOrCityCode = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneAreaOrCityCode;
                                    }

                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneCountryCode = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneCountryCode;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneCountryCodeSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneCountryCodeSpecified;

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneCountryPrefix != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneCountryPrefix = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneCountryPrefix;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneLocalNumber != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneLocalNumber = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneLocalNumber;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PostalCode != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PostalCode = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PostalCode;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.ReferenceID != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.ReferenceID = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.ReferenceID;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.StateOrProvince != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.StateOrProvince = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.StateOrProvince;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street1 != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street1 = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street1;
                                    }

                                    if (ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street2 != null)
                                    {
                                        treecatItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street2 = ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street2;
                                    }
                                }

                                treecatItem.SellingStatus.HighBidder.SellerInfo.StoreOwner = ebayItem.SellingStatus.HighBidder.SellerInfo.StoreOwner;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.StoreSite = ebayItem.SellingStatus.HighBidder.SellerInfo.StoreSite;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.StoreSiteSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.StoreSiteSpecified;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.TopRatedSeller = ebayItem.SellingStatus.HighBidder.SellerInfo.TopRatedSeller;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.TopRatedSellerSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.TopRatedSellerSpecified;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.TransactionPercent = ebayItem.SellingStatus.HighBidder.SellerInfo.TransactionPercent;
                                treecatItem.SellingStatus.HighBidder.SellerInfo.TransactionPercentSpecified = ebayItem.SellingStatus.HighBidder.SellerInfo.TransactionPercentSpecified;

                                if (ebayItem.SellingStatus.HighBidder.SellerInfo.StoreURL != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.StoreURL = ebayItem.SellingStatus.HighBidder.SellerInfo.StoreURL;
                                }

                                if (ebayItem.SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails = new TopRatedSellerDetailsType();
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails.Any = ebayItem.SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails.Any;
                                    treecatItem.SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails.TopRatedProgram = ebayItem.SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails.TopRatedProgram;
                                }
                            }

                            treecatItem.SellingStatus.HighBidder.SellerPaymentMethod = ebayItem.SellingStatus.HighBidder.SellerPaymentMethod;
                            treecatItem.SellingStatus.HighBidder.SellerPaymentMethodSpecified = ebayItem.SellingStatus.HighBidder.SellerPaymentMethodSpecified;

                            if (ebayItem.SellingStatus.HighBidder.ShippingAddress != null)
                            {
                                treecatItem.SellingStatus.HighBidder.ShippingAddress = new AddressType();
                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressAttribute != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.AddressAttribute = new AddressAttributeType[ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressAttribute.Length];
                                    for (int y = 0; ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressAttribute.Length > y; y++)
                                    {
                                        treecatItem.SellingStatus.HighBidder.ShippingAddress.AddressAttribute[y] = new AddressAttributeType();
                                        treecatItem.SellingStatus.HighBidder.ShippingAddress.AddressAttribute[y].type = ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressAttribute[y].type;
                                        treecatItem.SellingStatus.HighBidder.ShippingAddress.AddressAttribute[y].typeSpecified = ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressAttribute[y].typeSpecified;

                                        if (ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressAttribute[y].Value != null)
                                        {
                                            treecatItem.SellingStatus.HighBidder.ShippingAddress.AddressAttribute[y].Value = ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressAttribute[y].Value;
                                        }
                                    }
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressID != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.AddressID = ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressID;
                                }

                                treecatItem.SellingStatus.HighBidder.ShippingAddress.AddressOwner = ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressOwner;
                                treecatItem.SellingStatus.HighBidder.ShippingAddress.AddressOwnerSpecified = ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressOwnerSpecified;
                                treecatItem.SellingStatus.HighBidder.ShippingAddress.AddressRecordType = ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressRecordType;
                                treecatItem.SellingStatus.HighBidder.ShippingAddress.AddressRecordTypeSpecified = ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressRecordTypeSpecified;
                                treecatItem.SellingStatus.HighBidder.ShippingAddress.AddressStatus = ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressStatus;
                                treecatItem.SellingStatus.HighBidder.ShippingAddress.AddressStatusSpecified = ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressStatusSpecified;
                                treecatItem.SellingStatus.HighBidder.ShippingAddress.AddressUsage = ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressUsage;
                                treecatItem.SellingStatus.HighBidder.ShippingAddress.AddressUsageSpecified = ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressUsageSpecified;
                                treecatItem.SellingStatus.HighBidder.ShippingAddress.Any = ebayItem.SellingStatus.HighBidder.ShippingAddress.Any;

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.CityName != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.CityName = ebayItem.SellingStatus.HighBidder.ShippingAddress.CityName;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.CompanyName != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.CompanyName = ebayItem.SellingStatus.HighBidder.ShippingAddress.CompanyName;
                                }

                                treecatItem.SellingStatus.HighBidder.ShippingAddress.Country = ebayItem.SellingStatus.HighBidder.ShippingAddress.Country;

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.CountryName != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.CountryName = ebayItem.SellingStatus.HighBidder.ShippingAddress.CountryName;
                                }

                                treecatItem.SellingStatus.HighBidder.ShippingAddress.CountrySpecified = ebayItem.SellingStatus.HighBidder.ShippingAddress.CountrySpecified;

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.County != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.County = ebayItem.SellingStatus.HighBidder.ShippingAddress.County;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.ExternalAddressID != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.ExternalAddressID = ebayItem.SellingStatus.HighBidder.ShippingAddress.ExternalAddressID;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.FirstName != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.FirstName = ebayItem.SellingStatus.HighBidder.ShippingAddress.FirstName;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.InternationalName != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.InternationalName = ebayItem.SellingStatus.HighBidder.ShippingAddress.InternationalName;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.InternationalStateAndCity != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.InternationalStateAndCity = ebayItem.SellingStatus.HighBidder.ShippingAddress.InternationalStateAndCity;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.InternationalStreet != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.InternationalStreet = ebayItem.SellingStatus.HighBidder.ShippingAddress.InternationalStreet;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.LastName != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.LastName = ebayItem.SellingStatus.HighBidder.ShippingAddress.LastName;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.Name != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.Name = ebayItem.SellingStatus.HighBidder.ShippingAddress.Name;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.Phone != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.Phone = ebayItem.SellingStatus.HighBidder.ShippingAddress.Phone;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.Phone2 != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.Phone2 = ebayItem.SellingStatus.HighBidder.ShippingAddress.Phone2;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.PhoneAreaOrCityCode != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.PhoneAreaOrCityCode = ebayItem.SellingStatus.HighBidder.ShippingAddress.PhoneAreaOrCityCode;
                                }

                                treecatItem.SellingStatus.HighBidder.ShippingAddress.PhoneCountryCode = ebayItem.SellingStatus.HighBidder.ShippingAddress.PhoneCountryCode;
                                treecatItem.SellingStatus.HighBidder.ShippingAddress.PhoneCountryCodeSpecified = ebayItem.SellingStatus.HighBidder.ShippingAddress.PhoneCountryCodeSpecified;

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.PhoneCountryPrefix != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.PhoneCountryPrefix = ebayItem.SellingStatus.HighBidder.ShippingAddress.PhoneCountryPrefix;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.PhoneLocalNumber != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.PhoneLocalNumber = ebayItem.SellingStatus.HighBidder.ShippingAddress.PhoneLocalNumber;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.PostalCode != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.PostalCode = ebayItem.SellingStatus.HighBidder.ShippingAddress.PostalCode;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.ReferenceID != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.ReferenceID = ebayItem.SellingStatus.HighBidder.ShippingAddress.ReferenceID;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.StateOrProvince != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.StateOrProvince = ebayItem.SellingStatus.HighBidder.ShippingAddress.StateOrProvince;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.Street != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.Street = ebayItem.SellingStatus.HighBidder.ShippingAddress.Street;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.Street1 != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.Street1 = ebayItem.SellingStatus.HighBidder.ShippingAddress.Street1;
                                }

                                if (ebayItem.SellingStatus.HighBidder.ShippingAddress.Street2 != null)
                                {
                                    treecatItem.SellingStatus.HighBidder.ShippingAddress.Street2 = ebayItem.SellingStatus.HighBidder.ShippingAddress.Street2;
                                }

                            }

                            if (ebayItem.SellingStatus.HighBidder.SkypeID != null)
                            {
                                treecatItem.SellingStatus.HighBidder.SkypeID = ebayItem.SellingStatus.HighBidder.SkypeID;
                            }

                            if (ebayItem.SellingStatus.HighBidder.StaticAlias != null)
                            {
                                treecatItem.SellingStatus.HighBidder.StaticAlias = ebayItem.SellingStatus.HighBidder.StaticAlias;
                            }

                            if (ebayItem.SellingStatus.HighBidder.UserFirstName != null)
                            {
                                treecatItem.SellingStatus.HighBidder.UserFirstName = ebayItem.SellingStatus.HighBidder.UserFirstName;
                            }

                            if (ebayItem.SellingStatus.HighBidder.UserID != null)
                            {
                                treecatItem.SellingStatus.HighBidder.UserID = ebayItem.SellingStatus.HighBidder.UserID;
                            }

                            if (ebayItem.SellingStatus.HighBidder.UserLastName != null)
                            {
                                treecatItem.SellingStatus.HighBidder.UserLastName = ebayItem.SellingStatus.HighBidder.UserLastName;
                            }

                            if (ebayItem.SellingStatus.HighBidder.VATID != null)
                            {
                                treecatItem.SellingStatus.HighBidder.VATID = ebayItem.SellingStatus.HighBidder.VATID;
                            }

                            treecatItem.SellingStatus.HighBidder.Site = ebayItem.SellingStatus.HighBidder.Site;
                            treecatItem.SellingStatus.HighBidder.SiteSpecified = ebayItem.SellingStatus.HighBidder.SiteSpecified;
                            treecatItem.SellingStatus.HighBidder.SiteVerified = ebayItem.SellingStatus.HighBidder.SiteVerified;
                            treecatItem.SellingStatus.HighBidder.SiteVerifiedSpecified = ebayItem.SellingStatus.HighBidder.SiteVerifiedSpecified;
                            treecatItem.SellingStatus.HighBidder.Status = ebayItem.SellingStatus.HighBidder.Status;
                            treecatItem.SellingStatus.HighBidder.StatusSpecified = ebayItem.SellingStatus.HighBidder.StatusSpecified;
                            treecatItem.SellingStatus.HighBidder.TUVLevel = ebayItem.SellingStatus.HighBidder.TUVLevel;
                            treecatItem.SellingStatus.HighBidder.TUVLevelSpecified = ebayItem.SellingStatus.HighBidder.TUVLevelSpecified;
                            treecatItem.SellingStatus.HighBidder.UniqueNegativeFeedbackCount = ebayItem.SellingStatus.HighBidder.UniqueNegativeFeedbackCount;
                            treecatItem.SellingStatus.HighBidder.UniqueNegativeFeedbackCountSpecified = ebayItem.SellingStatus.HighBidder.UniqueNegativeFeedbackCountSpecified;
                            treecatItem.SellingStatus.HighBidder.UniqueNeutralFeedbackCount = ebayItem.SellingStatus.HighBidder.UniqueNeutralFeedbackCount;
                            treecatItem.SellingStatus.HighBidder.UniqueNeutralFeedbackCountSpecified = ebayItem.SellingStatus.HighBidder.UniqueNeutralFeedbackCountSpecified;
                            treecatItem.SellingStatus.HighBidder.UniquePositiveFeedbackCount = ebayItem.SellingStatus.HighBidder.UniquePositiveFeedbackCount;
                            treecatItem.SellingStatus.HighBidder.UniquePositiveFeedbackCountSpecified = ebayItem.SellingStatus.HighBidder.UniquePositiveFeedbackCountSpecified;
                            treecatItem.SellingStatus.HighBidder.UserAnonymized = ebayItem.SellingStatus.HighBidder.UserAnonymized;
                            treecatItem.SellingStatus.HighBidder.UserAnonymizedSpecified = ebayItem.SellingStatus.HighBidder.UserAnonymizedSpecified;
                            treecatItem.SellingStatus.HighBidder.UserIDChanged = ebayItem.SellingStatus.HighBidder.UserIDChanged;
                            treecatItem.SellingStatus.HighBidder.UserIDChangedSpecified = ebayItem.SellingStatus.HighBidder.UserIDChangedSpecified;
                            treecatItem.SellingStatus.HighBidder.UserSubscription = ebayItem.SellingStatus.HighBidder.UserSubscription;
                            treecatItem.SellingStatus.HighBidder.VATStatus = ebayItem.SellingStatus.HighBidder.VATStatus;
                            treecatItem.SellingStatus.HighBidder.VATStatusSpecified = ebayItem.SellingStatus.HighBidder.VATStatusSpecified;
                        }

                        treecatItem.SellingStatus.LeadCount = ebayItem.SellingStatus.LeadCount;
                        treecatItem.SellingStatus.LeadCountSpecified = ebayItem.SellingStatus.LeadCountSpecified;
                        treecatItem.SellingStatus.ListingStatus = ebayItem.SellingStatus.ListingStatus;
                        treecatItem.SellingStatus.ListingStatusSpecified = ebayItem.SellingStatus.ListingStatusSpecified;

                        if (ebayItem.SellingStatus.MinimumToBid != null)
                        {
                            treecatItem.SellingStatus.MinimumToBid = new AmountType();
                            treecatItem.SellingStatus.MinimumToBid.currencyID = ebayItem.SellingStatus.MinimumToBid.currencyID;
                            treecatItem.SellingStatus.MinimumToBid.Value = ebayItem.SellingStatus.MinimumToBid.Value;
                        }

                        if (ebayItem.SellingStatus.PromotionalSaleDetails != null)
                        {
                            treecatItem.SellingStatus.PromotionalSaleDetails = new PromotionalSaleDetailsType();
                            treecatItem.SellingStatus.PromotionalSaleDetails.Any = ebayItem.SellingStatus.PromotionalSaleDetails.Any;
                            treecatItem.SellingStatus.PromotionalSaleDetails.EndTime = ebayItem.SellingStatus.PromotionalSaleDetails.EndTime;
                            treecatItem.SellingStatus.PromotionalSaleDetails.EndTimeSpecified = ebayItem.SellingStatus.PromotionalSaleDetails.EndTimeSpecified;
                            treecatItem.SellingStatus.PromotionalSaleDetails.StartTime = ebayItem.SellingStatus.PromotionalSaleDetails.StartTime;
                            treecatItem.SellingStatus.PromotionalSaleDetails.StartTimeSpecified = ebayItem.SellingStatus.PromotionalSaleDetails.StartTimeSpecified;

                            if (ebayItem.SellingStatus.PromotionalSaleDetails.OriginalPrice != null)
                            {
                                treecatItem.SellingStatus.PromotionalSaleDetails.OriginalPrice = new AmountType();
                                treecatItem.SellingStatus.PromotionalSaleDetails.OriginalPrice.currencyID = ebayItem.SellingStatus.PromotionalSaleDetails.OriginalPrice.currencyID;
                                treecatItem.SellingStatus.PromotionalSaleDetails.OriginalPrice.Value = ebayItem.SellingStatus.PromotionalSaleDetails.OriginalPrice.Value;
                            }
                        }

                        treecatItem.SellingStatus.QuantitySold = ebayItem.SellingStatus.QuantitySold;
                        treecatItem.SellingStatus.QuantitySoldByPickupInStore = ebayItem.SellingStatus.QuantitySoldByPickupInStore;
                        treecatItem.SellingStatus.QuantitySoldByPickupInStoreSpecified = ebayItem.SellingStatus.QuantitySoldByPickupInStoreSpecified;
                        treecatItem.SellingStatus.QuantitySoldSpecified = ebayItem.SellingStatus.QuantitySoldSpecified;
                        treecatItem.SellingStatus.ReserveMet = ebayItem.SellingStatus.ReserveMet;
                        treecatItem.SellingStatus.ReserveMetSpecified = ebayItem.SellingStatus.ReserveMetSpecified;
                        treecatItem.SellingStatus.SecondChanceEligible = ebayItem.SellingStatus.SecondChanceEligible;
                        treecatItem.SellingStatus.SecondChanceEligibleSpecified = ebayItem.SellingStatus.SecondChanceEligibleSpecified;
                        treecatItem.SellingStatus.SoldAsBin = ebayItem.SellingStatus.SoldAsBin;
                        treecatItem.SellingStatus.SoldAsBinSpecified = ebayItem.SellingStatus.SoldAsBinSpecified;


                        if (ebayItem.SellingStatus.SuggestedBidValues != null)
                        {
                            treecatItem.SellingStatus.SuggestedBidValues.Any = ebayItem.SellingStatus.SuggestedBidValues.Any;
                            if (ebayItem.SellingStatus.SuggestedBidValues.BidValue != null)
                            {
                                treecatItem.SellingStatus.SuggestedBidValues.BidValue = new AmountType[ebayItem.SellingStatus.SuggestedBidValues.BidValue.Length];
                                for (int y = 0; ebayItem.SellingStatus.SuggestedBidValues.BidValue.Length > y; y++)
                                {
                                    treecatItem.SellingStatus.SuggestedBidValues.BidValue[y] = new AmountType();
                                    treecatItem.SellingStatus.SuggestedBidValues.BidValue[y].currencyID = ebayItem.SellingStatus.SuggestedBidValues.BidValue[y].currencyID;
                                    treecatItem.SellingStatus.SuggestedBidValues.BidValue[y].Value = ebayItem.SellingStatus.SuggestedBidValues.BidValue[y].Value;
                                }
                            }
                        }
                    }

                    if (ebayItem.ShippingDetails != null)
                    {
                        treecatItem.ShippingDetails = new ShippingDetailsType();
                        treecatItem.ShippingDetails.AllowPaymentEdit = ebayItem.ShippingDetails.AllowPaymentEdit;
                        treecatItem.ShippingDetails.AllowPaymentEditSpecified = ebayItem.ShippingDetails.AllowPaymentEditSpecified;
                        treecatItem.ShippingDetails.Any = ebayItem.ShippingDetails.Any;
                        treecatItem.ShippingDetails.ApplyShippingDiscount = ebayItem.ShippingDetails.ApplyShippingDiscount;
                        treecatItem.ShippingDetails.ApplyShippingDiscountSpecified = ebayItem.ShippingDetails.ApplyShippingDiscountSpecified;

                        if (ebayItem.ShippingDetails.CalculatedShippingDiscount != null)
                        {
                            treecatItem.ShippingDetails.CalculatedShippingDiscount = new CalculatedShippingDiscountType();
                            treecatItem.ShippingDetails.CalculatedShippingDiscount.Any = ebayItem.ShippingDetails.CalculatedShippingDiscount.Any;
                            treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountName = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountName;
                            treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountNameSpecified = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountNameSpecified;

                            if (ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile != null)
                            {
                                treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile = new DiscountProfileType[ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile.Length];
                                for (int y = 0; ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile.Length > y; y++)
                                {
                                    treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y] = new DiscountProfileType();
                                    treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].Any = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].Any;
                                    treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalPercentOff = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalPercentOff;
                                    treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalPercentOffSpecified = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalPercentOffSpecified;

                                    if (ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].DiscountProfileID != null)
                                    {
                                        treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].DiscountProfileID = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].DiscountProfileID;
                                    }

                                    if (ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].DiscountProfileName != null)
                                    {
                                        treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].DiscountProfileName = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].DiscountProfileName;
                                    }

                                    if (ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmount != null)
                                    {
                                        treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmount = new AmountType();
                                        treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmount.currencyID = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmount.currencyID;
                                        treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmount.Value = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmount.Value;
                                    }

                                    if (ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff != null)
                                    {
                                        treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff = new AmountType();
                                        treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.currencyID = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.currencyID;
                                        treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.Value = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.Value;
                                    }

                                    if (ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].MappedDiscountProfileID != null)
                                    {
                                        treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].MappedDiscountProfileID = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].MappedDiscountProfileID;
                                    }

                                    if (ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].WeightOff != null)
                                    {
                                        treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].WeightOff = new MeasureType();
                                        treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].WeightOff.measurementSystem = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].WeightOff.measurementSystem;
                                        treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].WeightOff.measurementSystemSpecified = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].WeightOff.measurementSystemSpecified;
                                        treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].WeightOff.Value = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].WeightOff.Value;

                                        if (ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].WeightOff.unit != null)
                                        {
                                            treecatItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].WeightOff.unit = ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile[y].WeightOff.unit;
                                        }
                                    }
                                }
                            }
                        }



                        if (ebayItem.ShippingDetails.CalculatedShippingRate != null)
                        {
                            treecatItem.ShippingDetails.CalculatedShippingRate = new CalculatedShippingRateType();

                            if (ebayItem.ShippingDetails.CalculatedShippingRate.InternationalPackagingHandlingCosts != null)
                            {
                                treecatItem.ShippingDetails.CalculatedShippingRate.InternationalPackagingHandlingCosts = new AmountType();
                                treecatItem.ShippingDetails.CalculatedShippingRate.InternationalPackagingHandlingCosts.currencyID = ebayItem.ShippingDetails.CalculatedShippingRate.InternationalPackagingHandlingCosts.currencyID;
                                treecatItem.ShippingDetails.CalculatedShippingRate.InternationalPackagingHandlingCosts.Value = ebayItem.ShippingDetails.CalculatedShippingRate.InternationalPackagingHandlingCosts.Value;
                            }

                            if (ebayItem.ShippingDetails.CalculatedShippingRate.OriginatingPostalCode != null)
                            {
                                treecatItem.ShippingDetails.CalculatedShippingRate.OriginatingPostalCode = ebayItem.ShippingDetails.CalculatedShippingRate.OriginatingPostalCode;
                            }

                            if (ebayItem.ShippingDetails.CalculatedShippingRate.PackagingHandlingCosts != null)
                            {
                                treecatItem.ShippingDetails.CalculatedShippingRate.PackagingHandlingCosts = new AmountType();
                                treecatItem.ShippingDetails.CalculatedShippingRate.PackagingHandlingCosts.currencyID = ebayItem.ShippingDetails.CalculatedShippingRate.PackagingHandlingCosts.currencyID;
                                treecatItem.ShippingDetails.CalculatedShippingRate.PackagingHandlingCosts.Value = ebayItem.ShippingDetails.CalculatedShippingRate.PackagingHandlingCosts.Value;
                            }

                            treecatItem.ShippingDetails.CalculatedShippingRate.Any = ebayItem.ShippingDetails.CalculatedShippingRate.Any;
                            treecatItem.ShippingDetails.CalculatedShippingRate.MeasurementUnit = ebayItem.ShippingDetails.CalculatedShippingRate.MeasurementUnit;
                            treecatItem.ShippingDetails.CalculatedShippingRate.MeasurementUnitSpecified = ebayItem.ShippingDetails.CalculatedShippingRate.MeasurementUnitSpecified;
                            treecatItem.ShippingDetails.CalculatedShippingRate.ShippingIrregular = ebayItem.ShippingDetails.CalculatedShippingRate.ShippingIrregular;
                            treecatItem.ShippingDetails.CalculatedShippingRate.ShippingIrregularSpecified = ebayItem.ShippingDetails.CalculatedShippingRate.ShippingIrregularSpecified;
                        }

                        treecatItem.ShippingDetails.ChangePaymentInstructions = ebayItem.ShippingDetails.ChangePaymentInstructions;
                        treecatItem.ShippingDetails.ChangePaymentInstructionsSpecified = ebayItem.ShippingDetails.ChangePaymentInstructionsSpecified;

                        if (ebayItem.ShippingDetails.CODCost != null)
                        {
                            treecatItem.ShippingDetails.CODCost = new AmountType();
                            treecatItem.ShippingDetails.CODCost.currencyID = ebayItem.ShippingDetails.CODCost.currencyID;
                            treecatItem.ShippingDetails.CODCost.Value = ebayItem.ShippingDetails.CODCost.Value;
                        }

                        if (ebayItem.ShippingDetails.DefaultShippingCost != null)
                        {
                            treecatItem.ShippingDetails.DefaultShippingCost = new AmountType();
                            treecatItem.ShippingDetails.DefaultShippingCost.currencyID = ebayItem.ShippingDetails.DefaultShippingCost.currencyID;
                            treecatItem.ShippingDetails.DefaultShippingCost.Value = ebayItem.ShippingDetails.DefaultShippingCost.Value;
                        }

                        if (ebayItem.ShippingDetails.ExcludeShipToLocation != null)
                        {
                            treecatItem.ShippingDetails.ExcludeShipToLocation = ebayItem.ShippingDetails.ExcludeShipToLocation;
                        }

                        if (ebayItem.ShippingDetails.FlatShippingDiscount != null)
                        {
                            treecatItem.ShippingDetails.FlatShippingDiscount = new FlatShippingDiscountType();
                            treecatItem.ShippingDetails.FlatShippingDiscount.Any = ebayItem.ShippingDetails.FlatShippingDiscount.Any;
                            treecatItem.ShippingDetails.FlatShippingDiscount.DiscountName = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountName;
                            treecatItem.ShippingDetails.FlatShippingDiscount.DiscountNameSpecified = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountNameSpecified;

                            if (ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile != null)
                            {
                                treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile = new DiscountProfileType[ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile.Length];
                                for (int y = 0; ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile.Length > y; y++)
                                {
                                    treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y] = new DiscountProfileType();
                                    treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].Any = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].Any;
                                    treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalPercentOff = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalPercentOff;
                                    treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalPercentOffSpecified = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalPercentOffSpecified;

                                    if (ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].DiscountProfileID != null)
                                    {
                                        treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].DiscountProfileID = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].DiscountProfileID;
                                    }

                                    if (ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].DiscountProfileName != null)
                                    {
                                        treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].DiscountProfileName = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].DiscountProfileName;
                                    }

                                    if (ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalAmount != null)
                                    {
                                        treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalAmount = new AmountType();
                                        treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalAmount.currencyID = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalAmount.currencyID;
                                        treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalAmount.Value = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalAmount.Value;
                                    }

                                    if (ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff != null)
                                    {
                                        treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff = new AmountType();
                                        treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.currencyID = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.currencyID;
                                        treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.Value = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.Value;
                                    }

                                    if (ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].MappedDiscountProfileID != null)
                                    {
                                        treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].MappedDiscountProfileID = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].MappedDiscountProfileID;
                                    }

                                    if (ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].WeightOff != null)
                                    {
                                        treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].WeightOff = new MeasureType();
                                        treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].WeightOff.measurementSystem = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].WeightOff.measurementSystem;
                                        treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].WeightOff.measurementSystemSpecified = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].WeightOff.measurementSystemSpecified;
                                        treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].WeightOff.Value = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].WeightOff.Value;

                                        if (ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].WeightOff.unit != null)
                                        {
                                            treecatItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].WeightOff.unit = ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile[y].WeightOff.unit;
                                        }
                                    }
                                }
                            }
                        }

                        treecatItem.ShippingDetails.GetItFast = ebayItem.ShippingDetails.GetItFast;
                        treecatItem.ShippingDetails.GetItFastSpecified = ebayItem.ShippingDetails.GetItFastSpecified;
                        treecatItem.ShippingDetails.GlobalShipping = ebayItem.ShippingDetails.GlobalShipping;
                        treecatItem.ShippingDetails.GlobalShippingSpecified = ebayItem.ShippingDetails.GlobalShippingSpecified;
                        treecatItem.ShippingDetails.InsuranceWanted = ebayItem.ShippingDetails.InsuranceWanted;
                        treecatItem.ShippingDetails.InsuranceWantedSpecified = ebayItem.ShippingDetails.InsuranceWantedSpecified;

                        if (ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount != null)
                        {
                            treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount = new CalculatedShippingDiscountType();
                            treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountName = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountName;
                            treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountNameSpecified = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountNameSpecified;

                            if (ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile != null)
                            {
                                treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile = new DiscountProfileType[ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile.Length];
                                for (int y = 0; ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile.Length > y; y++)
                                {
                                    treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y] = new DiscountProfileType();
                                    treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].Any = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].Any;
                                    treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalPercentOff = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalPercentOff;
                                    treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalPercentOffSpecified = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalPercentOffSpecified;

                                    if (ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].DiscountProfileID != null)
                                    {
                                        treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].DiscountProfileID = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].DiscountProfileID;
                                    }

                                    if (ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].DiscountProfileName != null)
                                    {
                                        treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].DiscountProfileName = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].DiscountProfileName;
                                    }

                                    if (ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmount != null)
                                    {
                                        treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmount = new AmountType();
                                        treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmount.currencyID = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmount.currencyID;
                                        treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmount.Value = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmount.Value;
                                    }

                                    if (ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff != null)
                                    {
                                        treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff = new AmountType();
                                        treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.currencyID = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.currencyID;
                                        treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.Value = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.Value;
                                    }

                                    if (ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].MappedDiscountProfileID != null)
                                    {
                                        treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].MappedDiscountProfileID = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].MappedDiscountProfileID;
                                    }

                                    if (ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].WeightOff != null)
                                    {
                                        treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].WeightOff = new MeasureType();
                                        treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].WeightOff.measurementSystem = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].WeightOff.measurementSystem;
                                        treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].WeightOff.measurementSystemSpecified = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].WeightOff.measurementSystemSpecified;
                                        treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].WeightOff.Value = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].WeightOff.Value;

                                        if (ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].WeightOff.unit != null)
                                        {
                                            treecatItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].WeightOff.unit = ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile[y].WeightOff.unit;
                                        }
                                    }
                                }
                            }
                        }

                        if (ebayItem.ShippingDetails.InternationalFlatShippingDiscount != null)
                        {
                            treecatItem.ShippingDetails.InternationalFlatShippingDiscount = new FlatShippingDiscountType();
                            treecatItem.ShippingDetails.InternationalFlatShippingDiscount.Any = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.Any;
                            treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountName = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountName;
                            treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountNameSpecified = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountNameSpecified;

                            if (ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile != null)
                            {
                                treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile = new DiscountProfileType[ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile.Length];
                                for (int y = 0; ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile.Length > y; y++)
                                {
                                    treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y] = new DiscountProfileType();
                                    treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].Any = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].Any;
                                    treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalPercentOff = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalPercentOff;
                                    treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalPercentOffSpecified = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalPercentOffSpecified;

                                    if (ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].DiscountProfileID != null)
                                    {
                                        treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].DiscountProfileID = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].DiscountProfileID;
                                    }

                                    if (ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].DiscountProfileName != null)
                                    {
                                        treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].DiscountProfileName = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].DiscountProfileName;
                                    }

                                    if (ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalAmount != null)
                                    {
                                        treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalAmount = new AmountType();
                                        treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalAmount.currencyID = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalAmount.currencyID;
                                        treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalAmount.Value = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalAmount.Value;
                                    }

                                    if (ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff != null)
                                    {
                                        treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff = new AmountType();
                                        treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.currencyID = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.currencyID;
                                        treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.Value = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].EachAdditionalAmountOff.Value;
                                    }

                                    if (ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].MappedDiscountProfileID != null)
                                    {
                                        treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].MappedDiscountProfileID = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].MappedDiscountProfileID;
                                    }

                                    if (ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].WeightOff != null)
                                    {
                                        treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].WeightOff = new MeasureType();
                                        treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].WeightOff.measurementSystem = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].WeightOff.measurementSystem;
                                        treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].WeightOff.measurementSystemSpecified = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].WeightOff.measurementSystemSpecified;
                                        treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].WeightOff.Value = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].WeightOff.Value;

                                        if (ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].WeightOff.unit != null)
                                        {
                                            treecatItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].WeightOff.unit = ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile[y].WeightOff.unit;
                                        }
                                    }
                                }
                            }
                        }

                        treecatItem.ShippingDetails.InternationalPromotionalShippingDiscount = ebayItem.ShippingDetails.InternationalPromotionalShippingDiscount;
                        treecatItem.ShippingDetails.InternationalPromotionalShippingDiscountSpecified = ebayItem.ShippingDetails.InternationalPromotionalShippingDiscountSpecified;

                        if (ebayItem.ShippingDetails.InternationalShippingDiscountProfileID != null)
                        {
                            treecatItem.ShippingDetails.InternationalShippingDiscountProfileID = ebayItem.ShippingDetails.InternationalShippingDiscountProfileID;
                        }

                        if (ebayItem.ShippingDetails.InternationalShippingServiceOption != null)
                        {
                            treecatItem.ShippingDetails.InternationalShippingServiceOption = new InternationalShippingServiceOptionsType[ebayItem.ShippingDetails.InternationalShippingServiceOption.Length];
                            for (int y = 0; ebayItem.ShippingDetails.InternationalShippingServiceOption.Length > y; y++)
                            {
                                treecatItem.ShippingDetails.InternationalShippingServiceOption[y] = new InternationalShippingServiceOptionsType();
                                treecatItem.ShippingDetails.InternationalShippingServiceOption[y].Any = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].Any;
                                treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingService = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingService;
                                treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceCutOffTime = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceCutOffTime;
                                treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceCutOffTimeSpecified = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceCutOffTimeSpecified;
                                treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServicePriority = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServicePriority;
                                treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServicePrioritySpecified = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServicePrioritySpecified;

                                if (ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceAdditionalCost != null)
                                {
                                    treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceAdditionalCost = new AmountType();
                                    treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceAdditionalCost.currencyID = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceAdditionalCost.currencyID;
                                    treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceAdditionalCost.Value = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceAdditionalCost.Value;
                                }

                                if (ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceCost != null)
                                {
                                    treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceCost = new AmountType();
                                    treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceCost.currencyID = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceCost.currencyID;
                                    treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceCost.Value = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingServiceCost.Value;
                                }

                                if (ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ImportCharge != null)
                                {
                                    treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ImportCharge = new AmountType();
                                    treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ImportCharge.currencyID = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ImportCharge.currencyID;
                                    treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ImportCharge.Value = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ImportCharge.Value;
                                }

                                if (ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingInsuranceCost != null)
                                {
                                    treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingInsuranceCost = new AmountType();
                                    treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingInsuranceCost.currencyID = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingInsuranceCost.currencyID;
                                    treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingInsuranceCost.Value = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShippingInsuranceCost.Value;
                                }

                                if (ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShipToLocation != null)
                                {
                                    treecatItem.ShippingDetails.InternationalShippingServiceOption[y].ShipToLocation = ebayItem.ShippingDetails.InternationalShippingServiceOption[y].ShipToLocation;
                                }
                            }
                        }

                        treecatItem.ShippingDetails.PaymentEdited = ebayItem.ShippingDetails.PaymentEdited;
                        treecatItem.ShippingDetails.PaymentEditedSpecified = ebayItem.ShippingDetails.PaymentEditedSpecified;
                        treecatItem.ShippingDetails.PromotionalShippingDiscount = ebayItem.ShippingDetails.PromotionalShippingDiscount;
                        treecatItem.ShippingDetails.PromotionalShippingDiscountSpecified = ebayItem.ShippingDetails.PromotionalShippingDiscountSpecified;

                        if (ebayItem.ShippingDetails.PaymentInstructions != null)
                        {
                            treecatItem.ShippingDetails.PaymentInstructions = ebayItem.ShippingDetails.PaymentInstructions;
                        }

                        if (ebayItem.ShippingDetails.PromotionalShippingDiscountDetails != null)
                        {
                            treecatItem.ShippingDetails.PromotionalShippingDiscountDetails = new PromotionalShippingDiscountDetailsType();
                            treecatItem.ShippingDetails.PromotionalShippingDiscountDetails.Any = ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.Any;
                            treecatItem.ShippingDetails.PromotionalShippingDiscountDetails.DiscountName = ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.DiscountName;
                            treecatItem.ShippingDetails.PromotionalShippingDiscountDetails.DiscountNameSpecified = ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.DiscountNameSpecified;
                            treecatItem.ShippingDetails.PromotionalShippingDiscountDetails.ItemCount = ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.ItemCount;
                            treecatItem.ShippingDetails.PromotionalShippingDiscountDetails.ItemCountSpecified = ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.ItemCountSpecified;

                            if (ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.OrderAmount != null)
                            {
                                treecatItem.ShippingDetails.PromotionalShippingDiscountDetails.OrderAmount = new AmountType();
                                treecatItem.ShippingDetails.PromotionalShippingDiscountDetails.OrderAmount.currencyID = ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.OrderAmount.currencyID;
                                treecatItem.ShippingDetails.PromotionalShippingDiscountDetails.OrderAmount.Value = ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.OrderAmount.Value;
                            }

                            if (ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.ShippingCost != null)
                            {
                                treecatItem.ShippingDetails.PromotionalShippingDiscountDetails.ShippingCost = new AmountType();
                                treecatItem.ShippingDetails.PromotionalShippingDiscountDetails.ShippingCost.currencyID = ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.ShippingCost.currencyID;
                                treecatItem.ShippingDetails.PromotionalShippingDiscountDetails.ShippingCost.Value = ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.ShippingCost.Value;
                            }
                        }

                        if (ebayItem.ShippingDetails.RateTableDetails != null)
                        {
                            treecatItem.ShippingDetails.RateTableDetails = new RateTableDetailsType();
                            treecatItem.ShippingDetails.RateTableDetails.Any = ebayItem.ShippingDetails.RateTableDetails.Any;

                            if (ebayItem.ShippingDetails.RateTableDetails.DomesticRateTable != null)
                            {
                                treecatItem.ShippingDetails.RateTableDetails.DomesticRateTable = ebayItem.ShippingDetails.RateTableDetails.DomesticRateTable;
                            }

                            if (ebayItem.ShippingDetails.RateTableDetails.DomesticRateTableId != null)
                            {
                                treecatItem.ShippingDetails.RateTableDetails.DomesticRateTableId = ebayItem.ShippingDetails.RateTableDetails.DomesticRateTableId;
                            }

                            if (ebayItem.ShippingDetails.RateTableDetails.InternationalRateTable != null)
                            {
                                treecatItem.ShippingDetails.RateTableDetails.InternationalRateTable = ebayItem.ShippingDetails.RateTableDetails.InternationalRateTable;
                            }

                            if (ebayItem.ShippingDetails.RateTableDetails.InternationalRateTableId != null)
                            {
                                treecatItem.ShippingDetails.RateTableDetails.InternationalRateTableId = ebayItem.ShippingDetails.RateTableDetails.InternationalRateTableId;
                            }
                        }

                        if (ebayItem.ShippingDetails.SalesTax != null)
                        {
                            treecatItem.ShippingDetails.SalesTax = new SalesTaxType();
                            if (ebayItem.ShippingDetails.SalesTax.SalesTaxAmount != null)
                            {
                                treecatItem.ShippingDetails.SalesTax.SalesTaxAmount = new AmountType();
                                treecatItem.ShippingDetails.SalesTax.SalesTaxAmount.currencyID = ebayItem.ShippingDetails.SalesTax.SalesTaxAmount.currencyID;
                                treecatItem.ShippingDetails.SalesTax.SalesTaxAmount.Value = ebayItem.ShippingDetails.SalesTax.SalesTaxAmount.Value;
                            }

                            if (ebayItem.ShippingDetails.SalesTax.SalesTaxState != null)
                            {
                                treecatItem.ShippingDetails.SalesTax.SalesTaxState = ebayItem.ShippingDetails.SalesTax.SalesTaxState;
                            }

                            treecatItem.ShippingDetails.SalesTax.SalesTaxPercent = ebayItem.ShippingDetails.SalesTax.SalesTaxPercent;
                            treecatItem.ShippingDetails.SalesTax.SalesTaxPercentSpecified = ebayItem.ShippingDetails.SalesTax.SalesTaxPercentSpecified;
                            treecatItem.ShippingDetails.SalesTax.ShippingIncludedInTax = ebayItem.ShippingDetails.SalesTax.ShippingIncludedInTax;
                            treecatItem.ShippingDetails.SalesTax.ShippingIncludedInTaxSpecified = ebayItem.ShippingDetails.SalesTax.ShippingIncludedInTaxSpecified;
                        }

                        treecatItem.ShippingDetails.SellerExcludeShipToLocationsPreference = ebayItem.ShippingDetails.SellerExcludeShipToLocationsPreference;
                        treecatItem.ShippingDetails.SellerExcludeShipToLocationsPreferenceSpecified = ebayItem.ShippingDetails.SellerExcludeShipToLocationsPreferenceSpecified;
                        treecatItem.ShippingDetails.SellingManagerSalesRecordNumber = ebayItem.ShippingDetails.SellingManagerSalesRecordNumber;
                        treecatItem.ShippingDetails.SellingManagerSalesRecordNumberSpecified = ebayItem.ShippingDetails.SellingManagerSalesRecordNumberSpecified;

                        if (ebayItem.ShippingDetails.ShipmentTrackingDetails != null)
                        {
                            treecatItem.ShippingDetails.ShipmentTrackingDetails = new ShipmentTrackingDetailsType[ebayItem.ShippingDetails.ShipmentTrackingDetails.Length];
                            for (int y = 0; ebayItem.ShippingDetails.ShipmentTrackingDetails.Length > y; y++)
                            {
                                treecatItem.ShippingDetails.ShipmentTrackingDetails[y] = new ShipmentTrackingDetailsType();
                                treecatItem.ShippingDetails.ShipmentTrackingDetails[y].Any = ebayItem.ShippingDetails.ShipmentTrackingDetails[y].Any;

                                if (ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem != null)
                                {
                                    treecatItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem = new ShipmentLineItemType();
                                    treecatItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.Any = ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.Any;

                                    if (ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem != null)
                                    {
                                        treecatItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem = new LineItemType[ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem.Length];
                                        for (int z = 0; ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem.Length > z; z++)
                                        {
                                            treecatItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z] = new LineItemType();
                                            treecatItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].Any = ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].Any;
                                            treecatItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].Quantity = ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].Quantity;
                                            treecatItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].QuantitySpecified = ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].QuantitySpecified;

                                            if (ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].CountryOfOrigin != null)
                                            {
                                                treecatItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].CountryOfOrigin = ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].CountryOfOrigin;
                                            }

                                            if (ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].Description != null)
                                            {
                                                treecatItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].Description = ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].Description;
                                            }

                                            if (ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].ItemID != null)
                                            {
                                                treecatItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].ItemID = ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].ItemID;
                                            }

                                            if (ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].TransactionID != null)
                                            {
                                                treecatItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].TransactionID = ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentLineItem.LineItem[z].TransactionID;
                                            }
                                        }
                                    }
                                }

                                if (ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentTrackingNumber != null)
                                {
                                    treecatItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentTrackingNumber = ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShipmentTrackingNumber;
                                }

                                if (ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShippingCarrierUsed != null)
                                {
                                    treecatItem.ShippingDetails.ShipmentTrackingDetails[y].ShippingCarrierUsed = ebayItem.ShippingDetails.ShipmentTrackingDetails[y].ShippingCarrierUsed;
                                }
                            }
                        }

                        if (ebayItem.ShippingDetails.ShippingDiscountProfileID != null)
                        {
                            treecatItem.ShippingDetails.ShippingDiscountProfileID = ebayItem.ShippingDetails.ShippingDiscountProfileID;
                        }

                        if (ebayItem.ShippingDetails.ShippingRateErrorMessage != null)
                        {
                            treecatItem.ShippingDetails.ShippingRateErrorMessage = ebayItem.ShippingDetails.ShippingRateErrorMessage;
                        }

                        treecatItem.ShippingDetails.ShippingRateType = ebayItem.ShippingDetails.ShippingRateType;
                        treecatItem.ShippingDetails.ShippingRateTypeSpecified = ebayItem.ShippingDetails.ShippingRateTypeSpecified;

                        if (ebayItem.ShippingDetails.ShippingServiceOptions != null)
                        {
                            treecatItem.ShippingDetails.ShippingServiceOptions = new ShippingServiceOptionsType[ebayItem.ShippingDetails.ShippingServiceOptions.Length];
                            for (int z = 0; ebayItem.ShippingDetails.ShippingServiceOptions.Length > z; z++)
                            {
                                treecatItem.ShippingDetails.ShippingServiceOptions[z] = new ShippingServiceOptionsType();
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].Any = ebayItem.ShippingDetails.ShippingServiceOptions[z].Any;
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].ExpeditedService = ebayItem.ShippingDetails.ShippingServiceOptions[z].ExpeditedService;
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].ExpeditedServiceSpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].ExpeditedServiceSpecified;
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].FreeShipping = ebayItem.ShippingDetails.ShippingServiceOptions[z].FreeShipping;
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].FreeShippingSpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].FreeShippingSpecified;
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].LocalPickup = ebayItem.ShippingDetails.ShippingServiceOptions[z].LocalPickup;
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].LocalPickupSpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].LocalPickupSpecified;

                                if (ebayItem.ShippingDetails.ShippingServiceOptions[z].ImportCharge != null)
                                {
                                    treecatItem.ShippingDetails.ShippingServiceOptions[z].ImportCharge = new AmountType();
                                    treecatItem.ShippingDetails.ShippingServiceOptions[z].ImportCharge.currencyID = ebayItem.ShippingDetails.ShippingServiceOptions[z].ImportCharge.currencyID;
                                    treecatItem.ShippingDetails.ShippingServiceOptions[z].ImportCharge.Value = ebayItem.ShippingDetails.ShippingServiceOptions[z].ImportCharge.Value;
                                }

                                if (ebayItem.ShippingDetails.ShippingServiceOptions[z].LogisticPlanType != null)
                                {
                                    treecatItem.ShippingDetails.ShippingServiceOptions[z].LogisticPlanType = ebayItem.ShippingDetails.ShippingServiceOptions[z].LogisticPlanType;
                                }

                                if (ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingInsuranceCost != null)
                                {
                                    treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingInsuranceCost = new AmountType();
                                    treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingInsuranceCost.currencyID = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingInsuranceCost.currencyID;
                                    treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingInsuranceCost.Value = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingInsuranceCost.Value;
                                }

                                if (ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo != null)
                                {
                                    treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo = new ShippingPackageInfoType[ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo.Length];
                                    for (int y = 0; ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo.Length > y; y++)
                                    {
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y] = new ShippingPackageInfoType();
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ActualDeliveryTime = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ActualDeliveryTime;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ActualDeliveryTimeSpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ActualDeliveryTimeSpecified;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].Any = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].Any;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].EstimatedDeliveryTimeMax = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].EstimatedDeliveryTimeMax;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].EstimatedDeliveryTimeMaxSpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].EstimatedDeliveryTimeMaxSpecified;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].EstimatedDeliveryTimeMin = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].EstimatedDeliveryTimeMin;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].EstimatedDeliveryTimeMinSpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].EstimatedDeliveryTimeMinSpecified;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].HandleByTime = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].HandleByTime;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].HandleByTimeSpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].HandleByTimeSpecified;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].MaxNativeEstimatedDeliveryTime = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].MaxNativeEstimatedDeliveryTime;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].MaxNativeEstimatedDeliveryTimeSpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].MaxNativeEstimatedDeliveryTimeSpecified;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].MinNativeEstimatedDeliveryTime = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].MinNativeEstimatedDeliveryTime;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].MinNativeEstimatedDeliveryTimeSpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].MinNativeEstimatedDeliveryTimeSpecified;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ScheduledDeliveryTimeMax = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ScheduledDeliveryTimeMax;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ScheduledDeliveryTimeMaxSpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ScheduledDeliveryTimeMaxSpecified;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ScheduledDeliveryTimeMin = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ScheduledDeliveryTimeMin;
                                        treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ScheduledDeliveryTimeMinSpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ScheduledDeliveryTimeMinSpecified;

                                        if (ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ShippingTrackingEvent != null)
                                        {
                                            treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ShippingTrackingEvent = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].ShippingTrackingEvent;
                                        }

                                        if (ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].StoreID != null)
                                        {
                                            treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].StoreID = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingPackageInfo[y].StoreID;
                                        }
                                    }
                                }

                                if (ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceAdditionalCost != null)
                                {
                                    treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceAdditionalCost = new AmountType();
                                    treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceAdditionalCost.currencyID = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceAdditionalCost.currencyID;
                                    treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceAdditionalCost.Value = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceAdditionalCost.Value;
                                }

                                if (ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceCost != null)
                                {
                                    treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceCost = new AmountType();
                                    treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceCost.currencyID = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceCost.currencyID;
                                    treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceCost.Value = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceCost.Value;
                                }

                                treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingService = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingService;
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceCutOffTime = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceCutOffTime;
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceCutOffTimeSpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingServiceCutOffTimeSpecified;
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingServicePriority = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingServicePriority;
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingServicePrioritySpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingServicePrioritySpecified;
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingTimeMax = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingTimeMax;
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingTimeMaxSpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingTimeMaxSpecified;
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingTimeMin = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingTimeMin;
                                treecatItem.ShippingDetails.ShippingServiceOptions[z].ShippingTimeMinSpecified = ebayItem.ShippingDetails.ShippingServiceOptions[z].ShippingTimeMinSpecified;
                            }
                        }

                        treecatItem.ShippingDetails.ShippingType = ebayItem.ShippingDetails.ShippingType;
                        treecatItem.ShippingDetails.ShippingTypeSpecified = ebayItem.ShippingDetails.ShippingTypeSpecified;

                        if (ebayItem.ShippingDetails.ShippingServiceUsed != null)
                        {
                            treecatItem.ShippingDetails.ShippingServiceUsed = ebayItem.ShippingDetails.ShippingServiceUsed;
                        }

                        if (ebayItem.ShippingDetails.TaxTable != null)
                        {
                            treecatItem.ShippingDetails.TaxTable = new TaxJurisdictionType[ebayItem.ShippingDetails.TaxTable.Length];
                            for (int y = 0; ebayItem.ShippingDetails.TaxTable.Length > y; y++)
                            {
                                treecatItem.ShippingDetails.TaxTable[y] = new TaxJurisdictionType();
                                treecatItem.ShippingDetails.TaxTable[y].Any = ebayItem.ShippingDetails.TaxTable[y].Any;
                                treecatItem.ShippingDetails.TaxTable[y].SalesTaxPercent = ebayItem.ShippingDetails.TaxTable[y].SalesTaxPercent;
                                treecatItem.ShippingDetails.TaxTable[y].SalesTaxPercentSpecified = ebayItem.ShippingDetails.TaxTable[y].SalesTaxPercentSpecified;
                                treecatItem.ShippingDetails.TaxTable[y].ShippingIncludedInTax = ebayItem.ShippingDetails.TaxTable[y].ShippingIncludedInTax;
                                treecatItem.ShippingDetails.TaxTable[y].ShippingIncludedInTaxSpecified = ebayItem.ShippingDetails.TaxTable[y].ShippingIncludedInTaxSpecified;
                                treecatItem.ShippingDetails.TaxTable[y].UpdateTime = ebayItem.ShippingDetails.TaxTable[y].UpdateTime;
                                treecatItem.ShippingDetails.TaxTable[y].UpdateTimeSpecified = ebayItem.ShippingDetails.TaxTable[y].UpdateTimeSpecified;

                                if (ebayItem.ShippingDetails.TaxTable[y].DetailVersion != null)
                                {
                                    treecatItem.ShippingDetails.TaxTable[y].DetailVersion = ebayItem.ShippingDetails.TaxTable[y].DetailVersion;
                                }

                                if (ebayItem.ShippingDetails.TaxTable[y].JurisdictionID != null)
                                {
                                    treecatItem.ShippingDetails.TaxTable[y].JurisdictionID = ebayItem.ShippingDetails.TaxTable[y].JurisdictionID;
                                }

                                if (ebayItem.ShippingDetails.TaxTable[y].JurisdictionName != null)
                                {
                                    treecatItem.ShippingDetails.TaxTable[y].JurisdictionName = ebayItem.ShippingDetails.TaxTable[y].JurisdictionName;
                                }
                            }
                        }

                        treecatItem.ShippingDetails.ThirdPartyCheckout = ebayItem.ShippingDetails.ThirdPartyCheckout;
                        treecatItem.ShippingDetails.ThirdPartyCheckoutSpecified = ebayItem.ShippingDetails.ThirdPartyCheckoutSpecified;
                    }

                    if (ebayItem.ShippingOverride != null)
                    {
                        treecatItem.ShippingOverride = new ShippingOverrideType();
                        treecatItem.ShippingOverride.DispatchTimeMaxOverride = ebayItem.ShippingOverride.DispatchTimeMaxOverride;
                        treecatItem.ShippingOverride.DispatchTimeMaxOverrideSpecified = ebayItem.ShippingOverride.DispatchTimeMaxOverrideSpecified;
                        treecatItem.ShippingOverride.ShippingServiceCostOverrideList.Any = ebayItem.ShippingOverride.ShippingServiceCostOverrideList.Any;

                        if (ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride != null)
                        {
                            treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride = new ShippingServiceCostOverrideType[ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride.Length];
                            for (int y = 0; ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride.Length > y; y++)
                            {
                                treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y] = new ShippingServiceCostOverrideType();
                                if (ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceAdditionalCost != null)
                                {
                                    treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceAdditionalCost = new AmountType();
                                    treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceAdditionalCost.currencyID = ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceAdditionalCost.currencyID;
                                    treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceAdditionalCost.Value = ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceAdditionalCost.Value;
                                }

                                if (ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceCost != null)
                                {
                                    treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceCost = new AmountType();
                                    treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceCost.currencyID = ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceCost.currencyID;
                                    treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceCost.Value = ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceCost.Value;
                                }

                                if (ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingSurcharge != null)
                                {
                                    treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingSurcharge = new AmountType();
                                    treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingSurcharge.currencyID = ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingSurcharge.currencyID;
                                    treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingSurcharge.Value = ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingSurcharge.Value;
                                }

                                treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].Any = ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].Any;
                                treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServicePriority = ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServicePriority;
                                treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServicePrioritySpecified = ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServicePrioritySpecified;
                                treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceType = ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceType;
                                treecatItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceTypeSpecified = ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceTypeSpecified;
                            }
                        }
                    }

                    if (ebayItem.ShippingPackageDetails != null)
                    {
                        treecatItem.ShippingPackageDetails = new ShipPackageDetailsType();
                        treecatItem.ShippingPackageDetails.Any = ebayItem.ShippingPackageDetails.Any;
                        treecatItem.ShippingPackageDetails.MeasurementUnit = ebayItem.ShippingPackageDetails.MeasurementUnit;
                        treecatItem.ShippingPackageDetails.MeasurementUnitSpecified = ebayItem.ShippingPackageDetails.MeasurementUnitSpecified;
                        treecatItem.ShippingPackageDetails.ShippingIrregular = ebayItem.ShippingPackageDetails.ShippingIrregular;
                        treecatItem.ShippingPackageDetails.ShippingIrregularSpecified = ebayItem.ShippingPackageDetails.ShippingIrregularSpecified;
                        treecatItem.ShippingPackageDetails.ShippingPackage = ebayItem.ShippingPackageDetails.ShippingPackage;
                        treecatItem.ShippingPackageDetails.ShippingPackageSpecified = ebayItem.ShippingPackageDetails.ShippingPackageSpecified;

                        if (ebayItem.ShippingPackageDetails.PackageDepth != null)
                        {
                            treecatItem.ShippingPackageDetails.PackageDepth = new MeasureType();
                            treecatItem.ShippingPackageDetails.PackageDepth.measurementSystem = ebayItem.ShippingPackageDetails.PackageDepth.measurementSystem;
                            treecatItem.ShippingPackageDetails.PackageDepth.measurementSystemSpecified = ebayItem.ShippingPackageDetails.PackageDepth.measurementSystemSpecified;
                            treecatItem.ShippingPackageDetails.PackageDepth.Value = ebayItem.ShippingPackageDetails.PackageDepth.Value;

                            if (ebayItem.ShippingPackageDetails.PackageDepth.unit != null)
                            {
                                treecatItem.ShippingPackageDetails.PackageDepth.unit = ebayItem.ShippingPackageDetails.PackageDepth.unit;
                            }
                        }

                        if (ebayItem.ShippingPackageDetails.PackageLength != null)
                        {
                            treecatItem.ShippingPackageDetails.PackageLength = new MeasureType();
                            treecatItem.ShippingPackageDetails.PackageLength.measurementSystem = ebayItem.ShippingPackageDetails.PackageLength.measurementSystem;
                            treecatItem.ShippingPackageDetails.PackageLength.measurementSystemSpecified = ebayItem.ShippingPackageDetails.PackageLength.measurementSystemSpecified;
                            treecatItem.ShippingPackageDetails.PackageLength.Value = ebayItem.ShippingPackageDetails.PackageLength.Value;

                            if (ebayItem.ShippingPackageDetails.PackageLength.unit != null)
                            {
                                treecatItem.ShippingPackageDetails.PackageLength.unit = ebayItem.ShippingPackageDetails.PackageLength.unit;
                            }
                        }

                        if (ebayItem.ShippingPackageDetails.PackageWidth != null)
                        {
                            treecatItem.ShippingPackageDetails.PackageWidth = new MeasureType();
                            treecatItem.ShippingPackageDetails.PackageWidth.measurementSystem = ebayItem.ShippingPackageDetails.PackageWidth.measurementSystem;
                            treecatItem.ShippingPackageDetails.PackageWidth.measurementSystemSpecified = ebayItem.ShippingPackageDetails.PackageWidth.measurementSystemSpecified;
                            treecatItem.ShippingPackageDetails.PackageWidth.Value = ebayItem.ShippingPackageDetails.PackageWidth.Value;

                            if (ebayItem.ShippingPackageDetails.PackageWidth.unit != null)
                            {
                                treecatItem.ShippingPackageDetails.PackageWidth.unit = ebayItem.ShippingPackageDetails.PackageWidth.unit;
                            }
                        }

                        if (ebayItem.ShippingPackageDetails.WeightMajor != null)
                        {
                            treecatItem.ShippingPackageDetails.WeightMajor = new MeasureType();
                            treecatItem.ShippingPackageDetails.WeightMajor.measurementSystem = ebayItem.ShippingPackageDetails.WeightMajor.measurementSystem;
                            treecatItem.ShippingPackageDetails.WeightMajor.measurementSystemSpecified = ebayItem.ShippingPackageDetails.WeightMajor.measurementSystemSpecified;
                            treecatItem.ShippingPackageDetails.WeightMajor.Value = ebayItem.ShippingPackageDetails.WeightMajor.Value;

                            if (ebayItem.ShippingPackageDetails.WeightMajor.unit != null)
                            {
                                treecatItem.ShippingPackageDetails.WeightMajor.unit = ebayItem.ShippingPackageDetails.WeightMajor.unit;
                            }
                        }

                        if (ebayItem.ShippingPackageDetails.WeightMinor != null)
                        {
                            treecatItem.ShippingPackageDetails.WeightMinor = new MeasureType();
                            treecatItem.ShippingPackageDetails.WeightMinor.measurementSystem = ebayItem.ShippingPackageDetails.WeightMinor.measurementSystem;
                            treecatItem.ShippingPackageDetails.WeightMinor.measurementSystemSpecified = ebayItem.ShippingPackageDetails.WeightMinor.measurementSystemSpecified;
                            treecatItem.ShippingPackageDetails.WeightMinor.Value = ebayItem.ShippingPackageDetails.WeightMinor.Value;

                            if (ebayItem.ShippingPackageDetails.WeightMinor.unit != null)
                            {
                                treecatItem.ShippingPackageDetails.WeightMinor.unit = ebayItem.ShippingPackageDetails.WeightMinor.unit;
                            }
                        }
                    }

                    if (ebayItem.ShippingServiceCostOverrideList != null)
                    {
                        treecatItem.ShippingServiceCostOverrideList = new ShippingServiceCostOverrideListType();
                        treecatItem.ShippingServiceCostOverrideList.Any = ebayItem.ShippingServiceCostOverrideList.Any;

                        if (ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride != null)
                        {
                            treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride = new ShippingServiceCostOverrideType[ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride.Length];
                            for (int y = 0; ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride.Length > y; y++)
                            {
                                treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y] = new ShippingServiceCostOverrideType();
                                treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].Any = ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].Any;
                                treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServicePriority = ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServicePriority;
                                treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServicePrioritySpecified = ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServicePrioritySpecified;
                                treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceType = ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceType;
                                treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceTypeSpecified = ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceTypeSpecified;

                                if (ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceAdditionalCost != null)
                                {
                                    treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceAdditionalCost = new AmountType();
                                    treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceAdditionalCost.currencyID = ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceAdditionalCost.currencyID;
                                    treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceAdditionalCost.Value = ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceAdditionalCost.Value;
                                }

                                if (ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceCost != null)
                                {
                                    treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceCost = new AmountType();
                                    treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceCost.currencyID = ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceCost.currencyID;
                                    treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceCost.Value = ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingServiceCost.Value;
                                }

                                if (ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingSurcharge != null)
                                {
                                    treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingSurcharge = new AmountType();
                                    treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingSurcharge.currencyID = ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingSurcharge.currencyID;
                                    treecatItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingSurcharge.Value = ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride[y].ShippingSurcharge.Value;
                                }
                            }
                        }
                    }

                    treecatItem.Site = ebayItem.Site;
                    treecatItem.SiteID = ebayItem.SiteId;
                    treecatItem.SiteIDSpecified = ebayItem.SiteIdSpecified;
                    treecatItem.SiteSpecified = ebayItem.SiteSpecified;

                    if (ebayItem.ShipToLocations != null)
                    {
                        treecatItem.ShipToLocations = ebayItem.ShipToLocations;
                    }

                    if (ebayItem.StartPrice != null)
                    {
                        treecatItem.StartPrice = new AmountType();
                        treecatItem.StartPrice.currencyID = ebayItem.StartPrice.currencyID;
                        treecatItem.StartPrice.Value = ebayItem.StartPrice.Value;
                    }

                    if (ebayItem.Storefront != null)
                    {
                        treecatItem.StoreFront = new StorefrontType();
                        treecatItem.StoreFront.Any = ebayItem.Storefront.Any;
                        treecatItem.StoreFront.StoreCategory2ID = ebayItem.Storefront.StoreCategory2ID;
                        treecatItem.StoreFront.StoreCategoryID = ebayItem.Storefront.StoreCategoryID;

                        if (ebayItem.Storefront.StoreCategory2Name != null)
                        {
                            treecatItem.StoreFront.StoreCategory2Name = ebayItem.Storefront.StoreCategory2Name;
                        }

                        if (ebayItem.Storefront.StoreCategoryName != null)
                        {
                            treecatItem.StoreFront.StoreCategoryName = ebayItem.Storefront.StoreCategoryName;
                        }

                        if (ebayItem.Storefront.StoreName != null)
                        {
                            treecatItem.StoreFront.StoreName = ebayItem.Storefront.StoreName;
                        }

                        if (ebayItem.Storefront.StoreURL != null)
                        {
                            treecatItem.StoreFront.StoreURL = ebayItem.Storefront.StoreURL;
                        }
                    }


                    if (ebayItem.SubTitle != null)
                    {
                        treecatItem.SubTitle = ebayItem.SubTitle;
                    }

                    if (ebayItem.TaxCategory != null)
                    {
                        treecatItem.TaxCategory = ebayItem.TaxCategory;
                    }

                    if (ebayItem.TimeLeft != null)
                    {
                        treecatItem.TimeLeft = ebayItem.TimeLeft;
                    }

                    if (ebayItem.Title != null)
                    {
                        treecatItem.Title = ebayItem.Title;
                    }

                    if (ebayItem.UnitInfo != null)
                    {
                        treecatItem.UnitInfo = new UnitInfoType();
                        treecatItem.UnitInfo.Any = ebayItem.UnitInfo.Any;
                        treecatItem.UnitInfo.UnitQuantity = ebayItem.UnitInfo.UnitQuantity;
                        treecatItem.UnitInfo.UnitQuantitySpecified = ebayItem.UnitInfo.UnitQuantitySpecified;

                        if (ebayItem.UnitInfo.UnitType != null)
                        {
                            treecatItem.UnitInfo.UnitType = ebayItem.UnitInfo.UnitType;
                        }
                    }

                    treecatItem.TopRatedListing = ebayItem.TopRatedListing;
                    treecatItem.TopRatedListingSpecified = ebayItem.TopRatedListingSpecified;
                    treecatItem.TotalQuestionCount = ebayItem.TotalQuestionCount;
                    treecatItem.TotalQuestionCountSpecified = ebayItem.TotalQuestionCountSpecified;
                    treecatItem.UpdateReturnPolicy = ebayItem.UpdateReturnPolicy;
                    treecatItem.UpdateReturnPolicySpecified = ebayItem.UpdateReturnPolicySpecified;
                    treecatItem.UpdateSellerInfo = ebayItem.UpdateSellerInfo;
                    treecatItem.UpdateSellerInfoSpecified = ebayItem.UpdateSellerInfoSpecified;
                    treecatItem.UseTaxTable = ebayItem.UseTaxTable;
                    treecatItem.UseTaxTableSpecified = ebayItem.UseTaxTableSpecified;

                    if (ebayItem.UUID != null)
                    {
                        treecatItem.UUID = ebayItem.UUID;
                    }

                    if (ebayItem.Variations != null)
                    {
                        treecatItem.Variations = new VariationsType();
                        treecatItem.Variations.Any = ebayItem.Variations.Any;

                        if (ebayItem.Variations.ModifyNameList != null)
                        {
                            treecatItem.Variations.ModifyNameList = new ModifyNameType[ebayItem.Variations.ModifyNameList.Length];
                            for (int y = 0; ebayItem.Variations.ModifyNameList.Length > y; y++)
                            {
                                treecatItem.Variations.ModifyNameList[y] = new ModifyNameType();
                                treecatItem.Variations.ModifyNameList[y].Any = ebayItem.Variations.ModifyNameList[y].Any;

                                if (ebayItem.Variations.ModifyNameList[y].Name != null)
                                {
                                    treecatItem.Variations.ModifyNameList[y].Name = ebayItem.Variations.ModifyNameList[y].Name;
                                }

                                if (ebayItem.Variations.ModifyNameList[y].NewName != null)
                                {
                                    treecatItem.Variations.ModifyNameList[y].NewName = ebayItem.Variations.ModifyNameList[y].NewName;
                                }
                            }
                        }

                        if (ebayItem.Variations.Pictures != null)
                        {
                            treecatItem.Variations.Pictures = new PicturesType[ebayItem.Variations.Pictures.Length];
                            for (int y = 0; ebayItem.Variations.Pictures.Length > y; y++)
                            {
                                treecatItem.Variations.Pictures[y] = new PicturesType();
                                treecatItem.Variations.Pictures[y].Any = ebayItem.Variations.Pictures[y].Any;

                                if (ebayItem.Variations.Pictures[y].VariationSpecificName != null)
                                {
                                    treecatItem.Variations.Pictures[y].VariationSpecificName = ebayItem.Variations.Pictures[y].VariationSpecificName;
                                }

                                if (ebayItem.Variations.Pictures[y].VariationSpecificPictureSet != null)
                                {
                                    treecatItem.Variations.Pictures[y].VariationSpecificPictureSet = new VariationSpecificPictureSetType[ebayItem.Variations.Pictures[y].VariationSpecificPictureSet.Length];
                                    for (int z = 0; ebayItem.Variations.Pictures[y].VariationSpecificPictureSet.Length > z; z++)
                                    {
                                        treecatItem.Variations.Pictures[y].VariationSpecificPictureSet[z] = new VariationSpecificPictureSetType();
                                        treecatItem.Variations.Pictures[y].VariationSpecificPictureSet[z].Any = ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].Any;
                                        treecatItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.Any = ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.Any;

                                        if (ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExternalPictureURL != null)
                                        {
                                            treecatItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExternalPictureURL = ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExternalPictureURL;
                                        }

                                        if (ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].GalleryURL != null)
                                        {
                                            treecatItem.Variations.Pictures[y].VariationSpecificPictureSet[z].GalleryURL = ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].GalleryURL;
                                        }

                                        if (ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].PictureURL != null)
                                        {
                                            treecatItem.Variations.Pictures[y].VariationSpecificPictureSet[z].PictureURL = ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].PictureURL;
                                        }

                                        if (ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].VariationSpecificValue != null)
                                        {
                                            treecatItem.Variations.Pictures[y].VariationSpecificPictureSet[z].VariationSpecificValue = ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].VariationSpecificValue;
                                        }

                                        if (ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.PictureURLs != null)
                                        {
                                            treecatItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.PictureURLs = new PictureURLsType[ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.PictureURLs.Length];
                                            for (int x = 0; ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.PictureURLs.Length > x; x++)
                                            {
                                                treecatItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.PictureURLs[x] = new PictureURLsType();
                                                treecatItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.PictureURLs[x].Any = ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.PictureURLs[x].Any;

                                                if (ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.PictureURLs[x].eBayPictureURL != null)
                                                {
                                                    treecatItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.PictureURLs[x].eBayPictureURL = ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.PictureURLs[x].eBayPictureURL;
                                                }

                                                if (ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.PictureURLs[x].ExternalPictureURL != null)
                                                {
                                                    treecatItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.PictureURLs[x].ExternalPictureURL = ebayItem.Variations.Pictures[y].VariationSpecificPictureSet[z].ExtendedPictureDetails.PictureURLs[x].ExternalPictureURL;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (ebayItem.Variations.Variation != null)
                        {
                            treecatItem.Variations.Variation = new VariationType[ebayItem.Variations.Variation.Length];
                            for (int y = 0; ebayItem.Variations.Variation.Length > y; y++)
                            {
                                treecatItem.Variations.Variation[y] = new VariationType();
                                treecatItem.Variations.Variation[y].Any = ebayItem.Variations.Variation[y].Any;
                                treecatItem.Variations.Variation[y].Delete = ebayItem.Variations.Variation[y].Delete;

                                if (ebayItem.Variations.Variation[y].DiscountPriceInfo != null)
                                {
                                    treecatItem.Variations.Variation[y].DiscountPriceInfo = new DiscountPriceInfoType();
                                    if (ebayItem.Variations.Variation[y].DiscountPriceInfo.MadeForOutletComparisonPrice != null)
                                    {
                                        treecatItem.Variations.Variation[y].DiscountPriceInfo.MadeForOutletComparisonPrice = new AmountType();
                                        treecatItem.Variations.Variation[y].DiscountPriceInfo.MadeForOutletComparisonPrice.currencyID = ebayItem.Variations.Variation[y].DiscountPriceInfo.MadeForOutletComparisonPrice.currencyID;
                                        treecatItem.Variations.Variation[y].DiscountPriceInfo.MadeForOutletComparisonPrice.Value = ebayItem.Variations.Variation[y].DiscountPriceInfo.MadeForOutletComparisonPrice.Value;
                                    }

                                    if (ebayItem.Variations.Variation[y].DiscountPriceInfo.MinimumAdvertisedPrice != null)
                                    {
                                        treecatItem.Variations.Variation[y].DiscountPriceInfo.MinimumAdvertisedPrice = new AmountType();
                                        treecatItem.Variations.Variation[y].DiscountPriceInfo.MinimumAdvertisedPrice.currencyID = ebayItem.Variations.Variation[y].DiscountPriceInfo.MinimumAdvertisedPrice.currencyID;
                                        treecatItem.Variations.Variation[y].DiscountPriceInfo.MinimumAdvertisedPrice.Value = ebayItem.Variations.Variation[y].DiscountPriceInfo.MinimumAdvertisedPrice.Value;
                                    }

                                    if (ebayItem.Variations.Variation[y].DiscountPriceInfo.OriginalRetailPrice != null)
                                    {
                                        treecatItem.Variations.Variation[y].DiscountPriceInfo.OriginalRetailPrice = new AmountType();
                                        treecatItem.Variations.Variation[y].DiscountPriceInfo.OriginalRetailPrice.currencyID = ebayItem.Variations.Variation[y].DiscountPriceInfo.OriginalRetailPrice.currencyID;
                                        treecatItem.Variations.Variation[y].DiscountPriceInfo.OriginalRetailPrice.Value = ebayItem.Variations.Variation[y].DiscountPriceInfo.OriginalRetailPrice.Value;
                                    }

                                    treecatItem.Variations.Variation[y].DiscountPriceInfo.Any = ebayItem.Variations.Variation[y].DiscountPriceInfo.Any;
                                    treecatItem.Variations.Variation[y].DiscountPriceInfo.MinimumAdvertisedPriceExposure = ebayItem.Variations.Variation[y].DiscountPriceInfo.MinimumAdvertisedPriceExposure;
                                    treecatItem.Variations.Variation[y].DiscountPriceInfo.MinimumAdvertisedPriceExposureSpecified = ebayItem.Variations.Variation[y].DiscountPriceInfo.MinimumAdvertisedPriceExposureSpecified;
                                    treecatItem.Variations.Variation[y].DiscountPriceInfo.PricingTreatment = ebayItem.Variations.Variation[y].DiscountPriceInfo.PricingTreatment;
                                    treecatItem.Variations.Variation[y].DiscountPriceInfo.PricingTreatmentSpecified = ebayItem.Variations.Variation[y].DiscountPriceInfo.PricingTreatmentSpecified;
                                    treecatItem.Variations.Variation[y].DiscountPriceInfo.SoldOffeBay = ebayItem.Variations.Variation[y].DiscountPriceInfo.SoldOffeBay;
                                    treecatItem.Variations.Variation[y].DiscountPriceInfo.SoldOneBay = ebayItem.Variations.Variation[y].DiscountPriceInfo.SoldOneBay;
                                }

                                if (ebayItem.Variations.Variation[y].PrivateNotes != null)
                                {
                                    treecatItem.Variations.Variation[y].PrivateNotes = ebayItem.Variations.Variation[y].PrivateNotes;
                                }

                                treecatItem.Variations.Variation[y].Quantity = ebayItem.Variations.Variation[y].Quantity;
                                treecatItem.Variations.Variation[y].QuantitySpecified = ebayItem.Variations.Variation[y].QuantitySpecified;

                                if (ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus != null)
                                {
                                    treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus = new SellingManagerProductInventoryStatusType();
                                    treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus.Any = ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus.Any;

                                    if (ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus.AverageSellingPrice != null)
                                    {
                                        treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus.AverageSellingPrice = new AmountType();
                                        treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus.AverageSellingPrice.currencyID = ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus.AverageSellingPrice.currencyID;
                                        treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus.AverageSellingPrice.Value = ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus.AverageSellingPrice.Value;
                                    }

                                    treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantityActive = ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantityActive;
                                    treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantityActiveSpecified = ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantityActiveSpecified;
                                    treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantityScheduled = ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantityScheduled;
                                    treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantityScheduledSpecified = ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantityScheduledSpecified;
                                    treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantitySold = ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantitySold;
                                    treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantitySoldSpecified = ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantitySoldSpecified;
                                    treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantityUnsold = ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantityUnsold;
                                    treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantityUnsoldSpecified = ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus.QuantityUnsoldSpecified;
                                    treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus.SuccessPercent = ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus.SuccessPercent;
                                    treecatItem.Variations.Variation[y].SellingManagerProductInventoryStatus.SuccessPercentSpecified = ebayItem.Variations.Variation[y].SellingManagerProductInventoryStatus.SuccessPercentSpecified;
                                }

                                if (ebayItem.Variations.Variation[y].SellingStatus != null)
                                {
                                    treecatItem.Variations.Variation[y].SellingStatus = new SellingStatusType();
                                    treecatItem.Variations.Variation[y].SellingStatus.AdminEnded = ebayItem.Variations.Variation[y].SellingStatus.AdminEnded;
                                    treecatItem.Variations.Variation[y].SellingStatus.AdminEndedSpecified = ebayItem.Variations.Variation[y].SellingStatus.AdminEndedSpecified;
                                    treecatItem.Variations.Variation[y].SellingStatus.Any = ebayItem.Variations.Variation[y].SellingStatus.Any;
                                    treecatItem.Variations.Variation[y].SellingStatus.BidCount = ebayItem.Variations.Variation[y].SellingStatus.BidCount;
                                    treecatItem.Variations.Variation[y].SellingStatus.BidCountSpecified = ebayItem.Variations.Variation[y].SellingStatus.BidCountSpecified;
                                    treecatItem.Variations.Variation[y].SellingStatus.BidderCount = ebayItem.Variations.Variation[y].SellingStatus.BidderCount;
                                    treecatItem.Variations.Variation[y].SellingStatus.BidderCountSpecified = ebayItem.Variations.Variation[y].SellingStatus.BidderCountSpecified;

                                    if (ebayItem.Variations.Variation[y].SellingStatus.BidIncrement != null)
                                    {
                                        treecatItem.Variations.Variation[y].SellingStatus.BidIncrement = new AmountType();
                                        treecatItem.Variations.Variation[y].SellingStatus.BidIncrement.currencyID = ebayItem.Variations.Variation[y].SellingStatus.BidIncrement.currencyID;
                                        treecatItem.Variations.Variation[y].SellingStatus.BidIncrement.Value = ebayItem.Variations.Variation[y].SellingStatus.BidIncrement.Value;
                                    }

                                    if (ebayItem.Variations.Variation[y].SellingStatus.ConvertedCurrentPrice != null)
                                    {
                                        treecatItem.Variations.Variation[y].SellingStatus.ConvertedCurrentPrice = new AmountType();
                                        treecatItem.Variations.Variation[y].SellingStatus.ConvertedCurrentPrice.currencyID = ebayItem.Variations.Variation[y].SellingStatus.ConvertedCurrentPrice.currencyID;
                                        treecatItem.Variations.Variation[y].SellingStatus.ConvertedCurrentPrice.Value = ebayItem.Variations.Variation[y].SellingStatus.ConvertedCurrentPrice.Value;
                                    }

                                    if (ebayItem.Variations.Variation[y].SellingStatus.CurrentPrice != null)
                                    {
                                        treecatItem.Variations.Variation[y].SellingStatus.CurrentPrice = new AmountType();
                                        treecatItem.Variations.Variation[y].SellingStatus.CurrentPrice.currencyID = ebayItem.Variations.Variation[y].SellingStatus.CurrentPrice.currencyID;
                                        treecatItem.Variations.Variation[y].SellingStatus.CurrentPrice.Value = ebayItem.Variations.Variation[y].SellingStatus.CurrentPrice.Value;
                                    }

                                    if (ebayItem.Variations.Variation[y].SellingStatus.FinalValueFee != null)
                                    {
                                        treecatItem.Variations.Variation[y].SellingStatus.FinalValueFee = new AmountType();
                                        treecatItem.Variations.Variation[y].SellingStatus.FinalValueFee.currencyID = ebayItem.Variations.Variation[y].SellingStatus.FinalValueFee.currencyID;
                                        treecatItem.Variations.Variation[y].SellingStatus.FinalValueFee.Value = ebayItem.Variations.Variation[y].SellingStatus.FinalValueFee.Value;
                                    }
                                    if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder != null)
                                    {
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder = new UserType();
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.AboutMePage = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.AboutMePage;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.AboutMePageSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.AboutMePageSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Any;

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary = new BiddingSummaryType();
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.Any;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidActivityWithSeller = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidActivityWithSeller;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidActivityWithSellerSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidActivityWithSellerSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidRetractions = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidRetractions;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidRetractionsSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidRetractionsSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidsToUniqueCategories = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidsToUniqueCategories;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidsToUniqueCategoriesSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidsToUniqueCategoriesSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidsToUniqueSellers = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidsToUniqueSellers;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidsToUniqueSellersSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.BidsToUniqueSellersSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.SummaryDaysSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.SummaryDaysSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.TotalBids = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.TotalBids;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.TotalBidsSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.TotalBidsSpecified;

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails = new ItemBidDetailsType[ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails.Length];
                                                for (int z = 0; ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails.Length > z; z++)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z] = new ItemBidDetailsType();
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].Any;
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].BidCount = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].BidCount;
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].BidCountSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].BidCountSpecified;
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].LastBidTime = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].LastBidTime;
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].LastBidTimeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].LastBidTimeSpecified;

                                                    if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].CategoryID != null)
                                                    {
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].CategoryID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].CategoryID;
                                                    }

                                                    if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].ItemID != null)
                                                    {
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].ItemID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].ItemID;
                                                    }

                                                    if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].SellerID != null)
                                                    {
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].SellerID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BiddingSummary.ItemBidDetails[z].SellerID;
                                                    }
                                                }
                                            }
                                        }

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BillingEmail != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BillingEmail = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BillingEmail;
                                        }

                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BusinessRole = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BusinessRole;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BusinessRoleSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BusinessRoleSpecified;

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo = new BuyerType();
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.Any;

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier = new TaxIdentifierType[ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier.Length];
                                                for (int z = 0; ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier.Length > z; z++)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z] = new TaxIdentifierType();
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Any;

                                                    if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Attribute != null)
                                                    {
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Attribute = new TaxIdentifierAttributeType[ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Attribute.Length];
                                                        for (int x = 0; ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Attribute.Length > x; x++)
                                                        {
                                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Attribute[x] = new TaxIdentifierAttributeType();
                                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Attribute[x].name = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Attribute[x].name;
                                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Attribute[x].nameSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Attribute[x].nameSpecified;

                                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Attribute[x].Value != null)
                                                            {
                                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Attribute[x].Value = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Attribute[x].Value;
                                                            }
                                                        }
                                                    }

                                                    if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].ID != null)
                                                    {
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].ID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].ID;
                                                    }

                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Type = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].Type;
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].TypeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier[z].TypeSpecified;
                                                }
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute = new AddressAttributeType[ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute.Length];
                                                for (int z = 0; ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute.Length > z; z++)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[z] = new AddressAttributeType();
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[z].type = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[z].type;
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[z].typeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[z].typeSpecified;

                                                    if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[z].Value != null)
                                                    {
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[z].Value = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute[z].Value;
                                                    }
                                                }
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress = new AddressType();
                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressID != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressID;
                                                }

                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressOwner = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressOwner;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressOwnerSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressOwnerSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressRecordType = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressRecordType;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressRecordTypeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressRecordTypeSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressStatus = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressStatus;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressStatusSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressStatusSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressUsage = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressUsage;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressUsageSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressUsageSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Any;

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CityName != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CityName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CityName;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CompanyName != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CompanyName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CompanyName;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CountryName != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CountryName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CountryName;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.County != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.County = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.County;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.ExternalAddressID != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.ExternalAddressID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.ExternalAddressID;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.FirstName != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.FirstName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.FirstName;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalName != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalName;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalStateAndCity != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalStateAndCity = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalStateAndCity;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalStreet != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalStreet = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalStreet;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.LastName != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.LastName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.LastName;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Name != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Name = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Name;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Phone != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Phone = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Phone;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Phone2 != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Phone2 = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Phone2;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneAreaOrCityCode != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneAreaOrCityCode = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneAreaOrCityCode;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneCountryPrefix != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneCountryPrefix = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneCountryPrefix;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneLocalNumber != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneLocalNumber = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneLocalNumber;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PostalCode != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PostalCode = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PostalCode;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.ReferenceID != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.ReferenceID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.ReferenceID;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.StateOrProvince != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.StateOrProvince = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.StateOrProvince;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street1 != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street1 = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street1;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street2 != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street2 = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street2;
                                                }
                                            }
                                        }

                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.CharityAffiliations.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.CharityAffiliations.Any;

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.CharityAffiliations.CharityID != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.CharityAffiliations.CharityID = new CharityIDType[ebayItem.Variations.Variation[y].SellingStatus.HighBidder.CharityAffiliations.CharityID.Length];
                                            for (int z = 0; ebayItem.Variations.Variation[y].SellingStatus.HighBidder.CharityAffiliations.CharityID.Length > z; z++)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.CharityAffiliations.CharityID[z] = new CharityIDType();
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.CharityAffiliations.CharityID[z].type = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.CharityAffiliations.CharityID[z].type;

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.CharityAffiliations.CharityID[z].Value != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.CharityAffiliations.CharityID[z].Value = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.CharityAffiliations.CharityID[z].Value;
                                                }
                                            }
                                        }

                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.eBayGoodStanding = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.eBayGoodStanding;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.eBayGoodStandingSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.eBayGoodStandingSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.eBayWikiReadOnly = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.eBayWikiReadOnly;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.eBayWikiReadOnlySpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.eBayWikiReadOnlySpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.EnterpriseSeller = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.EnterpriseSeller;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.EnterpriseSellerSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.EnterpriseSellerSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.FeedbackPrivate = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.FeedbackPrivate;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.FeedbackPrivateSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.FeedbackPrivateSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.FeedbackRatingStar = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.FeedbackRatingStar;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.FeedbackRatingStarSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.FeedbackRatingStarSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.FeedbackScore = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.FeedbackScore;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.FeedbackScoreSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.FeedbackScoreSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.IDVerified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.IDVerified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.IDVerifiedSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.IDVerifiedSpecified;

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Email != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.Email = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Email;
                                        }

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.EIASToken != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.EIASToken = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.EIASToken;
                                        }

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Membership != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.Membership = new MembershipDetailType[ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Membership.Length];
                                            for (int z = 0; ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Membership.Length > z; z++)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.Membership[z] = new MembershipDetailType();
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.Membership[z].Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Membership[z].Any;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.Membership[z].ExpiryDate = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Membership[z].ExpiryDate;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.Membership[z].ExpiryDateSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Membership[z].ExpiryDateSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.Membership[z].Site = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Membership[z].Site;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.Membership[z].SiteSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Membership[z].SiteSpecified;

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Membership[z].ProgramName != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.Membership[z].ProgramName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Membership[z].ProgramName;
                                                }
                                            }
                                        }

                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.NewUser = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.NewUser;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.NewUserSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.NewUserSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.PayPalAccountLevel = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.PayPalAccountLevel;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.PayPalAccountLevelSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.PayPalAccountLevelSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.PayPalAccountStatus = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.PayPalAccountStatus;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.PayPalAccountStatusSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.PayPalAccountStatusSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.PayPalAccountType = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.PayPalAccountType;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.PayPalAccountTypeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.PayPalAccountTypeSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.PositiveFeedbackPercent = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.PositiveFeedbackPercent;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.PositiveFeedbackPercentSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.PositiveFeedbackPercentSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.QualifiesForSelling = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.QualifiesForSelling;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.QualifiesForSellingSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.QualifiesForSellingSpecified;

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress = new AddressType();
                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressAttribute != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressAttribute = new AddressAttributeType[ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressAttribute.Length];
                                                for (int z = 0; ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressAttribute.Length > z; z++)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[z] = new AddressAttributeType();
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[z].type = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[z].type;
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[z].typeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[z].typeSpecified;

                                                    if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[z].Value != null)
                                                    {
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[z].Value = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressAttribute[z].Value;
                                                    }
                                                }
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressID != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressID;
                                            }

                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressOwner = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressOwner;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressOwnerSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressOwnerSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressRecordType = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressRecordType;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressRecordTypeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressRecordTypeSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressStatus = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressStatus;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressStatusSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressStatusSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressUsage = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressUsage;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressUsageSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.AddressUsageSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Any;

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.CityName != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.CityName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.CityName;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.CompanyName != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.CompanyName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.CompanyName;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.CountryName != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.CountryName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.CountryName;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.County != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.County = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.County;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.ExternalAddressID != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.ExternalAddressID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.ExternalAddressID;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.FirstName != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.FirstName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.FirstName;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.InternationalName != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.InternationalName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.InternationalName;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.InternationalStateAndCity != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.InternationalStateAndCity = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.InternationalStateAndCity;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.InternationalStreet != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.InternationalStreet = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.InternationalStreet;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.LastName != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.LastName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.LastName;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Name != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Name = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Name;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Phone != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Phone = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Phone;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Phone2 != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Phone2 = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Phone2;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.PhoneAreaOrCityCode != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.PhoneAreaOrCityCode = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.PhoneAreaOrCityCode;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.PhoneCountryPrefix != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.PhoneCountryPrefix = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.PhoneCountryPrefix;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.PhoneLocalNumber != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.PhoneLocalNumber = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.PhoneLocalNumber;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.PostalCode != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.PostalCode = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.PostalCode;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.ReferenceID != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.ReferenceID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.ReferenceID;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.StateOrProvince != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.StateOrProvince = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.StateOrProvince;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Street != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Street = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Street;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Street1 != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Street1 = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Street1;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Street2 != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Street2 = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationAddress.Street2;
                                            }
                                        }

                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationDate = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationDate;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationDateSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.RegistrationDateSpecified;

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo = new SellerType();
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.AllowPaymentEdit = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.AllowPaymentEdit;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.Any;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.BillingCurrency = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.BillingCurrency;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.BillingCurrencySpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.BillingCurrencySpecified;

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails = new CharityAffiliationDetailType[ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails.Length];
                                                for (int z = 0; ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails.Length > z; z++)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[z] = new CharityAffiliationDetailType();
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[z].AffiliationType = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[z].AffiliationType;
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[z].AffiliationTypeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[z].AffiliationTypeSpecified;
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[z].Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[z].Any;
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[z].LastUsedTime = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[z].LastUsedTime;
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[z].LastUsedTimeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[z].LastUsedTimeSpecified;

                                                    if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[z].CharityID != null)
                                                    {
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[z].CharityID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails[z].CharityID;
                                                    }
                                                }
                                            }

                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityRegistered = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityRegistered;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityRegisteredSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CharityRegisteredSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CheckoutEnabled = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CheckoutEnabled;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CIPBankAccountStored = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.CIPBankAccountStored;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.DomesticRateTable = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.DomesticRateTable;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.DomesticRateTableSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.DomesticRateTableSpecified;

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility = new FeatureEligibilityType();
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.Any;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForAuctionOneDayDuration = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForAuctionOneDayDuration;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForAuctionOneDayDurationSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForAuctionOneDayDurationSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForFixedPriceOneDayDuration = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForFixedPriceOneDayDuration;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForFixedPriceOneDayDurationSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForFixedPriceOneDayDurationSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNow = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNow;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowMultiple = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowMultiple;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowMultipleSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowMultipleSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForVariations = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForVariations;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForVariationsSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForVariationsSpecified;
                                            }

                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.GoodStanding = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.GoodStanding;

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo = new IntegratedMerchantCreditCardInfoType();
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo.Any;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo.SupportedSite = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo.SupportedSite;
                                            }

                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.InternationalRateTable = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.InternationalRateTable;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.InternationalRateTableSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.InternationalRateTableSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.MerchandizingPref = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.MerchandizingPref;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.MerchandizingPrefSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.MerchandizingPrefSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.PaisaPayEscrowEMIStatus = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.PaisaPayEscrowEMIStatus;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.PaisaPayEscrowEMIStatusSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.PaisaPayEscrowEMIStatusSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.PaisaPayStatus = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.PaisaPayStatus;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.PaisaPayStatusSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.PaisaPayStatusSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.PaymentMethod = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.PaymentMethod;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.PaymentMethodSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.PaymentMethodSpecified;

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference = new ProStoresCheckoutPreferenceType();
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.Any;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.CheckoutRedirectProStores = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.CheckoutRedirectProStores;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.CheckoutRedirectProStoresSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.CheckoutRedirectProStoresSpecified;

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails = new ProStoresDetailsType();
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.Any;
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.Status = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.Status;
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.StatusSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.StatusSpecified;

                                                    if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.SellerThirdPartyUsername != null)
                                                    {
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.SellerThirdPartyUsername = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.SellerThirdPartyUsername;
                                                    }

                                                    if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.StoreName != null)
                                                    {
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.StoreName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.StoreName;
                                                    }
                                                }
                                            }

                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.QualifiesForB2BVAT = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.QualifiesForB2BVAT;

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent = new RecoupmentPolicyConsentType();
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent.Any;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent.Site = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent.Site;
                                            }

                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.RegisteredBusinessSeller = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.RegisteredBusinessSeller;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.RegisteredBusinessSellerSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.RegisteredBusinessSellerSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SafePaymentExempt = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SafePaymentExempt;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SafePaymentExemptSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SafePaymentExemptSpecified;

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo = new SchedulingInfoType();
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo.Any;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledItems = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledItems;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledItemsSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledItemsSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledMinutes = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledMinutes;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledMinutesSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledMinutesSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MinScheduledMinutes = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MinScheduledMinutes;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MinScheduledMinutesSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MinScheduledMinutesSpecified;
                                            }

                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerBusinessType = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerBusinessType;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerBusinessTypeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerBusinessTypeSpecified;

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent = new SellereBayPaymentProcessConsentCodeType();
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.Any;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethod = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethod;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSet = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSet;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSetSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSetSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSpecified;

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo = new UserAgreementInfoType[ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo.Length];
                                                    for (int z = 0; ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo.Length > z; z++)
                                                    {
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z] = new UserAgreementInfoType();
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].AcceptedTime = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].AcceptedTime;
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].AcceptedTimeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].AcceptedTimeSpecified;
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].Any;
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].SellereBayPaymentProcessEnableTime = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].SellereBayPaymentProcessEnableTime;
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].SellereBayPaymentProcessEnableTimeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].SellereBayPaymentProcessEnableTimeSpecified;
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].SellereBayPaymentProcessStatus = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].SellereBayPaymentProcessStatus;
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].SellereBayPaymentProcessStatusSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].SellereBayPaymentProcessStatusSpecified;
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].Site = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].Site;
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].SiteSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].SiteSpecified;

                                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].UserAgreementURL != null)
                                                        {
                                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].UserAgreementURL = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo[z].UserAgreementURL;
                                                        }
                                                    }
                                                }
                                            }

                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessStatus = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessStatus;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessStatusSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessStatusSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerGuaranteeLevel = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerGuaranteeLevel;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerGuaranteeLevelSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerGuaranteeLevelSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerLevel = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerLevel;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerLevelSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerLevelSpecified;

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute = new AddressAttributeType[ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute.Length];
                                                for (int z = 0; ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute.Length > z; z++)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[z] = new AddressAttributeType();
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[z].type = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[z].type;
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[z].typeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[z].typeSpecified;

                                                    if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[z].Value != null)
                                                    {
                                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[z].Value = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute[z].Value;
                                                    }
                                                }
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress = new AddressType();
                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressID != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressID;
                                                }

                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressOwner = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressOwner;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressOwnerSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressOwnerSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressRecordType = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressRecordType;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressRecordTypeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressRecordTypeSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressStatus = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressStatus;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressStatusSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressStatusSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressUsage = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressUsage;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressUsageSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressUsageSpecified;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Any;

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CityName != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CityName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CityName;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CompanyName != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CompanyName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CompanyName;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CountryName != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CountryName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CountryName;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.County != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.County = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.County;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.ExternalAddressID != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.ExternalAddressID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.ExternalAddressID;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.FirstName != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.FirstName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.FirstName;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalName != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalName;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalStateAndCity != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalStateAndCity = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalStateAndCity;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalStreet != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalStreet = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalStreet;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.LastName != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.LastName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.LastName;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Name != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Name = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Name;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Phone != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Phone = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Phone;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Phone2 != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Phone2 = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Phone2;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneAreaOrCityCode != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneAreaOrCityCode = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneAreaOrCityCode;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneCountryPrefix != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneCountryPrefix = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneCountryPrefix;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneLocalNumber != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneLocalNumber = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneLocalNumber;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PostalCode != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PostalCode = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PostalCode;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.ReferenceID != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.ReferenceID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.ReferenceID;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.StateOrProvince != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.StateOrProvince = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.StateOrProvince;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street1 != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street1 = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street1;
                                                }

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street2 != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street2 = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street2;
                                                }
                                            }

                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.StoreOwner = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.StoreOwner;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.StoreSite = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.StoreSite;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.StoreSiteSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.StoreSiteSpecified;

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.StoreURL != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.StoreURL = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.StoreURL;
                                            }

                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.TopRatedSeller = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.TopRatedSeller;

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails = new TopRatedSellerDetailsType();
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails.Any;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails.TopRatedProgram = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails.TopRatedProgram;
                                            }

                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.TopRatedSellerSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.TopRatedSellerSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.TransactionPercent = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.TransactionPercent;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.TransactionPercentSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerInfo.TransactionPercentSpecified;
                                        }

                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerPaymentMethod = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerPaymentMethod;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SellerPaymentMethodSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SellerPaymentMethodSpecified;

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressAttribute != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressAttribute = new AddressAttributeType[ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressAttribute.Length];
                                            for (int z = 0; ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressAttribute.Length > z; z++)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressAttribute[z] = new AddressAttributeType();
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressAttribute[z].type = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressAttribute[z].type;
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressAttribute[z].typeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressAttribute[z].typeSpecified;

                                                if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressAttribute[z].Value != null)
                                                {
                                                    treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressAttribute[z].Value = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressAttribute[z].Value;
                                                }
                                            }
                                        }

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress = new AddressType();
                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressID != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressID;
                                            }

                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressOwner = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressOwner;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressOwnerSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressOwnerSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressRecordType = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressRecordType;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressRecordTypeSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressRecordTypeSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressStatus = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressStatus;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressStatusSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressStatusSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressUsage = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressUsage;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressUsageSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.AddressUsageSpecified;
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Any = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Any;

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.CityName != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.CityName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.CityName;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.CompanyName != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.CompanyName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.CompanyName;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.CountryName != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.CountryName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.CountryName;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.County != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.County = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.County;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.ExternalAddressID != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.ExternalAddressID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.ExternalAddressID;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.FirstName != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.FirstName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.FirstName;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.InternationalName != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.InternationalName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.InternationalName;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.InternationalStateAndCity != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.InternationalStateAndCity = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.InternationalStateAndCity;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.InternationalStreet != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.InternationalStreet = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.InternationalStreet;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.LastName != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.LastName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.LastName;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Name != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Name = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Name;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Phone != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Phone = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Phone;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Phone2 != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Phone2 = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Phone2;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.PhoneAreaOrCityCode != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.PhoneAreaOrCityCode = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.PhoneAreaOrCityCode;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.PhoneCountryPrefix != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.PhoneCountryPrefix = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.PhoneCountryPrefix;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.PhoneLocalNumber != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.PhoneLocalNumber = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.PhoneLocalNumber;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.PostalCode != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.PostalCode = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.PostalCode;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.ReferenceID != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.ReferenceID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.ReferenceID;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.StateOrProvince != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.StateOrProvince = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.StateOrProvince;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Street != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Street = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Street;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Street1 != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Street1 = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Street1;
                                            }

                                            if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Street2 != null)
                                            {
                                                treecatItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Street2 = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.ShippingAddress.Street2;
                                            }
                                        }

                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.Site = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Site;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SiteSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SiteSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SiteVerified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SiteVerified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SiteVerifiedSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SiteVerifiedSpecified;

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SkypeID != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.SkypeID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.SkypeID;
                                        }

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.StaticAlias != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.StaticAlias = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.StaticAlias;
                                        }

                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.Status = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.Status;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.StatusSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.StatusSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.TUVLevel = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.TUVLevel;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.TUVLevelSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.TUVLevelSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.UniqueNegativeFeedbackCount = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UniqueNegativeFeedbackCount;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.UniqueNegativeFeedbackCountSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UniqueNegativeFeedbackCountSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.UniqueNeutralFeedbackCount = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UniqueNeutralFeedbackCount;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.UniqueNeutralFeedbackCountSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UniqueNeutralFeedbackCountSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.UniquePositiveFeedbackCount = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UniquePositiveFeedbackCount;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.UniquePositiveFeedbackCountSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UniquePositiveFeedbackCountSpecified;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.UserAnonymized = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UserAnonymized;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.UserAnonymizedSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UserAnonymizedSpecified;

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UserFirstName != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.UserFirstName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UserFirstName;
                                        }

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UserID != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.UserID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UserID;
                                        }

                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.UserIDChanged = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UserIDChanged;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.UserIDChangedSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UserIDChangedSpecified;

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UserLastName != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.UserLastName = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UserLastName;
                                        }

                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.UserSubscription = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.UserSubscription;

                                        if (ebayItem.Variations.Variation[y].SellingStatus.HighBidder.VATID != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.HighBidder.VATID = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.VATID;
                                        }

                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.VATStatus = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.VATStatus;
                                        treecatItem.Variations.Variation[y].SellingStatus.HighBidder.VATStatusSpecified = ebayItem.Variations.Variation[y].SellingStatus.HighBidder.VATStatusSpecified;
                                    }

                                    treecatItem.Variations.Variation[y].SellingStatus.LeadCount = ebayItem.Variations.Variation[y].SellingStatus.LeadCount;
                                    treecatItem.Variations.Variation[y].SellingStatus.LeadCountSpecified = ebayItem.Variations.Variation[y].SellingStatus.LeadCountSpecified;
                                    treecatItem.Variations.Variation[y].SellingStatus.ListingStatus = ebayItem.Variations.Variation[y].SellingStatus.ListingStatus;
                                    treecatItem.Variations.Variation[y].SellingStatus.ListingStatusSpecified = ebayItem.Variations.Variation[y].SellingStatus.ListingStatusSpecified;

                                    if (ebayItem.Variations.Variation[y].SellingStatus.MinimumToBid != null)
                                    {
                                        treecatItem.Variations.Variation[y].SellingStatus.MinimumToBid = new AmountType();
                                        treecatItem.Variations.Variation[y].SellingStatus.MinimumToBid.currencyID = ebayItem.Variations.Variation[y].SellingStatus.MinimumToBid.currencyID;
                                        treecatItem.Variations.Variation[y].SellingStatus.MinimumToBid.Value = ebayItem.Variations.Variation[y].SellingStatus.MinimumToBid.Value;
                                    }

                                    if (ebayItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails != null)
                                    {
                                        treecatItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails = new PromotionalSaleDetailsType();
                                        treecatItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.Any = ebayItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.Any;
                                        treecatItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.EndTime = ebayItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.EndTime;
                                        treecatItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.EndTimeSpecified = ebayItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.EndTimeSpecified;

                                        if (ebayItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.OriginalPrice != null)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.OriginalPrice = new AmountType();
                                            treecatItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.OriginalPrice.currencyID = ebayItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.OriginalPrice.currencyID;
                                            treecatItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.OriginalPrice.Value = ebayItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.OriginalPrice.Value;
                                        }

                                        treecatItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.StartTime = ebayItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.StartTime;
                                        treecatItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.StartTimeSpecified = ebayItem.Variations.Variation[y].SellingStatus.PromotionalSaleDetails.StartTimeSpecified;
                                    }

                                    treecatItem.Variations.Variation[y].SellingStatus.QuantitySold = ebayItem.Variations.Variation[y].SellingStatus.QuantitySold;
                                    treecatItem.Variations.Variation[y].SellingStatus.QuantitySoldByPickupInStore = ebayItem.Variations.Variation[y].SellingStatus.QuantitySoldByPickupInStore;
                                    treecatItem.Variations.Variation[y].SellingStatus.QuantitySoldByPickupInStoreSpecified = ebayItem.Variations.Variation[y].SellingStatus.QuantitySoldByPickupInStoreSpecified;
                                    treecatItem.Variations.Variation[y].SellingStatus.QuantitySoldSpecified = ebayItem.Variations.Variation[y].SellingStatus.QuantitySoldSpecified;
                                    treecatItem.Variations.Variation[y].SellingStatus.ReserveMet = ebayItem.Variations.Variation[y].SellingStatus.ReserveMet;
                                    treecatItem.Variations.Variation[y].SellingStatus.ReserveMetSpecified = ebayItem.Variations.Variation[y].SellingStatus.ReserveMetSpecified;
                                    treecatItem.Variations.Variation[y].SellingStatus.SecondChanceEligible = ebayItem.Variations.Variation[y].SellingStatus.SecondChanceEligible;
                                    treecatItem.Variations.Variation[y].SellingStatus.SecondChanceEligibleSpecified = ebayItem.Variations.Variation[y].SellingStatus.SecondChanceEligibleSpecified;
                                    treecatItem.Variations.Variation[y].SellingStatus.SoldAsBin = ebayItem.Variations.Variation[y].SellingStatus.SoldAsBin;
                                    treecatItem.Variations.Variation[y].SellingStatus.SoldAsBinSpecified = ebayItem.Variations.Variation[y].SellingStatus.SoldAsBinSpecified;

                                    if (ebayItem.Variations.Variation[y].SellingStatus.SuggestedBidValues != null)
                                    {
                                        treecatItem.Variations.Variation[y].SellingStatus.SuggestedBidValues.BidValue = new AmountType[ebayItem.Variations.Variation[y].SellingStatus.SuggestedBidValues.BidValue.Length];
                                        for (int z = 0; ebayItem.Variations.Variation[y].SellingStatus.SuggestedBidValues.BidValue.Length > z; z++)
                                        {
                                            treecatItem.Variations.Variation[y].SellingStatus.SuggestedBidValues.BidValue[z] = new AmountType();
                                            treecatItem.Variations.Variation[y].SellingStatus.SuggestedBidValues.BidValue[z].currencyID = ebayItem.Variations.Variation[y].SellingStatus.SuggestedBidValues.BidValue[z].currencyID;
                                            treecatItem.Variations.Variation[y].SellingStatus.SuggestedBidValues.BidValue[z].Value = ebayItem.Variations.Variation[y].SellingStatus.SuggestedBidValues.BidValue[z].Value;
                                        }
                                    }
                                }

                                if (ebayItem.Variations.Variation[y].SKU != null)
                                {
                                    treecatItem.Variations.Variation[y].SKU = ebayItem.Variations.Variation[y].SKU;
                                }

                                if (ebayItem.Variations.Variation[y].StartPrice != null)
                                {
                                    treecatItem.Variations.Variation[y].StartPrice = new AmountType();
                                    treecatItem.Variations.Variation[y].StartPrice.currencyID = ebayItem.Variations.Variation[y].StartPrice.currencyID;
                                    treecatItem.Variations.Variation[y].StartPrice.Value = ebayItem.Variations.Variation[y].StartPrice.Value;
                                }

                                if (ebayItem.Variations.Variation[y].UnitCost != null)
                                {
                                    treecatItem.Variations.Variation[y].UnitCost = new AmountType();
                                    treecatItem.Variations.Variation[y].UnitCost.currencyID = ebayItem.Variations.Variation[y].UnitCost.currencyID;
                                    treecatItem.Variations.Variation[y].UnitCost.Value = ebayItem.Variations.Variation[y].UnitCost.Value;
                                }

                                treecatItem.Variations.Variation[y].UnitsAvailable = ebayItem.Variations.Variation[y].UnitsAvailable;
                                treecatItem.Variations.Variation[y].UnitsAvailableSpecified = ebayItem.Variations.Variation[y].UnitsAvailableSpecified;

                                if (ebayItem.Variations.Variation[y].VariationProductListingDetails != null)
                                {
                                    treecatItem.Variations.Variation[y].VariationProductListingDetails = new VariationProductListingDetailsType();
                                    treecatItem.Variations.Variation[y].VariationProductListingDetails.Any = ebayItem.Variations.Variation[y].VariationProductListingDetails.Any;

                                    if (ebayItem.Variations.Variation[y].VariationProductListingDetails.EAN != null)
                                    {
                                        treecatItem.Variations.Variation[y].VariationProductListingDetails.EAN = ebayItem.Variations.Variation[y].VariationProductListingDetails.EAN;
                                    }

                                    if (ebayItem.Variations.Variation[y].VariationProductListingDetails.ISBN != null)
                                    {
                                        treecatItem.Variations.Variation[y].VariationProductListingDetails.ISBN = ebayItem.Variations.Variation[y].VariationProductListingDetails.ISBN;
                                    }

                                    if (ebayItem.Variations.Variation[y].VariationProductListingDetails.NameValueList != null)
                                    {
                                        treecatItem.Variations.Variation[y].VariationProductListingDetails.NameValueList = new NameValueListType[ebayItem.Variations.Variation[y].VariationProductListingDetails.NameValueList.Length];
                                        for (int z = 0; ebayItem.Variations.Variation[y].VariationProductListingDetails.NameValueList.Length > z; z++)
                                        {
                                            treecatItem.Variations.Variation[y].VariationProductListingDetails.NameValueList[z] = new NameValueListType();
                                            treecatItem.Variations.Variation[y].VariationProductListingDetails.NameValueList[z].Any = ebayItem.Variations.Variation[y].VariationProductListingDetails.NameValueList[z].Any;
                                            treecatItem.Variations.Variation[y].VariationProductListingDetails.NameValueList[z].Source = ebayItem.Variations.Variation[y].VariationProductListingDetails.NameValueList[z].Source;
                                            treecatItem.Variations.Variation[y].VariationProductListingDetails.NameValueList[z].SourceSpecified = ebayItem.Variations.Variation[y].VariationProductListingDetails.NameValueList[z].SourceSpecified;

                                            if (ebayItem.Variations.Variation[y].VariationProductListingDetails.NameValueList[z].Value != null)
                                            {
                                                treecatItem.Variations.Variation[y].VariationProductListingDetails.NameValueList[z].Value = ebayItem.Variations.Variation[y].VariationProductListingDetails.NameValueList[z].Value;
                                            }

                                            if (ebayItem.Variations.Variation[y].VariationProductListingDetails.NameValueList[z].Name != null)
                                            {
                                                treecatItem.Variations.Variation[y].VariationProductListingDetails.NameValueList[z].Name = ebayItem.Variations.Variation[y].VariationProductListingDetails.NameValueList[z].Name;
                                            }
                                        }
                                    }

                                    if (ebayItem.Variations.Variation[y].VariationProductListingDetails.ProductReferenceID != null)
                                    {
                                        treecatItem.Variations.Variation[y].VariationProductListingDetails.ProductReferenceID = ebayItem.Variations.Variation[y].VariationProductListingDetails.ProductReferenceID;
                                    }

                                    if (ebayItem.Variations.Variation[y].VariationProductListingDetails.UPC != null)
                                    {
                                        treecatItem.Variations.Variation[y].VariationProductListingDetails.UPC = ebayItem.Variations.Variation[y].VariationProductListingDetails.UPC;
                                    }
                                }

                                if (ebayItem.Variations.Variation[y].VariationSpecifics != null)
                                {
                                    treecatItem.Variations.Variation[y].VariationSpecifics = new NameValueListType[ebayItem.Variations.Variation[y].VariationSpecifics.Length];
                                    for (int z = 0; ebayItem.Variations.Variation[y].VariationSpecifics.Length > z; z++)
                                    {
                                        treecatItem.Variations.Variation[y].VariationSpecifics[z] = new NameValueListType();
                                        treecatItem.Variations.Variation[y].VariationSpecifics[z].Any = ebayItem.Variations.Variation[y].VariationSpecifics[z].Any;
                                        treecatItem.Variations.Variation[y].VariationSpecifics[z].Source = ebayItem.Variations.Variation[y].VariationSpecifics[z].Source;
                                        treecatItem.Variations.Variation[y].VariationSpecifics[z].SourceSpecified = ebayItem.Variations.Variation[y].VariationSpecifics[z].SourceSpecified;

                                        if (ebayItem.Variations.Variation[y].VariationSpecifics[z].Value != null)
                                        {
                                            treecatItem.Variations.Variation[y].VariationSpecifics[z].Value = ebayItem.Variations.Variation[y].VariationSpecifics[z].Value;
                                        }

                                        if (ebayItem.Variations.Variation[y].VariationSpecifics[z].Name != null)
                                        {
                                            treecatItem.Variations.Variation[y].VariationSpecifics[z].Name = ebayItem.Variations.Variation[y].VariationSpecifics[z].Name;
                                        }
                                    }
                                }

                                if (ebayItem.Variations.Variation[y].VariationTitle != null)
                                {
                                    treecatItem.Variations.Variation[y].VariationTitle = ebayItem.Variations.Variation[y].VariationTitle;
                                }

                                if (ebayItem.Variations.Variation[y].VariationViewItemURL != null)
                                {
                                    treecatItem.Variations.Variation[y].VariationViewItemURL = ebayItem.Variations.Variation[y].VariationViewItemURL;
                                }

                                treecatItem.Variations.Variation[y].WatchCount = ebayItem.Variations.Variation[y].WatchCount;
                                treecatItem.Variations.Variation[y].WatchCountSpecified = ebayItem.Variations.Variation[y].WatchCountSpecified;
                            }
                        }

                        if (ebayItem.Variations.VariationSpecificsSet != null)
                        {
                            treecatItem.Variations.VariationSpecificsSet = new NameValueListType[ebayItem.Variations.VariationSpecificsSet.Length];
                            for (int y = 0; ebayItem.Variations.VariationSpecificsSet.Length > y; y++)
                            {
                                treecatItem.Variations.VariationSpecificsSet[y] = new NameValueListType();
                                treecatItem.Variations.VariationSpecificsSet[y].Any = ebayItem.Variations.VariationSpecificsSet[y].Any;
                                treecatItem.Variations.VariationSpecificsSet[y].Source = ebayItem.Variations.VariationSpecificsSet[y].Source;
                                treecatItem.Variations.VariationSpecificsSet[y].SourceSpecified = ebayItem.Variations.VariationSpecificsSet[y].SourceSpecified;

                                if (ebayItem.Variations.VariationSpecificsSet[y].Value != null)
                                {
                                    treecatItem.Variations.VariationSpecificsSet[y].Value = ebayItem.Variations.VariationSpecificsSet[y].Value;
                                }

                                if (ebayItem.Variations.VariationSpecificsSet[y].Name != null)
                                {
                                    treecatItem.Variations.VariationSpecificsSet[y].Name = ebayItem.Variations.VariationSpecificsSet[y].Name;
                                }
                            }
                        }
                    }

                    if (ebayItem.VATDetails != null)
                    {
                        treecatItem.VATDetails = new VATDetailsType();
                        treecatItem.VATDetails.Any = ebayItem.VATDetails.Any;
                        treecatItem.VATDetails.BusinessSeller = ebayItem.VATDetails.BusinessSeller;
                        treecatItem.VATDetails.BusinessSellerSpecified = ebayItem.VATDetails.BusinessSellerSpecified;
                        treecatItem.VATDetails.RestrictedToBusiness = ebayItem.VATDetails.RestrictedToBusiness;
                        treecatItem.VATDetails.RestrictedToBusinessSpecified = ebayItem.VATDetails.RestrictedToBusinessSpecified;
                        treecatItem.VATDetails.VATPercent = ebayItem.VATDetails.VATPercent;
                        treecatItem.VATDetails.VATPercentSpecified = ebayItem.VATDetails.VATPercentSpecified;

                        if (ebayItem.VATDetails.VATSite != null)
                        {
                            treecatItem.VATDetails.VATSite = ebayItem.VATDetails.VATSite;
                        }

                        if (ebayItem.VATDetails.VATID != null)
                        {
                            treecatItem.VATDetails.VATID = ebayItem.VATDetails.VATID;
                        }
                    }

                    if (ebayItem.VIN != null)
                    {
                        treecatItem.VIN = ebayItem.VIN;
                    }

                    if (ebayItem.VINLink != null)
                    {
                        treecatItem.VINLink = ebayItem.VINLink;
                    }

                    if (ebayItem.VRM != null)
                    {
                        treecatItem.VRM = ebayItem.VRM;
                    }

                    if (ebayItem.VRMLink != null)
                    {
                        treecatItem.VRMLink = ebayItem.VRMLink;
                    }

                    treecatItem.WatchCount = ebayItem.WatchCount;
                    treecatItem.WatchCountSpecified = ebayItem.WatchCountSpecified;

                    // Ebay has no shares/ likes / comments
                    // Item.Categories.Add("BFRST-NPY");
                    // Item.Colors.Add("BFRST-NPY");

                    if (ebayItem.ListingDetails.StartTime != null)
                    {
                        treecatItem.Date = ebayItem.ListingDetails.StartTime.ToString().Substring(0, 10);
                    }

                    if (ebayItem.ListingDetails.ViewItemURL != null)
                    {
                        treecatItem.URL = ebayItem.ListingDetails.ViewItemURL;
                    }

                    if (ebayItem.SellingStatus != null)
                    {
                        treecatItem.HasOffer = ebayItem.SellingStatus.BidCount > 0 ? "Yes" : "No";
                    }

                    result.Add(treecatItem);
                }
                catch (NullReferenceException e)
                {
                    logger.LogInformation("Error listing eBay items: " + e.ToString());
                }
            }
          
            return result;
        }

        public async Task<List<string>> ImportTreecatIDs(dynamic itemsToImport, List<Item> userEbayCachedItems)
        {
            List<string> cachedTreecatEbayIDs = new List<string>();
            List<string> treecatIDs = new List<string>();
            for (int i = 0; itemsToImport.EbayIDs.Count > i; i++)
                {
                // New TREECAT ID using Guid
                Guid treecatItemID = Guid.NewGuid();

                // Get "treecat_list" from cache into list
                var treecatByteItems = await this.cache.GetAsync("treecat_list");

                if (treecatByteItems != null)
                {
                    treecatIDs = JsonConvert.DeserializeObject<List<string>>(
                        ASCIIEncoding.UTF8.GetString(treecatByteItems));
                }

                // Link TreeCat ID to Item by importing the entire Item value to the GUID key
                // Also checks the item to import 
                foreach (var ebayItem in userEbayCachedItems)
                {
                    if (treecatByteItems != null)
                    {
                        foreach (var treecatID in treecatIDs)
                        {
                            var treecatGUID = await this.cache.GetAsync(treecatID);
                            Item treecatItem = new Item();

                            treecatItem = JsonConvert.DeserializeObject<Item>(
                                    ASCIIEncoding.UTF8.GetString(treecatGUID));

                            cachedTreecatEbayIDs.Add(treecatItem.ID);
                        }

                        if (itemsToImport.EbayIDs[i] == ebayItem.ID)
                        {
                            bool hasMatch = cachedTreecatEbayIDs.Contains(ebayItem.ID);
                            if (hasMatch == false)
                            {
                                // Insert GUID Key to Ebay Item value
                                await this.cache.SetAsync(
                                    treecatItemID.ToString(),
                                    ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(ebayItem)),
                                    new DistributedCacheEntryOptions()
                                    {
                                        AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                                    });

                                // Update "treecat_list" cache by adding the new TreeCat ID generated with GUID
                                treecatIDs.Add(treecatItemID.ToString());

                                await this.cache.SetAsync(
                                    "treecat_list",
                                    ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(treecatIDs)),
                                    new DistributedCacheEntryOptions()
                                    {
                                        AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                                    });
                            }
                        }
                    }
                    else if (treecatByteItems == null)
                    {
                        if (itemsToImport.EbayIDs[i] == ebayItem.ID)
                        {
                            await this.cache.SetAsync(
                                treecatItemID.ToString(),
                                ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(ebayItem)),
                                new DistributedCacheEntryOptions()
                                {
                                    AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                                });

                            // Update "treecat_list" cache by adding the new TreeCat ID generated with GUID
                            await this.cache.RemoveAsync("treecat_list");
                            treecatIDs.Add(treecatItemID.ToString());

                            await this.cache.SetAsync(
                                "treecat_list",
                                ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(treecatIDs)),
                                new DistributedCacheEntryOptions()
                                {
                                    AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                                });

                            // Update "treecat_list" cache by adding the new TreeCat ID generated with GUID
                            await this.cache.RemoveAsync("treecat_list");
                            treecatIDs.Add(treecatItemID.ToString());

                            await this.cache.SetAsync(
                                "treecat_list",
                                ASCIIEncoding.UTF8.GetBytes(JsonConvert.SerializeObject(treecatIDs)),
                                new DistributedCacheEntryOptions()
                                {
                                    AbsoluteExpiration = DateTime.Now + TimeSpan.FromDays(200)
                                });
                        }
                    }
                }
            }
            return treecatIDs;
        }

    }
}
