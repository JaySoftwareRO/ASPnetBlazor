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

        IDistributedCache cache;
        ILogger logger;
        int liveCallLimit;
        int liveCalls = 0;
        ITokenGetter tokenGetter;
        string accountID;

        public EbayLister(IDistributedCache cache, ILogger logger, int liveCallLimit, ITokenGetter tokenGetter)
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

                if (sellingItems.GetMyeBaySellingResponse1 == null)
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

            foreach (var ebayItem in sellingItems.GetMyeBaySellingResponse1.ActiveList.ItemArray)
            {
                try
                {
                    var treecatItem = new Item();

                    treecatItem.ID = ebayItem.ItemID;
                    treecatItem.Provisioner = "eBay";
                    treecatItem.Title = ebayItem.Title;
                    treecatItem.Price = ebayItem.BuyItNowPrice.Value;
                    treecatItem.Description = "BFRST-NPY";
                    treecatItem.Status = ebayItem.SellingStatus.ListingStatus.ToString();
                    treecatItem.Stock = ebayItem.QuantityAvailable;
                    treecatItem.MainImageURL = ebayItem.PictureDetails.GalleryURL;
                    treecatItem.Size = "BFRST-NPY";
                    treecatItem.Brand = "BFRST-NPY";
                    treecatItem.SKU = ebayItem.SKU;

                    // Ebay Item only
                    treecatItem.ApplicationData = ebayItem.ApplicationData;
                    treecatItem.ApplyBuyerProtection = ebayItem.ApplyBuyerProtection;
                    treecatItem.AttributeArray = ebayItem.AttributeArray;
                    treecatItem.AttributeSetArray = ebayItem.AttributeSetArray;
                    treecatItem.AutoPay = ebayItem.AutoPay;
                    treecatItem.AutoPaySpecified = ebayItem.AutoPaySpecified;
                    treecatItem.AvailableForPickupDropOff = ebayItem.AvailableForPickupDropOff;
                    treecatItem.AvailableForPickupDropOffSpecified = ebayItem.AvailableForPickupDropOffSpecified;

                    //treecatItem.BestOfferDetails.Any = ebayItem.BestOfferDetails.Any;
                    //treecatItem.BestOfferDetails.BestOffer.currencyID = ebayItem.BestOfferDetails.BestOffer.currencyID;
                    //treecatItem.BestOfferDetails.BestOffer.Value = ebayItem.BestOfferDetails.BestOffer.Value;
                    //treecatItem.BestOfferDetails.BestOfferCount = ebayItem.BestOfferDetails.BestOfferCount;
                    //treecatItem.BestOfferDetails.BestOfferCountSpecified = ebayItem.BestOfferDetails.BestOfferCountSpecified;
                    //treecatItem.BestOfferDetails.BestOfferEnabled = ebayItem.BestOfferDetails.BestOfferEnabled;
                    //treecatItem.BestOfferDetails.BestOfferEnabledSpecified = ebayItem.BestOfferDetails.BestOfferEnabledSpecified;
                    //treecatItem.BestOfferDetails.BestOfferStatus = ebayItem.BestOfferDetails.BestOfferStatus;
                    //treecatItem.BestOfferDetails.BestOfferStatusSpecified = ebayItem.BestOfferDetails.BestOfferStatusSpecified;
                    //treecatItem.BestOfferDetails.BestOfferType = ebayItem.BestOfferDetails.BestOfferType;
                    //treecatItem.BestOfferDetails.BestOfferTypeSpecified = ebayItem.BestOfferDetails.BestOfferTypeSpecified;
                    //treecatItem.BestOfferDetails.NewBestOffer = ebayItem.BestOfferDetails.NewBestOffer;
                    //treecatItem.BestOfferDetails.NewBestOfferSpecified = ebayItem.BestOfferDetails.NewBestOfferSpecified;

                    //ebayItem.BestOfferEnabled;
                    //ebayItem.BestOfferEnabledSpecified;

                    //ebayItem.BiddingDetails;
                    //ebayItem.BiddingDetails.Any;
                    //ebayItem.BiddingDetails.BidAssistant;
                    //ebayItem.BiddingDetails.BidAssistantSpecified;
                    //ebayItem.BiddingDetails.ConvertedMaxBid.currencyID;
                    //ebayItem.BiddingDetails.ConvertedMaxBid.Value;
                    //ebayItem.BiddingDetails.MaxBid.currencyID;
                    //ebayItem.BiddingDetails.MaxBid.Value;
                    //ebayItem.BiddingDetails.QuantityBid;
                    //ebayItem.BiddingDetails.QuantityBidSpecified;
                    //ebayItem.BiddingDetails.QuantityWon;
                    //ebayItem.BiddingDetails.QuantityWonSpecified;
                    //ebayItem.BiddingDetails.Winning;
                    //ebayItem.BiddingDetails.WinningSpecified;

                    //ebayItem.BidGroupItem;
                    //ebayItem.BidGroupItemSpecified;

                    //ebayItem.BusinessSellerDetails;
                    //ebayItem.BusinessSellerDetails.AdditionalContactInformation;
                    //ebayItem.BusinessSellerDetails.Address;
                    //ebayItem.BusinessSellerDetails.Address.AddressAttribute;
                    //ebayItem.BusinessSellerDetails.Address.AddressID;
                    //ebayItem.BusinessSellerDetails.Address.AddressOwner;
                    //ebayItem.BusinessSellerDetails.Address.AddressOwnerSpecified;
                    //ebayItem.BusinessSellerDetails.Address.AddressRecordType;
                    //ebayItem.BusinessSellerDetails.Address.AddressRecordTypeSpecified;
                    //ebayItem.BusinessSellerDetails.Address.AddressStatus;
                    //ebayItem.BusinessSellerDetails.Address.AddressStatusSpecified;
                    //ebayItem.BusinessSellerDetails.Address.AddressUsage;
                    //ebayItem.BusinessSellerDetails.Address.AddressUsageSpecified;
                    //ebayItem.BusinessSellerDetails.Address.Any;
                    //ebayItem.BusinessSellerDetails.Address.CityName;
                    //ebayItem.BusinessSellerDetails.Address.CompanyName;
                    //ebayItem.BusinessSellerDetails.Address.Country;
                    //ebayItem.BusinessSellerDetails.Address.CountryName;
                    //ebayItem.BusinessSellerDetails.Address.CountrySpecified;
                    //ebayItem.BusinessSellerDetails.Address.County;
                    //ebayItem.BusinessSellerDetails.Address.ExternalAddressID;
                    //ebayItem.BusinessSellerDetails.Address.FirstName;
                    //ebayItem.BusinessSellerDetails.Address.InternationalName;
                    //ebayItem.BusinessSellerDetails.Address.InternationalStateAndCity;
                    //ebayItem.BusinessSellerDetails.Address.InternationalStreet;
                    //ebayItem.BusinessSellerDetails.Address.LastName;
                    //ebayItem.BusinessSellerDetails.Address.Name;
                    //ebayItem.BusinessSellerDetails.Address.Phone;
                    //ebayItem.BusinessSellerDetails.Address.Phone2;
                    //ebayItem.BusinessSellerDetails.Address.PhoneAreaOrCityCode;
                    //ebayItem.BusinessSellerDetails.Address.PhoneCountryCode;
                    //ebayItem.BusinessSellerDetails.Address.PhoneCountryCodeSpecified;
                    //ebayItem.BusinessSellerDetails.Address.PhoneCountryPrefix;
                    //ebayItem.BusinessSellerDetails.Address.PhoneLocalNumber;
                    //ebayItem.BusinessSellerDetails.Address.PostalCode;
                    //ebayItem.BusinessSellerDetails.Address.ReferenceID;
                    //ebayItem.BusinessSellerDetails.Address.StateOrProvince;
                    //ebayItem.BusinessSellerDetails.Address.Street;
                    //ebayItem.BusinessSellerDetails.Address.Street1;
                    //ebayItem.BusinessSellerDetails.Address.Street2;
                    //ebayItem.BusinessSellerDetails.Any;
                    //ebayItem.BusinessSellerDetails.Email;
                    //ebayItem.BusinessSellerDetails.Fax;
                    //ebayItem.BusinessSellerDetails.LegalInvoice;
                    //ebayItem.BusinessSellerDetails.LegalInvoiceSpecified;
                    //ebayItem.BusinessSellerDetails.TermsAndConditions;
                    //ebayItem.BusinessSellerDetails.TradeRegistrationNumber;
                    //ebayItem.BusinessSellerDetails.VATDetails;
                    //ebayItem.BusinessSellerDetails.VATDetails.Any;
                    //ebayItem.BusinessSellerDetails.VATDetails.BusinessSeller;
                    //ebayItem.BusinessSellerDetails.VATDetails.BusinessSellerSpecified;
                    //ebayItem.BusinessSellerDetails.VATDetails.RestrictedToBusiness;
                    //ebayItem.BusinessSellerDetails.VATDetails.RestrictedToBusinessSpecified;
                    //ebayItem.BusinessSellerDetails.VATDetails.VATID;
                    //ebayItem.BusinessSellerDetails.VATDetails.VATPercent;
                    //ebayItem.BusinessSellerDetails.VATDetails.VATPercentSpecified;
                    //ebayItem.BusinessSellerDetails.VATDetails.VATSite;

                    //ebayItem.BuyerGuaranteePrice;
                    //ebayItem.BuyerGuaranteePrice.currencyID;
                    //ebayItem.BuyerGuaranteePrice.Value;

                    //ebayItem.BuyerProtection;
                    //ebayItem.BuyerProtectionSpecified;

                    //ebayItem.BuyerRequirementDetails;
                    //ebayItem.BuyerRequirementDetails.Any;
                    //ebayItem.BuyerRequirementDetails.MaximumItemRequirements;
                    //ebayItem.BuyerRequirementDetails.MaximumItemRequirements.MaximumItemCount;
                    //ebayItem.BuyerRequirementDetails.MaximumItemRequirements.MaximumItemCountSpecified;
                    //ebayItem.BuyerRequirementDetails.MaximumItemRequirements.MinimumFeedbackScore;
                    //ebayItem.BuyerRequirementDetails.MaximumItemRequirements.MinimumFeedbackScoreSpecified;
                    //ebayItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.Any;
                    //ebayItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.Count;
                    //ebayItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.CountSpecified;
                    //ebayItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.Period;
                    //ebayItem.BuyerRequirementDetails.MaximumUnpaidItemStrikesInfo.PeriodSpecified;
                    //ebayItem.BuyerRequirementDetails.ShipToRegistrationCountry;
                    //ebayItem.BuyerRequirementDetails.ShipToRegistrationCountrySpecified;
                    //ebayItem.BuyerRequirementDetails.ZeroFeedbackScore;
                    //ebayItem.BuyerRequirementDetails.ZeroFeedbackScoreSpecified;

                    //ebayItem.BuyerResponsibleForShipping;
                    //ebayItem.BuyerResponsibleForShippingSpecified;

                    //ebayItem.BuyItNowPrice;
                    //ebayItem.BuyItNowPrice.currencyID;
                    //ebayItem.BuyItNowPrice.Value;

                    //ebayItem.CategoryMappingAllowed;

                    //ebayItem.CeilingPrice;
                    //ebayItem.CeilingPrice.currencyID;
                    //ebayItem.CeilingPrice.Value;

                    //ebayItem.Charity;
                    //ebayItem.Charity.Any;
                    //ebayItem.Charity.CharityID;
                    //ebayItem.Charity.CharityListing;
                    //ebayItem.Charity.CharityListingSpecified;
                    //ebayItem.Charity.CharityName;
                    //ebayItem.Charity.CharityNumber;
                    //ebayItem.Charity.CharityNumberSpecified;
                    //ebayItem.Charity.DonationPercent;
                    //ebayItem.Charity.DonationPercentSpecified;
                    //ebayItem.Charity.LogoURL;
                    //ebayItem.Charity.Mission;
                    //ebayItem.Charity.Status;
                    //ebayItem.Charity.StatusSpecified;

                    //ebayItem.ClassifiedAdPayPerLeadFee;
                    //ebayItem.ClassifiedAdPayPerLeadFee.currencyID;
                    //ebayItem.ClassifiedAdPayPerLeadFee.Value;

                    //ebayItem.ConditionDefinition;
                    //ebayItem.ConditionDescription;
                    //ebayItem.ConditionDisplayName;
                    //ebayItem.ConditionID;
                    //ebayItem.ConditionIDSpecified;
                    //ebayItem.Country;
                    //ebayItem.CountrySpecified;
                    //ebayItem.CrossBorderTrade;

                    //ebayItem.CrossPromotion.Any;
                    //ebayItem.CrossPromotion.ItemID;
                    //ebayItem.CrossPromotion.PrimaryScheme;
                    //ebayItem.CrossPromotion.PrimarySchemeSpecified;
                    //ebayItem.CrossPromotion.PromotedItem;
                    //ebayItem.CrossPromotion.PromotionMethod;
                    //ebayItem.CrossPromotion.PromotionMethodSpecified;
                    //ebayItem.CrossPromotion.SellerID;
                    //ebayItem.CrossPromotion.ShippingDiscount;
                    //ebayItem.CrossPromotion.StoreName;

                    //ebayItem.Currency;
                    //ebayItem.CurrencySpecified;
                    //ebayItem.DescriptionReviseMode;
                    //ebayItem.DescriptionReviseModeSpecified;

                    //ebayItem.DigitalGoodInfo;
                    //ebayItem.DigitalGoodInfo.Any;
                    //ebayItem.DigitalGoodInfo.DigitalDelivery;
                    //ebayItem.DigitalGoodInfo.DigitalDeliverySpecified;

                    //ebayItem.DisableBuyerRequirements;
                    //ebayItem.DisableBuyerRequirementsSpecified;

                    //ebayItem.DiscountPriceInfo;
                    //ebayItem.DiscountPriceInfo.Any;
                    //ebayItem.DiscountPriceInfo.MadeForOutletComparisonPrice.currencyID;
                    //ebayItem.DiscountPriceInfo.MadeForOutletComparisonPrice.Value;
                    //ebayItem.DiscountPriceInfo.MinimumAdvertisedPrice.currencyID;
                    //ebayItem.DiscountPriceInfo.MinimumAdvertisedPrice.Value;
                    //ebayItem.DiscountPriceInfo.MinimumAdvertisedPriceExposure;
                    //ebayItem.DiscountPriceInfo.MinimumAdvertisedPriceExposureSpecified;
                    //ebayItem.DiscountPriceInfo.OriginalRetailPrice.currencyID;
                    //ebayItem.DiscountPriceInfo.OriginalRetailPrice.Value;
                    //ebayItem.DiscountPriceInfo.PricingTreatment;
                    //ebayItem.DiscountPriceInfo.PricingTreatmentSpecified;
                    //ebayItem.DiscountPriceInfo.SoldOffeBay;
                    //ebayItem.DiscountPriceInfo.SoldOneBay;

                    //ebayItem.DispatchTimeMax;
                    //ebayItem.DispatchTimeMaxSpecified;

                    //ebayItem.Distance;
                    //ebayItem.Distance.Any;
                    //ebayItem.Distance.DistanceMeasurement;
                    //ebayItem.Distance.DistanceUnit;

                    //ebayItem.eBayNotes;
                    //ebayItem.eBayPlus;
                    //ebayItem.eBayPlusEligible;
                    //ebayItem.eBayPlusEligibleSpecified;
                    //ebayItem.eBayPlusSpecified;
                    //ebayItem.EligibleForPickupDropOff;
                    //ebayItem.EligibleForPickupDropOffSpecified;
                    //ebayItem.eMailDeliveryAvailable;
                    //ebayItem.eMailDeliveryAvailableSpecified;
                    //ebayItem.ExtendedSellerContactDetails;

                    //ebayItem.FloorPrice;
                    //ebayItem.FloorPrice.currencyID;
                    //ebayItem.FloorPrice.Value;

                    //ebayItem.FreeAddedCategory;
                    //ebayItem.FreeAddedCategory.Any;
                    //ebayItem.FreeAddedCategory.AutoPayEnabled;
                    //ebayItem.FreeAddedCategory.AutoPayEnabledSpecified;
                    //ebayItem.FreeAddedCategory.B2BVATEnabled;
                    //ebayItem.FreeAddedCategory.B2BVATEnabledSpecified;
                    //ebayItem.FreeAddedCategory.BestOfferEnabled;
                    //ebayItem.FreeAddedCategory.BestOfferEnabledSpecified;
                    //ebayItem.FreeAddedCategory.CatalogEnabled;
                    //ebayItem.FreeAddedCategory.CatalogEnabledSpecified;
                    //ebayItem.FreeAddedCategory.CategoryID;
                    //ebayItem.FreeAddedCategory.CategoryLevel;
                    //ebayItem.FreeAddedCategory.CategoryLevelSpecified;
                    //ebayItem.FreeAddedCategory.CategoryName;
                    //ebayItem.FreeAddedCategory.CategoryParentID;
                    //ebayItem.FreeAddedCategory.CategoryParentName;
                    //ebayItem.FreeAddedCategory.CharacteristicsSets;
                    //ebayItem.FreeAddedCategory.Expired;
                    //ebayItem.FreeAddedCategory.ExpiredSpecified;
                    //ebayItem.FreeAddedCategory.IntlAutosFixedCat;
                    //ebayItem.FreeAddedCategory.IntlAutosFixedCatSpecified;
                    //ebayItem.FreeAddedCategory.Keywords;
                    //ebayItem.FreeAddedCategory.LeafCategory;
                    //ebayItem.FreeAddedCategory.LeafCategorySpecified;
                    //ebayItem.FreeAddedCategory.LSD;
                    //ebayItem.FreeAddedCategory.LSDSpecified;
                    //ebayItem.FreeAddedCategory.NumOfItems;
                    //ebayItem.FreeAddedCategory.NumOfItemsSpecified;
                    //ebayItem.FreeAddedCategory.ORPA;
                    //ebayItem.FreeAddedCategory.ORPASpecified;
                    //ebayItem.FreeAddedCategory.ORRA;
                    //ebayItem.FreeAddedCategory.ORRASpecified;
                    //ebayItem.FreeAddedCategory.ProductFinderIDs;
                    //ebayItem.FreeAddedCategory.ProductSearchPageAvailable;
                    //ebayItem.FreeAddedCategory.ProductSearchPageAvailableSpecified;
                    //ebayItem.FreeAddedCategory.SellerGuaranteeEligible;
                    //ebayItem.FreeAddedCategory.SellerGuaranteeEligibleSpecified;
                    //ebayItem.FreeAddedCategory.Virtual;
                    //ebayItem.FreeAddedCategory.VirtualSpecified;

                    //ebayItem.GetItFast;
                    //ebayItem.GetItFastSpecified;
                    //ebayItem.GroupCategoryID;
                    //ebayItem.HideFromSearch;
                    //ebayItem.HideFromSearchSpecified;
                    //ebayItem.HitCount;
                    //ebayItem.HitCounter;
                    //ebayItem.HitCounterSpecified;
                    //ebayItem.HitCountSpecified;
                    //ebayItem.IgnoreQuantity;
                    //ebayItem.IgnoreQuantitySpecified;
                    //ebayItem.IncludeRecommendations;
                    //ebayItem.IntegratedMerchantCreditCardEnabled;
                    //ebayItem.IntegratedMerchantCreditCardEnabledSpecified;
                    //ebayItem.InventoryTrackingMethod;
                    //ebayItem.InventoryTrackingMethodSpecified;
                    //ebayItem.IsIntermediatedShippingEligible;
                    //ebayItem.IsIntermediatedShippingEligibleSpecified;
                    //ebayItem.IsSecureDescription;
                    //ebayItem.IsSecureDescriptionSpecified;
                    //ebayItem.ItemCompatibilityCount;
                    //ebayItem.ItemCompatibilityCountSpecified;

                    //ebayItem.ItemCompatibilityList;
                    //ebayItem.ItemCompatibilityList.Any;
                    //ebayItem.ItemCompatibilityList.Compatibility;
                    //ebayItem.ItemCompatibilityList.ReplaceAll;
                    //ebayItem.ItemCompatibilityList.ReplaceAllSpecified;

                    //ebayItem.ItemPolicyViolation;
                    //ebayItem.ItemPolicyViolation.Any;
                    //ebayItem.ItemPolicyViolation.PolicyID;
                    //ebayItem.ItemPolicyViolation.PolicyIDSpecified;
                    //ebayItem.ItemPolicyViolation.PolicyText;

                    //ebayItem.ItemSpecifics.;
                    //ebayItem.LeadCount;
                    //ebayItem.LeadCountSpecified;
                    //ebayItem.LimitedWarrantyEligible;
                    //ebayItem.LimitedWarrantyEligibleSpecified;

                    //ebayItem.ListingDesigner;
                    //ebayItem.ListingDesigner.Any;
                    //ebayItem.ListingDesigner.LayoutID;
                    //ebayItem.ListingDesigner.LayoutIDSpecified;
                    //ebayItem.ListingDesigner.OptimalPictureSize;
                    //ebayItem.ListingDesigner.OptimalPictureSizeSpecified;
                    //ebayItem.ListingDesigner.ThemeID;
                    //ebayItem.ListingDesigner.ThemeIDSpecified;

                    //ebayItem.ListingDetails;
                    //ebayItem.ListingDetails.Adult;
                    //ebayItem.ListingDetails.AdultSpecified;
                    //ebayItem.ListingDetails.Any;
                    //ebayItem.ListingDetails.BestOfferAutoAcceptPrice;
                    //ebayItem.ListingDetails.BestOfferAutoAcceptPrice.currencyID;
                    //ebayItem.ListingDetails.BestOfferAutoAcceptPrice.Value;
                    //ebayItem.ListingDetails.BindingAuction;
                    //ebayItem.ListingDetails.BindingAuctionSpecified;
                    //ebayItem.ListingDetails.BuyItNowAvailable;
                    //ebayItem.ListingDetails.BuyItNowAvailableSpecified;
                    //ebayItem.ListingDetails.CheckoutEnabled;
                    //ebayItem.ListingDetails.CheckoutEnabledSpecified;
                    //ebayItem.ListingDetails.ConvertedBuyItNowPrice;
                    //ebayItem.ListingDetails.ConvertedBuyItNowPrice.currencyID;
                    //ebayItem.ListingDetails.ConvertedBuyItNowPrice.Value;
                    //ebayItem.ListingDetails.ConvertedReservePrice;
                    //ebayItem.ListingDetails.ConvertedReservePrice.currencyID;
                    //ebayItem.ListingDetails.ConvertedReservePrice.Value;
                    //ebayItem.ListingDetails.ConvertedStartPrice;
                    //ebayItem.ListingDetails.ConvertedStartPrice.currencyID;
                    //ebayItem.ListingDetails.ConvertedStartPrice.Value;
                    //ebayItem.ListingDetails.EndingReason;
                    //ebayItem.ListingDetails.EndingReasonSpecified;
                    //ebayItem.ListingDetails.EndTime;
                    //ebayItem.ListingDetails.EndTime.Ticks;
                    //ebayItem.ListingDetails.EndTime.Date;
                    //ebayItem.ListingDetails.EndTime.Year;
                    //ebayItem.ListingDetails.EndTime.Month;
                    //ebayItem.ListingDetails.EndTime.Day;
                    //ebayItem.ListingDetails.EndTime.Hour;
                    //ebayItem.ListingDetails.EndTime.Minute;
                    //ebayItem.ListingDetails.EndTime.Second;
                    //ebayItem.ListingDetails.EndTime.Millisecond;
                    //ebayItem.ListingDetails.EndTime.Kind;
                    //ebayItem.ListingDetails.EndTimeSpecified;
                    //ebayItem.ListingDetails.HasPublicMessages;
                    //ebayItem.ListingDetails.HasPublicMessagesSpecified;
                    //ebayItem.ListingDetails.HasReservePrice;
                    //ebayItem.ListingDetails.HasReservePriceSpecified;
                    //ebayItem.ListingDetails.HasUnansweredQuestions;
                    //ebayItem.ListingDetails.HasUnansweredQuestionsSpecified;
                    //ebayItem.ListingDetails.LocalListingDistance;
                    //ebayItem.ListingDetails.MinimumBestOfferMessage;
                    //ebayItem.ListingDetails.MinimumBestOfferPrice;
                    //ebayItem.ListingDetails.MinimumBestOfferPrice.currencyID;
                    //ebayItem.ListingDetails.MinimumBestOfferPrice.Value;
                    //ebayItem.ListingDetails.PayPerLeadEnabled;
                    //ebayItem.ListingDetails.PayPerLeadEnabledSpecified;
                    //ebayItem.ListingDetails.RelistedItemID;
                    //ebayItem.ListingDetails.SecondChanceOriginalItemID;
                    //ebayItem.ListingDetails.SellerBusinessType;
                    //ebayItem.ListingDetails.SellerBusinessTypeSpecified;
                    //ebayItem.ListingDetails.StartTime;
                    //ebayItem.ListingDetails.StartTime.Ticks;
                    //ebayItem.ListingDetails.StartTime.Date;
                    //ebayItem.ListingDetails.StartTime.Year;
                    //ebayItem.ListingDetails.StartTime.Month;
                    //ebayItem.ListingDetails.StartTime.Day;
                    //ebayItem.ListingDetails.StartTime.Hour;
                    //ebayItem.ListingDetails.StartTime.Minute;
                    //ebayItem.ListingDetails.StartTime.Second;
                    //ebayItem.ListingDetails.StartTime.Millisecond;
                    //ebayItem.ListingDetails.StartTime.Kind;
                    //ebayItem.ListingDetails.StartTimeSpecified;
                    //ebayItem.ListingDetails.TCROriginalItemID;
                    //ebayItem.ListingDetails.ViewItemURL;
                    //ebayItem.ListingDetails.ViewItemURLForNaturalSearch;

                    //ebayItem.ListingDuration;
                    //ebayItem.ListingEnhancement;
                    //ebayItem.ListingSubtype2;
                    //ebayItem.ListingSubtype2Specified;
                    //ebayItem.ListingType;
                    //ebayItem.ListingTypeSpecified;
                    //ebayItem.LiveAuction;
                    //ebayItem.LiveAuctionSpecified;
                    //ebayItem.LocalListing;
                    //ebayItem.LocalListingSpecified;
                    //ebayItem.Location;
                    //ebayItem.LocationDefaulted;
                    //ebayItem.LocationDefaultedSpecified;
                    //ebayItem.LookupAttributeArray;
                    //ebayItem.LotSize;
                    //ebayItem.LotSizeSpecified;
                    //ebayItem.MechanicalCheckAccepted;
                    //ebayItem.MechanicalCheckAcceptedSpecified;
                    //ebayItem.NewLeadCount;
                    //ebayItem.NewLeadCountSpecified;
                    //ebayItem.PartnerCode;
                    //ebayItem.PartnerName;
                    //ebayItem.PaymentAllowedSite;

                    //ebayItem.PaymentDetails;
                    //ebayItem.PaymentDetails.Any;
                    //ebayItem.PaymentDetails.DaysToFullPayment;
                    //ebayItem.PaymentDetails.DaysToFullPaymentSpecified;
                    //ebayItem.PaymentDetails.DepositAmount;
                    //ebayItem.PaymentDetails.DepositAmount.currencyID;
                    //ebayItem.PaymentDetails.DepositAmount.Value;
                    //ebayItem.PaymentDetails.DepositType;
                    //ebayItem.PaymentDetails.DepositTypeSpecified;
                    //ebayItem.PaymentDetails.HoursToDeposit;
                    //ebayItem.PaymentDetails.HoursToDepositSpecified;

                    //ebayItem.PaymentMethods;
                    //ebayItem.PayPalEmailAddress;

                    //ebayItem.PickupInStoreDetails;
                    //ebayItem.PickupInStoreDetails.Any;
                    //ebayItem.PickupInStoreDetails.EligibleForPickupDropOff;
                    //ebayItem.PickupInStoreDetails.EligibleForPickupDropOffSpecified;
                    //ebayItem.PickupInStoreDetails.EligibleForPickupInStore;
                    //ebayItem.PickupInStoreDetails.EligibleForPickupInStoreSpecified;

                    //ebayItem.PictureDetails;
                    //ebayItem.PictureDetails.Any;
                    //ebayItem.PictureDetails.ExtendedPictureDetails;
                    //ebayItem.PictureDetails.ExtendedPictureDetails.Any;
                    //ebayItem.PictureDetails.ExtendedPictureDetails.PictureURLs;
                    //ebayItem.PictureDetails.ExternalPictureURL;
                    //ebayItem.PictureDetails.GalleryErrorInfo;
                    //ebayItem.PictureDetails.GalleryStatus;
                    //ebayItem.PictureDetails.GalleryStatusSpecified;
                    //ebayItem.PictureDetails.GalleryType;
                    //ebayItem.PictureDetails.GalleryTypeSpecified;
                    //ebayItem.PictureDetails.GalleryURL;
                    //ebayItem.PictureDetails.PhotoDisplay;
                    //ebayItem.PictureDetails.PhotoDisplaySpecified;
                    //ebayItem.PictureDetails.PictureSource;
                    //ebayItem.PictureDetails.PictureSourceSpecified;
                    //ebayItem.PictureDetails.PictureURL;

                    //ebayItem.PostalCode;

                    //ebayItem.PrimaryCategory;
                    //ebayItem.PrimaryCategory.Any;
                    //ebayItem.PrimaryCategory.AutoPayEnabled;
                    //ebayItem.PrimaryCategory.AutoPayEnabledSpecified;
                    //ebayItem.PrimaryCategory.B2BVATEnabled;
                    //ebayItem.PrimaryCategory.B2BVATEnabledSpecified;
                    //ebayItem.PrimaryCategory.BestOfferEnabled;
                    //ebayItem.PrimaryCategory.BestOfferEnabledSpecified;
                    //ebayItem.PrimaryCategory.CatalogEnabled;
                    //ebayItem.PrimaryCategory.CatalogEnabledSpecified;
                    //ebayItem.PrimaryCategory.CategoryID;
                    //ebayItem.PrimaryCategory.CategoryLevel;
                    //ebayItem.PrimaryCategory.CategoryLevelSpecified;
                    //ebayItem.PrimaryCategory.CategoryName;
                    //ebayItem.PrimaryCategory.CategoryParentID;
                    //ebayItem.PrimaryCategory.CategoryParentName;
                    //ebayItem.PrimaryCategory.CharacteristicsSets;
                    //ebayItem.PrimaryCategory.Expired;
                    //ebayItem.PrimaryCategory.ExpiredSpecified;
                    //ebayItem.PrimaryCategory.IntlAutosFixedCat;
                    //ebayItem.PrimaryCategory.IntlAutosFixedCatSpecified;
                    //ebayItem.PrimaryCategory.Keywords;
                    //ebayItem.PrimaryCategory.LeafCategory;
                    //ebayItem.PrimaryCategory.LeafCategorySpecified;
                    //ebayItem.PrimaryCategory.LSD;
                    //ebayItem.PrimaryCategory.LSDSpecified;
                    //ebayItem.PrimaryCategory.NumOfItems;
                    //ebayItem.PrimaryCategory.NumOfItemsSpecified;
                    //ebayItem.PrimaryCategory.ORPA;
                    //ebayItem.PrimaryCategory.ORPASpecified;
                    //ebayItem.PrimaryCategory.ORRA;
                    //ebayItem.PrimaryCategory.ORRASpecified;
                    //ebayItem.PrimaryCategory.ProductFinderIDs;
                    //ebayItem.PrimaryCategory.ProductSearchPageAvailable;
                    //ebayItem.PrimaryCategory.ProductSearchPageAvailableSpecified;
                    //ebayItem.PrimaryCategory.SellerGuaranteeEligible;
                    //ebayItem.PrimaryCategory.SellerGuaranteeEligibleSpecified;
                    //ebayItem.PrimaryCategory.Virtual;
                    //ebayItem.PrimaryCategory.VirtualSpecified;

                    //ebayItem.PrivateListing;
                    //ebayItem.PrivateListingSpecified;
                    //ebayItem.PrivateNotes;

                    //ebayItem.ProductListingDetails;
                    //ebayItem.ProductListingDetails.Any;
                    //ebayItem.ProductListingDetails.BrandMPN;
                    //ebayItem.ProductListingDetails.BrandMPN.Brand;
                    //ebayItem.ProductListingDetails.BrandMPN.MPN;
                    //ebayItem.ProductListingDetails.Copyright;
                    //ebayItem.ProductListingDetails.DetailsURL;
                    //ebayItem.ProductListingDetails.EAN;
                    //ebayItem.ProductListingDetails.IncludeeBayProductDetails;
                    //ebayItem.ProductListingDetails.IncludeeBayProductDetailsSpecified;
                    //ebayItem.ProductListingDetails.IncludeStockPhotoURL;
                    //ebayItem.ProductListingDetails.IncludeStockPhotoURLSpecified;
                    //ebayItem.ProductListingDetails.ISBN;
                    //ebayItem.ProductListingDetails.NameValueList;
                    //ebayItem.ProductListingDetails.ProductDetailsURL;
                    //ebayItem.ProductListingDetails.ProductReferenceID;
                    //ebayItem.ProductListingDetails.ReturnSearchResultOnDuplicates;
                    //ebayItem.ProductListingDetails.ReturnSearchResultOnDuplicatesSpecified;
                    //ebayItem.ProductListingDetails.StockPhotoURL;
                    //ebayItem.ProductListingDetails.TicketListingDetails;
                    //ebayItem.ProductListingDetails.TicketListingDetails.Any;
                    //ebayItem.ProductListingDetails.TicketListingDetails.EventTitle;
                    //ebayItem.ProductListingDetails.TicketListingDetails.PrintedDate;
                    //ebayItem.ProductListingDetails.TicketListingDetails.PrintedTime;
                    //ebayItem.ProductListingDetails.TicketListingDetails.Venue;
                    //ebayItem.ProductListingDetails.UPC;
                    //ebayItem.ProductListingDetails.UseFirstProduct;
                    //ebayItem.ProductListingDetails.UseFirstProductSpecified;
                    //ebayItem.ProductListingDetails.UseStockPhotoURLAsGallery;
                    //ebayItem.ProductListingDetails.UseStockPhotoURLAsGallerySpecified;

                    //ebayItem.ProxyItem;
                    //ebayItem.ProxyItemSpecified;
                    //ebayItem.Quantity;
                    //ebayItem.QuantityAvailable;
                    //ebayItem.QuantityAvailableHint;
                    //ebayItem.QuantityAvailableHintSpecified;
                    //ebayItem.QuantityAvailableSpecified;

                    //ebayItem.QuantityInfo;
                    //ebayItem.QuantityInfo.Any;
                    //ebayItem.QuantityInfo.MinimumRemnantSet;
                    //ebayItem.QuantityInfo.MinimumRemnantSetSpecified;

                    //ebayItem.QuantityRestrictionPerBuyer;
                    //ebayItem.QuantityRestrictionPerBuyer.MaximumQuantity;
                    //ebayItem.QuantityRestrictionPerBuyer.MaximumQuantitySpecified;

                    //ebayItem.QuantitySpecified;
                    //ebayItem.QuantityThreshold;
                    //ebayItem.QuantityThresholdSpecified;
                    //ebayItem.QuestionCount;
                    //ebayItem.QuestionCountSpecified;
                    //ebayItem.ReasonHideFromSearch;
                    //ebayItem.ReasonHideFromSearchSpecified;
                    //ebayItem.RegionID;
                    //ebayItem.Relisted;
                    //ebayItem.RelistedSpecified;
                    //ebayItem.RelistLink;
                    //ebayItem.RelistLinkSpecified;
                    //ebayItem.RelistParentID;
                    //ebayItem.RelistParentIDSpecified;

                    //ebayItem.ReservePrice;
                    //ebayItem.ReservePrice.currencyID;
                    //ebayItem.ReservePrice.Value;

                    //ebayItem.ReturnPolicy;
                    //ebayItem.ReturnPolicy.Any;
                    //ebayItem.ReturnPolicy.Description;
                    //ebayItem.ReturnPolicy.ExtendedHolidayReturns;
                    //ebayItem.ReturnPolicy.ExtendedHolidayReturnsSpecified;
                    //ebayItem.ReturnPolicy.InternationalRefundOption;
                    //ebayItem.ReturnPolicy.InternationalReturnsAcceptedOption;
                    //ebayItem.ReturnPolicy.InternationalReturnsWithinOption;
                    //ebayItem.ReturnPolicy.InternationalShippingCostPaidByOption;
                    //ebayItem.ReturnPolicy.Refund;
                    //ebayItem.ReturnPolicy.RefundOption;
                    //ebayItem.ReturnPolicy.RestockingFeeValue;
                    //ebayItem.ReturnPolicy.RestockingFeeValueOption;
                    //ebayItem.ReturnPolicy.ReturnsAccepted;
                    //ebayItem.ReturnPolicy.ReturnsAcceptedOption;
                    //ebayItem.ReturnPolicy.ReturnsWithin;
                    //ebayItem.ReturnPolicy.ReturnsWithinOption;
                    //ebayItem.ReturnPolicy.ShippingCostPaidBy;
                    //ebayItem.ReturnPolicy.ShippingCostPaidByOption;
                    //ebayItem.ReturnPolicy.WarrantyDuration;
                    //ebayItem.ReturnPolicy.WarrantyDurationOption;
                    //ebayItem.ReturnPolicy.WarrantyOffered;
                    //ebayItem.ReturnPolicy.WarrantyOfferedOption;
                    //ebayItem.ReturnPolicy.WarrantyType;
                    //ebayItem.ReturnPolicy.WarrantyTypeOption;

                    //ebayItem.ReviseStatus;
                    //ebayItem.ReviseStatus.Any;
                    //ebayItem.ReviseStatus.BuyItNowAdded;
                    //ebayItem.ReviseStatus.BuyItNowAddedSpecified;
                    //ebayItem.ReviseStatus.BuyItNowLowered;
                    //ebayItem.ReviseStatus.BuyItNowLoweredSpecified;
                    //ebayItem.ReviseStatus.ItemRevised;
                    //ebayItem.ReviseStatus.ReserveLowered;
                    //ebayItem.ReviseStatus.ReserveLoweredSpecified;
                    //ebayItem.ReviseStatus.ReserveRemoved;
                    //ebayItem.ReviseStatus.ReserveRemovedSpecified;

                    //ebayItem.ScheduleTime;
                    //ebayItem.ScheduleTime.Ticks;
                    //ebayItem.ScheduleTime.Date;
                    //ebayItem.ScheduleTime.Year;
                    //ebayItem.ScheduleTime.Month;
                    //ebayItem.ScheduleTime.Day;
                    //ebayItem.ScheduleTime.Hour;
                    //ebayItem.ScheduleTime.Minute;
                    //ebayItem.ScheduleTime.Second;
                    //ebayItem.ScheduleTime.Millisecond;
                    //ebayItem.ScheduleTime.Kind;
                    //ebayItem.ScheduleTimeSpecified;

                    //ebayItem.SearchDetails;
                    //ebayItem.SearchDetails.Any;
                    //ebayItem.SearchDetails.BuyItNowEnabled;
                    //ebayItem.SearchDetails.BuyItNowEnabledSpecified;
                    //ebayItem.SearchDetails.Picture;
                    //ebayItem.SearchDetails.PictureSpecified;
                    //ebayItem.SearchDetails.RecentListing;
                    //ebayItem.SearchDetails.RecentListingSpecified;

                    //ebayItem.SecondaryCategory;
                    //ebayItem.SecondaryCategory.Any;
                    //ebayItem.SecondaryCategory.AutoPayEnabled;
                    //ebayItem.SecondaryCategory.AutoPayEnabledSpecified;
                    //ebayItem.SecondaryCategory.B2BVATEnabled;
                    //ebayItem.SecondaryCategory.B2BVATEnabledSpecified;
                    //ebayItem.SecondaryCategory.BestOfferEnabled;
                    //ebayItem.SecondaryCategory.BestOfferEnabledSpecified;
                    //ebayItem.SecondaryCategory.CatalogEnabled;
                    //ebayItem.SecondaryCategory.CatalogEnabledSpecified;
                    //ebayItem.SecondaryCategory.CategoryID;
                    //ebayItem.SecondaryCategory.CategoryLevel;
                    //ebayItem.SecondaryCategory.CategoryLevelSpecified;
                    //ebayItem.SecondaryCategory.CategoryName;
                    //ebayItem.SecondaryCategory.CategoryParentID;
                    //ebayItem.SecondaryCategory.CategoryParentName;
                    //ebayItem.SecondaryCategory.CharacteristicsSets;
                    //ebayItem.SecondaryCategory.Expired;
                    //ebayItem.SecondaryCategory.ExpiredSpecified;
                    //ebayItem.SecondaryCategory.IntlAutosFixedCat;
                    //ebayItem.SecondaryCategory.IntlAutosFixedCatSpecified;
                    //ebayItem.SecondaryCategory.Keywords;
                    //ebayItem.SecondaryCategory.LeafCategory;
                    //ebayItem.SecondaryCategory.LeafCategorySpecified;
                    //ebayItem.SecondaryCategory.LSD;
                    //ebayItem.SecondaryCategory.LSDSpecified;
                    //ebayItem.SecondaryCategory.NumOfItems;
                    //ebayItem.SecondaryCategory.NumOfItemsSpecified;
                    //ebayItem.SecondaryCategory.ORPA;
                    //ebayItem.SecondaryCategory.ORPASpecified;
                    //ebayItem.SecondaryCategory.ORRA;
                    //ebayItem.SecondaryCategory.ORRASpecified;
                    //ebayItem.SecondaryCategory.ProductFinderIDs;
                    //ebayItem.SecondaryCategory.ProductSearchPageAvailable;
                    //ebayItem.SecondaryCategory.ProductSearchPageAvailableSpecified;
                    //ebayItem.SecondaryCategory.SellerGuaranteeEligible;
                    //ebayItem.SecondaryCategory.SellerGuaranteeEligibleSpecified;
                    //ebayItem.SecondaryCategory.Virtual;
                    //ebayItem.SecondaryCategory.VirtualSpecified;

                    //ebayItem.Seller;
                    //ebayItem.Seller.AboutMePage;
                    //ebayItem.Seller.AboutMePageSpecified;
                    //ebayItem.Seller.Any;
                    //ebayItem.Seller.BiddingSummary;
                    //ebayItem.Seller.BiddingSummary.Any;
                    //ebayItem.Seller.BiddingSummary.BidActivityWithSeller;
                    //ebayItem.Seller.BiddingSummary.BidActivityWithSellerSpecified;
                    //ebayItem.Seller.BiddingSummary.BidRetractions;
                    //ebayItem.Seller.BiddingSummary.BidRetractionsSpecified;
                    //ebayItem.Seller.BiddingSummary.BidsToUniqueCategories;
                    //ebayItem.Seller.BiddingSummary.BidsToUniqueCategoriesSpecified;
                    //ebayItem.Seller.BiddingSummary.BidsToUniqueSellers;
                    //ebayItem.Seller.BiddingSummary.BidsToUniqueSellersSpecified;
                    //ebayItem.Seller.BiddingSummary.ItemBidDetails;
                    //ebayItem.Seller.BiddingSummary.SummaryDays;
                    //ebayItem.Seller.BiddingSummary.SummaryDaysSpecified;
                    //ebayItem.Seller.BiddingSummary.TotalBids;
                    //ebayItem.Seller.BiddingSummary.TotalBidsSpecified;

                    //ebayItem.SellerContactDetails;
                    //ebayItem.SellerContactDetails.AddressAttribute;
                    //ebayItem.SellerContactDetails.AddressID;
                    //ebayItem.SellerContactDetails.AddressOwner;
                    //ebayItem.SellerContactDetails.AddressOwnerSpecified;
                    //ebayItem.SellerContactDetails.AddressRecordType;
                    //ebayItem.SellerContactDetails.AddressRecordTypeSpecified;
                    //ebayItem.SellerContactDetails.AddressStatus;
                    //ebayItem.SellerContactDetails.AddressStatusSpecified;
                    //ebayItem.SellerContactDetails.AddressUsage;
                    //ebayItem.SellerContactDetails.AddressUsageSpecified;
                    //ebayItem.SellerContactDetails.Any;
                    //ebayItem.SellerContactDetails.CityName;
                    //ebayItem.SellerContactDetails.CompanyName;
                    //ebayItem.SellerContactDetails.Country;
                    //ebayItem.SellerContactDetails.CountryName;
                    //ebayItem.SellerContactDetails.CountrySpecified;
                    //ebayItem.SellerContactDetails.County;
                    //ebayItem.SellerContactDetails.ExternalAddressID;
                    //ebayItem.SellerContactDetails.FirstName;
                    //ebayItem.SellerContactDetails.InternationalName;
                    //ebayItem.SellerContactDetails.InternationalStateAndCity;
                    //ebayItem.SellerContactDetails.InternationalStreet;
                    //ebayItem.SellerContactDetails.LastName;
                    //ebayItem.SellerContactDetails.Name;
                    //ebayItem.SellerContactDetails.Phone;
                    //ebayItem.SellerContactDetails.Phone2;
                    //ebayItem.SellerContactDetails.PhoneAreaOrCityCode;
                    //ebayItem.SellerContactDetails.PhoneCountryCode;
                    //ebayItem.SellerContactDetails.PhoneCountryCodeSpecified;
                    //ebayItem.SellerContactDetails.PhoneCountryPrefix;
                    //ebayItem.SellerContactDetails.PhoneLocalNumber;
                    //ebayItem.SellerContactDetails.PostalCode;
                    //ebayItem.SellerContactDetails.ReferenceID;
                    //ebayItem.SellerContactDetails.StateOrProvince;
                    //ebayItem.SellerContactDetails.Street;
                    //ebayItem.SellerContactDetails.Street1;
                    //ebayItem.SellerContactDetails.Street2;

                    //ebayItem.SellerProfiles;
                    //ebayItem.SellerProfiles.Any;
                    //ebayItem.SellerProfiles.SellerPaymentProfile;
                    //ebayItem.SellerProfiles.SellerPaymentProfile.Any;
                    //ebayItem.SellerProfiles.SellerPaymentProfile.PaymentProfileID;
                    //ebayItem.SellerProfiles.SellerPaymentProfile.PaymentProfileIDSpecified;
                    //ebayItem.SellerProfiles.SellerPaymentProfile.PaymentProfileName;
                    //ebayItem.SellerProfiles.SellerReturnProfile;
                    //ebayItem.SellerProfiles.SellerReturnProfile.Any;
                    //ebayItem.SellerProfiles.SellerReturnProfile.ReturnProfileID;
                    //ebayItem.SellerProfiles.SellerReturnProfile.ReturnProfileIDSpecified;
                    //ebayItem.SellerProfiles.SellerReturnProfile.ReturnProfileName;
                    //ebayItem.SellerProfiles.SellerShippingProfile.Any;
                    //ebayItem.SellerProfiles.SellerShippingProfile.ShippingProfileID;
                    //ebayItem.SellerProfiles.SellerShippingProfile.ShippingProfileIDSpecified;
                    //ebayItem.SellerProfiles.SellerShippingProfile.ShippingProfileName;

                    //ebayItem.SellerProvidedTitle;
                    //ebayItem.SellerVacationNote;

                    //ebayItem.SellingStatus;
                    //ebayItem.SellingStatus.AdminEnded;
                    //ebayItem.SellingStatus.AdminEndedSpecified;
                    //ebayItem.SellingStatus.Any;
                    //ebayItem.SellingStatus.BidCount;
                    //ebayItem.SellingStatus.BidCountSpecified;
                    //ebayItem.SellingStatus.BidderCount;
                    //ebayItem.SellingStatus.BidderCountSpecified;
                    //ebayItem.SellingStatus.BidIncrement;
                    //ebayItem.SellingStatus.BidIncrement.currencyID;
                    //ebayItem.SellingStatus.BidIncrement.Value;
                    //ebayItem.SellingStatus.ConvertedCurrentPrice;
                    //ebayItem.SellingStatus.ConvertedCurrentPrice.currencyID;
                    //ebayItem.SellingStatus.ConvertedCurrentPrice.Value;
                    //ebayItem.SellingStatus.CurrentPrice;
                    //ebayItem.SellingStatus.CurrentPrice.currencyID;
                    //ebayItem.SellingStatus.CurrentPrice.Value;
                    //ebayItem.SellingStatus.FinalValueFee;
                    //ebayItem.SellingStatus.FinalValueFee.currencyID;
                    //ebayItem.SellingStatus.FinalValueFee.Value;
                    //ebayItem.SellingStatus.HighBidder;
                    //ebayItem.SellingStatus.HighBidder.AboutMePage;
                    //ebayItem.SellingStatus.HighBidder.AboutMePageSpecified;
                    //ebayItem.SellingStatus.HighBidder.Any;
                    //ebayItem.SellingStatus.HighBidder.BiddingSummary.Any;
                    //ebayItem.SellingStatus.HighBidder.BiddingSummary.BidActivityWithSeller;
                    //ebayItem.SellingStatus.HighBidder.BiddingSummary.BidActivityWithSellerSpecified;
                    //ebayItem.SellingStatus.HighBidder.BiddingSummary.BidRetractions;
                    //ebayItem.SellingStatus.HighBidder.BiddingSummary.BidRetractionsSpecified;
                    //ebayItem.SellingStatus.HighBidder.BiddingSummary.BidsToUniqueCategories;
                    //ebayItem.SellingStatus.HighBidder.BiddingSummary.BidsToUniqueCategoriesSpecified;
                    //ebayItem.SellingStatus.HighBidder.BiddingSummary.BidsToUniqueSellers;
                    //ebayItem.SellingStatus.HighBidder.BiddingSummary.BidsToUniqueSellersSpecified;
                    //ebayItem.SellingStatus.HighBidder.BiddingSummary.ItemBidDetails;
                    //ebayItem.SellingStatus.HighBidder.BiddingSummary.SummaryDays;
                    //ebayItem.SellingStatus.HighBidder.BiddingSummary.SummaryDaysSpecified;
                    //ebayItem.SellingStatus.HighBidder.BiddingSummary.TotalBids;
                    //ebayItem.SellingStatus.HighBidder.BiddingSummary.TotalBidsSpecified;
                    //ebayItem.SellingStatus.HighBidder.BillingEmail;
                    //ebayItem.SellingStatus.HighBidder.BusinessRole;
                    //ebayItem.SellingStatus.HighBidder.BusinessRoleSpecified;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.Any;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.BuyerTaxIdentifier;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressAttribute;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressID;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressOwner;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressOwnerSpecified;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressRecordType;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressRecordTypeSpecified;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressStatus;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressStatusSpecified;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressUsage;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.AddressUsageSpecified;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Any;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CityName;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CompanyName;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Country;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CountryName;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.CountrySpecified;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.County;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.ExternalAddressID;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.FirstName;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalName;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalStateAndCity;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.InternationalStreet;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.LastName;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Name;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Phone;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Phone2;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneAreaOrCityCode;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneCountryCode;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneCountryCodeSpecified;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneCountryPrefix;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PhoneLocalNumber;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.PostalCode;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.ReferenceID;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.StateOrProvince;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street1;
                    //ebayItem.SellingStatus.HighBidder.BuyerInfo.ShippingAddress.Street2;
                    //ebayItem.SellingStatus.HighBidder.CharityAffiliations;
                    //ebayItem.SellingStatus.HighBidder.CharityAffiliations.Any;
                    //ebayItem.SellingStatus.HighBidder.CharityAffiliations.CharityID;
                    //ebayItem.SellingStatus.HighBidder.eBayGoodStanding;
                    //ebayItem.SellingStatus.HighBidder.eBayGoodStandingSpecified;
                    //ebayItem.SellingStatus.HighBidder.eBayWikiReadOnly;
                    //ebayItem.SellingStatus.HighBidder.eBayWikiReadOnlySpecified;
                    //ebayItem.SellingStatus.HighBidder.EIASToken;
                    //ebayItem.SellingStatus.HighBidder.Email;
                    //ebayItem.SellingStatus.HighBidder.EnterpriseSeller;
                    //ebayItem.SellingStatus.HighBidder.EnterpriseSellerSpecified;
                    //ebayItem.SellingStatus.HighBidder.FeedbackPrivate;
                    //ebayItem.SellingStatus.HighBidder.FeedbackPrivateSpecified;
                    //ebayItem.SellingStatus.HighBidder.FeedbackRatingStar;
                    //ebayItem.SellingStatus.HighBidder.FeedbackRatingStarSpecified;
                    //ebayItem.SellingStatus.HighBidder.FeedbackScore;
                    //ebayItem.SellingStatus.HighBidder.FeedbackScoreSpecified;
                    //ebayItem.SellingStatus.HighBidder.IDVerified;
                    //ebayItem.SellingStatus.HighBidder.IDVerifiedSpecified;
                    //ebayItem.SellingStatus.HighBidder.Membership;
                    //ebayItem.SellingStatus.HighBidder.NewUser;
                    //ebayItem.SellingStatus.HighBidder.NewUserSpecified;
                    //ebayItem.SellingStatus.HighBidder.PayPalAccountLevel;
                    //ebayItem.SellingStatus.HighBidder.PayPalAccountLevelSpecified;
                    //ebayItem.SellingStatus.HighBidder.PayPalAccountStatus;
                    //ebayItem.SellingStatus.HighBidder.PayPalAccountStatusSpecified;
                    //ebayItem.SellingStatus.HighBidder.PayPalAccountType;
                    //ebayItem.SellingStatus.HighBidder.PayPalAccountTypeSpecified;
                    //ebayItem.SellingStatus.HighBidder.PositiveFeedbackPercent;
                    //ebayItem.SellingStatus.HighBidder.PositiveFeedbackPercentSpecified;
                    //ebayItem.SellingStatus.HighBidder.QualifiesForSelling;
                    //ebayItem.SellingStatus.HighBidder.QualifiesForSellingSpecified;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressAttribute;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressID;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressOwner;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressOwnerSpecified;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressRecordType;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressRecordTypeSpecified;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressStatus;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressStatusSpecified;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressUsage;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.AddressUsageSpecified;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.Any;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.CityName;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.CompanyName;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.Country;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.CountryName;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.CountrySpecified;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.County;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.ExternalAddressID;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.FirstName;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.InternationalName;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.InternationalStateAndCity;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.InternationalStreet;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.LastName;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.Name;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.Phone;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.Phone2;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.PhoneAreaOrCityCode;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.PhoneCountryCode;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.PhoneCountryCodeSpecified;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.PhoneCountryPrefix;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.PhoneLocalNumber;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.PostalCode;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.ReferenceID;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.StateOrProvince;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.Street;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.Street1;
                    //ebayItem.SellingStatus.HighBidder.RegistrationAddress.Street2;
                    //ebayItem.SellingStatus.HighBidder.RegistrationDate.Ticks;
                    //ebayItem.SellingStatus.HighBidder.RegistrationDate.Date;
                    //ebayItem.SellingStatus.HighBidder.RegistrationDate.Year;
                    //ebayItem.SellingStatus.HighBidder.RegistrationDate.Month;
                    //ebayItem.SellingStatus.HighBidder.RegistrationDate.Day;
                    //ebayItem.SellingStatus.HighBidder.RegistrationDate.Hour;
                    //ebayItem.SellingStatus.HighBidder.RegistrationDate.Minute;
                    //ebayItem.SellingStatus.HighBidder.RegistrationDate.Second;
                    //ebayItem.SellingStatus.HighBidder.RegistrationDate.Millisecond;
                    //ebayItem.SellingStatus.HighBidder.RegistrationDate.Kind;
                    //ebayItem.SellingStatus.HighBidder.RegistrationDate.DayOfWeek;
                    //ebayItem.SellingStatus.HighBidder.RegistrationDate.DayOfYear;
                    //ebayItem.SellingStatus.HighBidder.RegistrationDateSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.AllowPaymentEdit;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.Any;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.BillingCurrency;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.BillingCurrencySpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.CharityAffiliationDetails;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.CharityRegistered;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.CharityRegisteredSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.CheckoutEnabled;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.CIPBankAccountStored;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.DomesticRateTable;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.DomesticRateTableSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.Any;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForAuctionOneDayDuration;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForAuctionOneDayDurationSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForFixedPriceOneDayDuration;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiedForFixedPriceOneDayDurationSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNow;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowMultiple;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowMultipleSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForBuyItNowSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForVariations;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.FeatureEligibility.QualifiesForVariationsSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.GoodStanding;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo.Any;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.IntegratedMerchantCreditCardInfo.SupportedSite;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.InternationalRateTable;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.InternationalRateTableSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.MerchandizingPref;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.MerchandizingPrefSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.PaisaPayEscrowEMIStatus;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.PaisaPayEscrowEMIStatusSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.PaisaPayStatus;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.PaisaPayStatusSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.PaymentMethod;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.PaymentMethodSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.Any;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.CheckoutRedirectProStores;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.CheckoutRedirectProStoresSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.Any;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.SellerThirdPartyUsername;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.Status;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.StatusSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.ProStoresPreference.ProStoresDetails.StoreName;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.QualifiesForB2BVAT;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent.Any;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.RecoupmentPolicyConsent.Site;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.RegisteredBusinessSeller;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.RegisteredBusinessSellerSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SafePaymentExempt;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SafePaymentExemptSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.Any;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledItems;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledItemsSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledMinutes;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MaxScheduledMinutesSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MinScheduledMinutes;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SchedulingInfo.MinScheduledMinutesSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerBusinessType;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerBusinessTypeSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.Any;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethod;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSet;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSetSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.PayoutMethodSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessConsent.UserAgreementInfo;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessStatus;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellereBayPaymentProcessStatusSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerGuaranteeLevel;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerGuaranteeLevelSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerLevel;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerLevelSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressAttribute;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressID;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressOwner;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressOwnerSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressRecordType;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressRecordTypeSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressStatus;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressStatusSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressUsage;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.AddressUsageSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Any;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CityName;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CompanyName;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Country;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CountryName;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.CountrySpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.County;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.ExternalAddressID;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.FirstName;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalName;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalStateAndCity;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.InternationalStreet;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.LastName;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Name;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Phone;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Phone2;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneAreaOrCityCode;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneCountryCode;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneCountryCodeSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneCountryPrefix;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PhoneLocalNumber;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.PostalCode;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.ReferenceID;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.StateOrProvince;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street1;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.SellerPaymentAddress.Street2;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.StoreOwner;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.StoreSite;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.StoreSiteSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.StoreURL;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.TopRatedSeller;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails.Any;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.TopRatedSellerDetails.TopRatedProgram;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.TopRatedSellerSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.TransactionPercent;
                    //ebayItem.SellingStatus.HighBidder.SellerInfo.TransactionPercentSpecified;
                    //ebayItem.SellingStatus.HighBidder.SellerPaymentMethod;
                    //ebayItem.SellingStatus.HighBidder.SellerPaymentMethodSpecified;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressAttribute;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressID;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressOwner;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressOwnerSpecified;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressRecordType;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressRecordTypeSpecified;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressStatus;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressStatusSpecified;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressUsage;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.AddressUsageSpecified;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.Any;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.CityName;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.CompanyName;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.Country;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.CountryName;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.CountrySpecified;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.County;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.ExternalAddressID;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.FirstName;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.InternationalName;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.InternationalStateAndCity;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.InternationalStreet;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.LastName;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.Name;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.Phone;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.Phone2;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.PhoneAreaOrCityCode;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.PhoneCountryCode;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.PhoneCountryCodeSpecified;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.PhoneCountryPrefix;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.PhoneLocalNumber;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.PostalCode;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.ReferenceID;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.StateOrProvince;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.Street;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.Street1;
                    //ebayItem.SellingStatus.HighBidder.ShippingAddress.Street2;
                    //ebayItem.SellingStatus.HighBidder.Site;
                    //ebayItem.SellingStatus.HighBidder.SiteSpecified;
                    //ebayItem.SellingStatus.HighBidder.SiteVerified;
                    //ebayItem.SellingStatus.HighBidder.SiteVerifiedSpecified;
                    //ebayItem.SellingStatus.HighBidder.SkypeID;
                    //ebayItem.SellingStatus.HighBidder.StaticAlias;
                    //ebayItem.SellingStatus.HighBidder.Status;
                    //ebayItem.SellingStatus.HighBidder.StatusSpecified;
                    //ebayItem.SellingStatus.HighBidder.TUVLevel;
                    //ebayItem.SellingStatus.HighBidder.TUVLevelSpecified;
                    //ebayItem.SellingStatus.HighBidder.UniqueNegativeFeedbackCount;
                    //ebayItem.SellingStatus.HighBidder.UniqueNegativeFeedbackCountSpecified;
                    //ebayItem.SellingStatus.HighBidder.UniqueNeutralFeedbackCount;
                    //ebayItem.SellingStatus.HighBidder.UniqueNeutralFeedbackCountSpecified;
                    //ebayItem.SellingStatus.HighBidder.UniquePositiveFeedbackCount;
                    //ebayItem.SellingStatus.HighBidder.UniquePositiveFeedbackCountSpecified;
                    //ebayItem.SellingStatus.HighBidder.UserAnonymized;
                    //ebayItem.SellingStatus.HighBidder.UserAnonymizedSpecified;
                    //ebayItem.SellingStatus.HighBidder.UserFirstName;
                    //ebayItem.SellingStatus.HighBidder.UserID;
                    //ebayItem.SellingStatus.HighBidder.UserIDChanged;
                    //ebayItem.SellingStatus.HighBidder.UserIDChangedSpecified;
                    //ebayItem.SellingStatus.HighBidder.UserIDLastChanged.Ticks;
                    //ebayItem.SellingStatus.HighBidder.UserIDLastChanged.Date;
                    //ebayItem.SellingStatus.HighBidder.UserIDLastChanged.Year;
                    //ebayItem.SellingStatus.HighBidder.UserIDLastChanged.Month;
                    //ebayItem.SellingStatus.HighBidder.UserIDLastChanged.Day;
                    //ebayItem.SellingStatus.HighBidder.UserIDLastChanged.Hour;
                    //ebayItem.SellingStatus.HighBidder.UserIDLastChanged.Minute;
                    //ebayItem.SellingStatus.HighBidder.UserIDLastChanged.Second;
                    //ebayItem.SellingStatus.HighBidder.UserIDLastChanged.Millisecond;
                    //ebayItem.SellingStatus.HighBidder.UserIDLastChanged.Kind;
                    //ebayItem.SellingStatus.HighBidder.UserIDLastChanged.DayOfWeek;
                    //ebayItem.SellingStatus.HighBidder.UserIDLastChanged.DayOfYear;
                    //ebayItem.SellingStatus.HighBidder.UserIDLastChanged.TimeOfDay;
                    //ebayItem.SellingStatus.HighBidder.UserIDChangedSpecified;
                    //ebayItem.SellingStatus.HighBidder.UserLastName;
                    //ebayItem.SellingStatus.HighBidder.UserSubscription;
                    //ebayItem.SellingStatus.HighBidder.VATID;
                    //ebayItem.SellingStatus.HighBidder.VATStatus;
                    //ebayItem.SellingStatus.HighBidder.VATStatusSpecified;
                    //ebayItem.SellingStatus.LeadCount;
                    //ebayItem.SellingStatus.LeadCountSpecified;
                    //ebayItem.SellingStatus.ListingStatus;
                    //ebayItem.SellingStatus.ListingStatusSpecified;
                    //ebayItem.SellingStatus.MinimumToBid;
                    //ebayItem.SellingStatus.MinimumToBid.currencyID;
                    //ebayItem.SellingStatus.MinimumToBid.Value;
                    //ebayItem.SellingStatus.PromotionalSaleDetails;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.Any;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.EndTime;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.Endtime.Ticks;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.Endtime.Date;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.Endtime.Year;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.Endtime.Month;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.Endtime.Day;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.Endtime.Hour;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.Endtime.Minute;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.Endtime.Second;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.Endtime.Millisecond;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.Endtime.Kind;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.Endtime.DayOfWeek;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.Endtime.DayOfYear;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.Endtime.TimeOfDay;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.EndTimeSpecified;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.OriginalPrice;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.OriginalPrice.currencyID;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.OriginalPrice.Value;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime.Ticks;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime.Date;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime.Year;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime.Month;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime.Day;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime.Hour;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime.Minute;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime.Second;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime.Millisecond;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime.Kind;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime.DayOfWeek;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime.DayOfYear;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTime.TimeOfDay;
                    //ebayItem.SellingStatus.PromotionalSaleDetails.StartTimeSpecified;
                    //ebayItem.SellingStatus.QuantitySold;
                    //ebayItem.SellingStatus.QuantitySoldByPickupInStore;
                    //ebayItem.SellingStatus.QuantitySoldByPickupInStoreSpecified;
                    //ebayItem.SellingStatus.QuantitySoldSpecified;
                    //ebayItem.SellingStatus.ReserveMet;
                    //ebayItem.SellingStatus.ReserveMetSpecified;
                    //ebayItem.SellingStatus.SecondChanceEligible;
                    //ebayItem.SellingStatus.SecondChanceEligibleSpecified;
                    //ebayItem.SellingStatus.SoldAsBin;
                    //ebayItem.SellingStatus.SoldAsBinSpecified;
                    //ebayItem.SellingStatus.SuggestedBidValues;
                    //ebayItem.SellingStatus.SuggestedBidValues.Any;
                    //ebayItem.SellingStatus.SuggestedBidValues.BidValue;



                    //ebayItem.ShippingDetails.AllowPaymentEdit;
                    //ebayItem.ShippingDetails.AllowPaymentEditSpecified;
                    //ebayItem.ShippingDetails.Any;
                    //ebayItem.ShippingDetails.ApplyShippingDiscount;
                    //ebayItem.ShippingDetails.ApplyShippingDiscountSpecified;
                    //ebayItem.ShippingDetails.CalculatedShippingDiscount;
                    //ebayItem.ShippingDetails.CalculatedShippingDiscount.Any;
                    //ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountName;
                    //ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountNameSpecified;
                    //ebayItem.ShippingDetails.CalculatedShippingDiscount.DiscountProfile;
                    //ebayItem.ShippingDetails.CalculatedShippingRate;
                    //ebayItem.ShippingDetails.CalculatedShippingRate.Any;
                    //ebayItem.ShippingDetails.CalculatedShippingRate.InternationalPackagingHandlingCosts;
                    //ebayItem.ShippingDetails.CalculatedShippingRate.InternationalPackagingHandlingCosts.currencyID;
                    //ebayItem.ShippingDetails.CalculatedShippingRate.InternationalPackagingHandlingCosts.Value;
                    //ebayItem.ShippingDetails.CalculatedShippingRate.MeasurementUnit;
                    //ebayItem.ShippingDetails.CalculatedShippingRate.MeasurementUnitSpecified;
                    //ebayItem.ShippingDetails.CalculatedShippingRate.OriginatingPostalCode;
                    //ebayItem.ShippingDetails.CalculatedShippingRate.PackagingHandlingCosts;
                    //ebayItem.ShippingDetails.CalculatedShippingRate.PackagingHandlingCosts.currencyID;
                    //ebayItem.ShippingDetails.CalculatedShippingRate.PackagingHandlingCosts.Value;
                    //ebayItem.ShippingDetails.CalculatedShippingRate.ShippingIrregular;
                    //ebayItem.ShippingDetails.CalculatedShippingRate.ShippingIrregularSpecified;
                    //ebayItem.ShippingDetails.ChangePaymentInstructions;
                    //ebayItem.ShippingDetails.ChangePaymentInstructionsSpecified;
                    //ebayItem.ShippingDetails.CODCost;
                    //ebayItem.ShippingDetails.CODCost.currencyID;
                    //ebayItem.ShippingDetails.CODCost.Value;
                    //ebayItem.ShippingDetails.DefaultShippingCost;
                    //ebayItem.ShippingDetails.DefaultShippingCost.currencyID;
                    //ebayItem.ShippingDetails.DefaultShippingCost.Value;
                    //ebayItem.ShippingDetails.ExcludeShipToLocation;
                    //ebayItem.ShippingDetails.FlatShippingDiscount;
                    //ebayItem.ShippingDetails.FlatShippingDiscount.Any;
                    //ebayItem.ShippingDetails.FlatShippingDiscount.DiscountName;
                    //ebayItem.ShippingDetails.FlatShippingDiscount.DiscountNameSpecified;
                    //ebayItem.ShippingDetails.FlatShippingDiscount.DiscountProfile;
                    //ebayItem.ShippingDetails.GetItFast;
                    //ebayItem.ShippingDetails.GetItFastSpecified;
                    //ebayItem.ShippingDetails.GlobalShipping;
                    //ebayItem.ShippingDetails.GlobalShippingSpecified;
                    //ebayItem.ShippingDetails.InsuranceWanted;
                    //ebayItem.ShippingDetails.InsuranceWantedSpecified;
                    //ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount;
                    //ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountName;
                    //ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountNameSpecified;
                    //ebayItem.ShippingDetails.InternationalCalculatedShippingDiscount.DiscountProfile;
                    //ebayItem.ShippingDetails.InternationalFlatShippingDiscount;
                    //ebayItem.ShippingDetails.InternationalFlatShippingDiscount.Any;
                    //ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountName;
                    //ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountNameSpecified;
                    //ebayItem.ShippingDetails.InternationalFlatShippingDiscount.DiscountProfile;
                    //ebayItem.ShippingDetails.InternationalPromotionalShippingDiscount;
                    //ebayItem.ShippingDetails.InternationalPromotionalShippingDiscountSpecified;
                    //ebayItem.ShippingDetails.InternationalShippingDiscountProfileID;
                    //ebayItem.ShippingDetails.InternationalShippingServiceOption;
                    //ebayItem.ShippingDetails.PaymentEdited;
                    //ebayItem.ShippingDetails.PaymentEditedSpecified;
                    //ebayItem.ShippingDetails.PaymentInstructions;
                    //ebayItem.ShippingDetails.PromotionalShippingDiscount;
                    //ebayItem.ShippingDetails.PromotionalShippingDiscountDetails;
                    //ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.Any;
                    //ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.DiscountName;
                    //ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.DiscountNameSpecified;
                    //ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.ItemCount;
                    //ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.ItemCountSpecified;
                    //ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.OrderAmount;
                    //ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.OrderAmount.currencyID;
                    //ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.OrderAmount.Value;
                    //ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.ShippingCost;
                    //ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.ShippingCost.currencyID;
                    //ebayItem.ShippingDetails.PromotionalShippingDiscountDetails.ShippingCost.Value;
                    //ebayItem.ShippingDetails.PromotionalShippingDiscountSpecified;
                    //ebayItem.ShippingDetails.RateTableDetails;
                    //ebayItem.ShippingDetails.RateTableDetails.Any;
                    //ebayItem.ShippingDetails.RateTableDetails.DomesticRateTable;
                    //ebayItem.ShippingDetails.RateTableDetails.DomesticRateTableId;
                    //ebayItem.ShippingDetails.RateTableDetails.InternationalRateTable;
                    //ebayItem.ShippingDetails.RateTableDetails.InternationalRateTableId;
                    //ebayItem.ShippingDetails.SalesTax;
                    //ebayItem.ShippingDetails.SalesTax.SalesTaxAmount;
                    //ebayItem.ShippingDetails.SalesTax.SalesTaxAmount.currencyID;
                    //ebayItem.ShippingDetails.SalesTax.SalesTaxAmount.Value;
                    //ebayItem.ShippingDetails.SalesTax.SalesTaxPercent;
                    //ebayItem.ShippingDetails.SalesTax.SalesTaxPercentSpecified;
                    //ebayItem.ShippingDetails.SalesTax.SalesTaxState;
                    //ebayItem.ShippingDetails.SalesTax.ShippingIncludedInTax;
                    //ebayItem.ShippingDetails.SalesTax.ShippingIncludedInTaxSpecified;
                    //ebayItem.ShippingDetails.SellerExcludeShipToLocationsPreference;
                    //ebayItem.ShippingDetails.SellerExcludeShipToLocationsPreferenceSpecified;
                    //ebayItem.ShippingDetails.SellingManagerSalesRecordNumber;
                    //ebayItem.ShippingDetails.SellingManagerSalesRecordNumberSpecified;
                    //ebayItem.ShippingDetails.ShipmentTrackingDetails;
                    //ebayItem.ShippingDetails.ShippingDiscountProfileID;
                    //ebayItem.ShippingDetails.ShippingRateErrorMessage;
                    //ebayItem.ShippingDetails.ShippingRateType;
                    //ebayItem.ShippingDetails.ShippingRateTypeSpecified;
                    //ebayItem.ShippingDetails.ShippingServiceOptions;
                    //ebayItem.ShippingDetails.ShippingServiceUsed;
                    //ebayItem.ShippingDetails.ShippingType;
                    //ebayItem.ShippingDetails.ShippingTypeSpecified;
                    //ebayItem.ShippingDetails.TaxTable;
                    //ebayItem.ShippingDetails.ThirdPartyCheckout;
                    //ebayItem.ShippingDetails.ThirdPartyCheckoutSpecified;



                    //ebayItem.ShippingOverride;
                    //ebayItem.ShippingOverride.DispatchTimeMaxOverride;
                    //ebayItem.ShippingOverride.DispatchTimeMaxOverrideSpecified;
                    //ebayItem.ShippingOverride.ShippingServiceCostOverrideList;
                    //ebayItem.ShippingOverride.ShippingServiceCostOverrideList.Any;
                    //ebayItem.ShippingOverride.ShippingServiceCostOverrideList.ShippingServiceCostOverride;

                    //ebayItem.ShippingPackageDetails;
                    //ebayItem.ShippingPackageDetails.Any;
                    //ebayItem.ShippingPackageDetails.MeasurementUnit;
                    //ebayItem.ShippingPackageDetails.MeasurementUnitSpecified;
                    //ebayItem.ShippingPackageDetails.PackageDepth;
                    //ebayItem.ShippingPackageDetails.PackageDepth.measurementSystem;
                    //ebayItem.ShippingPackageDetails.PackageDepth.measurementSystemSpecified;
                    //ebayItem.ShippingPackageDetails.PackageDepth.unit;
                    //ebayItem.ShippingPackageDetails.PackageDepth.Value;
                    //ebayItem.ShippingPackageDetails.PackageLength;
                    //ebayItem.ShippingPackageDetails.PackageLength.measurementSystem;
                    //ebayItem.ShippingPackageDetails.PackageLength.measurementSystemSpecified;
                    //ebayItem.ShippingPackageDetails.PackageLength.unit;
                    //ebayItem.ShippingPackageDetails.PackageLength.Value;
                    //ebayItem.ShippingPackageDetails.PackageWidth;
                    //ebayItem.ShippingPackageDetails.PackageWidth.measurementSystem;
                    //ebayItem.ShippingPackageDetails.PackageWidth.measurementSystemSpecified;
                    //ebayItem.ShippingPackageDetails.PackageWidth.unit;
                    //ebayItem.ShippingPackageDetails.PackageWidth.Value;
                    //ebayItem.ShippingPackageDetails.ShippingIrregular;
                    //ebayItem.ShippingPackageDetails.ShippingIrregularSpecified;
                    //ebayItem.ShippingPackageDetails.ShippingPackage;
                    //ebayItem.ShippingPackageDetails.ShippingPackageSpecified;
                    //ebayItem.ShippingPackageDetails.WeightMajor;
                    //ebayItem.ShippingPackageDetails.WeightMajor.measurementSystem;
                    //ebayItem.ShippingPackageDetails.WeightMajor.measurementSystemSpecified;
                    //ebayItem.ShippingPackageDetails.WeightMajor.unit;
                    //ebayItem.ShippingPackageDetails.WeightMajor.Value;
                    //ebayItem.ShippingPackageDetails.WeightMinor;
                    //ebayItem.ShippingPackageDetails.WeightMinor.measurementSystem;
                    //ebayItem.ShippingPackageDetails.WeightMinor.measurementSystemSpecified;
                    //ebayItem.ShippingPackageDetails.WeightMinor.unit;
                    //ebayItem.ShippingPackageDetails.WeightMinor.Value;


                    //ebayItem.ShippingServiceCostOverrideList;
                    //ebayItem.ShippingServiceCostOverrideList.Any;
                    //ebayItem.ShippingServiceCostOverrideList.ShippingServiceCostOverride;

                    //ebayItem.ShipToLocations;
                    //ebayItem.Site;
                    //ebayItem.SiteId;
                    //ebayItem.SiteIdSpecified;
                    //ebayItem.SiteSpecified;
                    
                    
                    //ebayItem.StartPrice;
                    //ebayItem.StartPrice.currencyID;
                    //ebayItem.StartPrice.Value;

                    //ebayItem.Storefront;
                    //ebayItem.Storefront.Any;
                    //ebayItem.Storefront.StoreCategory2ID;
                    //ebayItem.Storefront.StoreCategory2Name;
                    //ebayItem.Storefront.StoreCategoryID;
                    //ebayItem.Storefront.StoreCategoryName;
                    //ebayItem.Storefront.StoreName;
                    //ebayItem.Storefront.StoreURL;

                    //ebayItem.SubTitle;
                    //ebayItem.TaxCategory;
                    //ebayItem.TimeLeft;
                    //ebayItem.Title;
                    //ebayItem.TopRatedListing;
                    //ebayItem.TopRatedListingSpecified;
                    //ebayItem.TotalQuestionCount;
                    //ebayItem.TotalQuestionCountSpecified;

                    //ebayItem.UnitInfo;
                    //ebayItem.UnitInfo.Any;
                    //ebayItem.UnitInfo.UnitQuantity;
                    //ebayItem.UnitInfo.UnitQuantitySpecified;
                    //ebayItem.UnitInfo.UnitType;


                    //ebayItem.UpdateReturnPolicy;
                    //ebayItem.UpdateReturnPolicySpecified;
                    //ebayItem.UpdateSellerInfo;
                    //ebayItem.UpdateSellerInfoSpecified;
                    //ebayItem.UseTaxTable;
                    //ebayItem.UseTaxTableSpecified;
                    //ebayItem.UUID;

                    //ebayItem.Variations;
                    //ebayItem.Variations.Any;
                    //ebayItem.Variations.ModifyNameList;
                    //ebayItem.Variations.Pictures;
                    //ebayItem.Variations.Variation;
                    //ebayItem.Variations.VariationSpecificsSet;


                    //ebayItem.VATDetails;
                    //ebayItem.VATDetails.Any;
                    //ebayItem.VATDetails.BusinessSeller;
                    //ebayItem.VATDetails.BusinessSellerSpecified;
                    //ebayItem.VATDetails.RestrictedToBusiness;
                    //ebayItem.VATDetails.RestrictedToBusinessSpecified;
                    //ebayItem.VATDetails.VATID;
                    //ebayItem.VATDetails.VATPercent;
                    //ebayItem.VATDetails.VATPercentSpecified;
                    //ebayItem.VATDetails.VATSite;

                    //ebayItem.VIN;
                    //ebayItem.VINLink;
                    //ebayItem.VRM;
                    //ebayItem.VRMLink;
                    //ebayItem.WatchCount;
                    //ebayItem.WatchCountSpecified;

                    // Ebay has no shares/likes/comments
                    // Item.Categories.Add("BFRST-NPY");
                    // Item.Colors.Add("BFRST-NPY");
                    treecatItem.Date = ebayItem.ListingDetails.StartTime.ToString().Substring(0, 10);
                    treecatItem.URL = ebayItem.ListingDetails.ViewItemURL;
                    treecatItem.HasOffer = ebayItem.SellingStatus.BidCount > 0 ? "Yes" : "No";

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
