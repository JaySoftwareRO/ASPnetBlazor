using ebayws;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

// Lists all items for a provider
namespace lib
{
    public interface Lister
    {
        Task<List<Item>> List();
        Task<List<string>> ImportTreecatIDs(dynamic itemsToImport, List<Item> userEbayCachedItems);
    }

    public class Item
    {
        public string ID { get; set; }

        public string Provisioner { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public double Price { get; set; }

        public Dictionary<Feature, object> Value { get; set; }

        public string Status { get; set; }

        public int Stock { get; set; }

        public string SKU { get; set; }

        public string MainImageURL { get; set; }

        public string Size { get; set; }

        public string Brand { get; set; }

        public int OriginalPrice { get; set; }

        public List<string> Categories { get; set; }

        public List<string> Colors { get; set; }

        public string Date { get; set; }

        public string URL { get; set; }

        public string Shares { get; set; }

        public string Comments { get; set; }

        public string Likes { get; set; }

        public string HasOffer { get; set; }

        // Poshmark only fields
        public string CreatorID { get; set; }
        public string Category { get; set; }
        public string StatusChangedAt { get; set; }
        public int PublishCount { get; set; }
        public string App { get; set; }
        public string AppVersion { get; set; }
        public string InventoryStatus { get; set; }
        public string InventoryStatusChangedAt { get; set; }
        public string InventorySizeQuantitiesSizeID { get; set; }
        public int InventorySizeQuantitiesQuantityAvailable { get; set; }
        public int InventorySizeQuantitiesQuantityReserved { get; set; }
        public int InventorySizeQuantitiesQuantitySold { get; set; }
        public int InventorySizeQuantitiesSizeRef { get; set; }
        public string InventorySizeQuantitiesSizeObjID { get; set; }
        public string InventorySizeQuantitiesSizeObjDisplay { get; set; }
        public string InventorySizeQuantitiesSizeObjDisplayWithSizeSet { get; set; }
        public string InventorySizeQuantitiesSizeSetTags { get; set; }
        public int InventorySizeQuantityRevision { get; set; }
        public string InventoryLastUnitReservedAt { get; set; }
        public string InventoryNfsReason { get; set; }
        public bool InventoryMultiItem { get; set; }
        public string CatalogDepartment { get; set; }
        public string CatalogCategory { get; set; }
        public string CatalogCategoryFeatures { get; set; }
        public string CatalogDepartmentObjID { get; set; }
        public string CatalogDepartmentObjDisplay { get; set; }
        public string CatalogDepartmentObjSlug { get; set; }
        public string CatalogCategoryObjID { get; set; }
        public string CatalogCategoryObjDisplay { get; set; }
        public string CatalogCategoryObjSlug { get; set; }
        public string CatalogCategoryFeatureObjID { get; set; }
        public string CatalogCategoryFeatureObjDisplay { get; set; }
        public string CatalogCategoryFeatureObjSlug { get; set; }
        public List<string> ColorsName { get; set; }
        public List<string> ColorsRGB { get; set; }
        public string CatalogSource { get; set; }
        public string UpdatedAt { get; set; }
        public List<string> PicturesID { get; set; }
        public List<string> PicturesPicture { get; set; }
        public List<string> PicturesPath { get; set; }
        public List<string> PicturesPathSmall { get; set; }
        public List<string> PicturesPathLarge { get; set; }
        public List<string> PicturesContentType { get; set; }
        public List<string> PicturesStorageLocation { get; set; }
        public List<string> PicturesMd5Hash { get; set; }
        public List<string> PicturesCreatedAt { get; set; }
        public List<string> PicturesUrl { get; set; }
        public List<string> PicturesUrlSmall { get; set; }
        public List<string> PicturesUrlLarge { get; set; }
        public string PriceAmountVal { get; set; }
        public string PriceAmountCurrencyCode { get; set; }
        public string PriceAmountCurrencySymbol { get; set; }
        public string OriginalPriceAmountVal { get; set; }
        public string OriginalPriceAmountCurrencyCode { get; set; }
        public string OriginalPriceAmountCurrencySymbol { get; set; }
        public string BrandID { get; set; }
        public string Condition { get; set; }
        public string CoverShotID { get; set; }
        public string CoverShotPicture { get; set; }
        public string CoverShotPath { get; set; }
        public string CoverShotPathSmall { get; set; }
        public string CoverShotPathLarge { get; set; }
        public string CoverShotContentType { get; set; }
        public string CoverShotStorageLocation { get; set; }
        public string CoverShotMd5Hash { get; set; }
        public string CoverShotCreatedAt { get; set; }
        public string CoverShotUrl { get; set; }
        public string CoverShotUrlSmall { get; set; }
        public string CoverShotUrlLarge { get; set; }
        public int PostLikePage { get; set; }
        public int LikeCount { get; set; }
        public int PostCommentPage { get; set; }
        public int CommentCount { get; set; }
        public string FirstEventShareEventID { get; set; }
        public string FirstEventShareSharedAt { get; set; }
        public string InventoryUnitID { get; set; }
        public string PostEventPage { get; set; }
        public string CreatedAt { get; set; }
        public int ShareCount { get; set; }
        public int PostEventHostSharesPage { get; set; }
        public string OriginalDomain { get; set; }
        public List<string> DestinationDomains { get; set; }
        public bool HasSellerOffer { get; set; }
        public string PictureUrl { get; set; }
        public string AggregatesShares { get; set; }
        public string AggregatesComments { get; set; }
        public string AggregatesLikes { get; set; }
        public List<string> Events { get; set; }
        public List<string> EventsHostShares { get; set; }
        public bool PoshPassEligible { get; set; }
        public string CreatorUsername { get; set; }
        public string CreatorDisplayHandle { get; set; }
        public string CreatorFullName { get; set; }
        public string CreatorFbID { get; set; }
        public string CreatorPictureURL { get; set; }
        public bool CallerHasLiked { get; set; }
        public string DepartmentID { get; set; }
        public string DepartmentDisplay { get; set; }
        public string DepartmentSlug { get; set; }
        public string CategoryV2ID { get; set; }
        public string CategoryV2Display { get; set; }
        public string CategoryV2Slug { get; set; }
        public List<string> CategoryFeaturesID { get; set; }
        public List<string> CategoryFeaturesDisplay { get; set; }
        public List<string> CategoryFeaturesSlug { get; set; }
        public string SizeObjID { get; set; }
        public string SizeObjDisplay { get; set; }
        public string SizeObjDisplayWithSizeSet { get; set; }
        public string BrandObjID { get; set; }
        public string BrandObjCanonicalName { get; set; }
        public string BrandObjSlug { get; set; }

        // Ebay only fields
        public string ApplicationData { get; set; }

        public BuyerProtectionDetailsType ApplyBuyerProtection { get; set; }

        public AttributeType[] AttributeArray { get; set; }

        public AttributeSetType[] AttributeSetArray { get; set; }

        public bool AutoPay { get; set; }

        public bool AutoPaySpecified { get; set; }

        public bool AvailableForPickupDropOff { get; set; }

        public bool AvailableForPickupDropOffSpecified { get; set; }

        public BestOfferDetailsType BestOfferDetails { get; set; }

        public bool BestOfferEnabled { get; set; }

        public bool BestOfferEnabledSpecified { get; set; }

        public BiddingDetailsType BiddingDetails { get; set; }

        public bool BidGroupItem { get; set; }

        public bool BidGroupItemSpecified { get; set; }

        public BusinessSellerDetailsType BusinessSellerDetails { get; set; }

        public AmountType BuyerGuaranteePrice { get; set; }

        public BuyerProtectionCodeType BuyerProtection { get; set; }

        public bool BuyerProtectionSpecified { get; set; }

        public BuyerRequirementDetailsType BuyerRequirementDetails { get; set; }

        public bool BuyerResponsibleForShipping { get; set; }

        public bool BuyerResponsibleForShippingSpecified { get; set; }

        public AmountType BuyItNowPrice { get; set; }

        public bool CategoryMappingAllowed { get; set; }

        public AmountType CeilingPrice { get; set; }

        public CharityType Charity { get; set; }

        public AmountType ClassifiedAdPayPerLeadFee { get; set; }

        public string ConditionDefinition { get; set; }

        public string ConditionDescription { get; set; }

        public string ConditionDisplayName { get; set; }

        public int ConditionID { get; set; }

        public bool ConditionIDSpecified { get; set; }

        public CountryCodeType Country { get; set; }

        public bool CountrySpecified { get; set; }

        public string[] CrossBorderTrade { get; set; }

        public CrossPromotionsType CrossPromotions { get; set; }

        public CurrencyCodeType Currency { get; set; }

        public bool CurrencySpecified { get; set; }

        public DescriptionReviseModeCodeType DescriptionReviseMode { get; set; }

        public bool DescriptionReviseModeSpecified { get; set; }

        public DigitalGoodInfoType DigitalGoodInfo { get; set; }

        public bool DisableBuyerRequirements { get; set; }

        public bool DisableBuyerRequirementsSpecified { get; set; }

        public DiscountPriceInfoType DiscountPriceInfo { get; set; }

        public int DispatchTimeMax { get; set; }

        public bool DispatchTimeMaxSpecified { get; set; }

        public DistanceType Distance { get; set; }

        public string eBayNotes { get; set; }

        public bool eBayPlus { get; set; }

        public bool eBayPlusSpecified { get; set; }

        public bool eBayPlusEligible { get; set; }

        public bool eBayPlusEligibleSpecified { get; set; }

        public bool EligibleForPickupDropOff { get; set; }

        public bool EligibleForPickupDropOffSpecified { get; set; }

        public bool eMailDeliveryAvailable { get; set; }

        public bool eMailDeliveryAvailableSpecified { get; set; }

        public ExtendedContactDetailsType ExtendedContactDetails { get; set; }

        public AmountType FloorPrice { get; set; }

        public CategoryType FreeAddedCategory { get; set; }

        public bool GetItFast { get; set; }

        public bool GetItFastSpecified { get; set; }

        public string GroupCategoryID { get; set; }

        public bool HideFromSearch { get; set; }

        public bool HideFromSearchSpecified { get; set; }

        public long HitCount { get; set; }

        public HitCounterCodeType HitCounter { get; set; }

        public bool HitCounterSpecified { get; set; }

        public bool HitCountSpecified { get; set; }

        public bool IgnoreQuantity { get; set; }

        public bool IgnoreQuantitySpecified { get; set; }

        public bool IncludeRecommendations { get; set; }

        public bool IntegratedMerchantCreditCardEnabled { get; set; }

        public bool IntegratedMerchantCreditCardEnabledSpecified { get; set; }

        public InventoryTrackingMethodCodeType InventoryTrackingMethod { get; set; }

        public bool InventoryTrackingMethodSpecified { get; set; }

        public bool IsIntermediatedShippingEligible { get; set; }

        public bool IsIntermediatedShippingEligibleSpecified { get; set; }

        public bool IsSecureDescription { get; set; }

        public bool IsSecureDescriptionSpecified { get; set; }

        public int ItemCompatibilityCount { get; set; }

        public bool ItemCompatibilityCountSpecified { get; set; }

        public ItemCompatibilityListType ItemCompatibilityList { get; set; }

        public ItemPolicyViolationType ItemPolicyViolation { get; set; }

        public NameValueListType[] ItemSpecifics { get; set; }

        public int LeadCount { get; set; }

        public bool LeadCountSpecified { get; set; }

        public bool LimitedWarrantyEligible { get; set; }

        public bool LimitedWarrantyEligibleSpecified { get; set; }

        public ListingDesignerType ListingDesigner { get; set; }

        public ListingDetailsType ListingDetails { get; set; }

        public string ListingDuration { get; set; }

        public ListingEnhancementsCodeType[] ListingEnhancement { get; set; }

        public ListingSubtypeCodeType ListingSubtype2 { get; set; }

        public bool ListingSubtype2Specified { get; set; }

        public ListingTypeCodeType ListingType { get; set; }

        public bool ListingTypeSpecified { get; set; }

        public bool LiveAuction { get; set; }

        public bool LiveAuctionSpecified{ get; set; }

        public bool LocalListing { get; set; }

        public bool LocalListingSpecified { get; set; }

        public string Location { get; set; }

        public bool LocationDefaulted { get; set; }

        public bool LocationDefaultedSpecified { get; set; }

        public LookupAttributeType[] LookupAttributeArray { get; set; }

        public int LotSize { get; set; }

        public bool LotSizeSpecified { get; set; }

        public bool MechanicalCheckAccepted { get; set; }

        public bool MechanicalCheckAcceptedSpecified { get; set; }

        public int NewLeadCount { get; set; }

        public bool NewLeadCountSpecified { get; set; }

        public string PartnerCode { get; set; }

        public string PartnerName { get; set; }

        public SiteCodeType[] PaymentAllowedSite { get; set; }

        public PaymentDetailsType PaymentDetails { get; set; }

        public BuyerPaymentMethodCodeType[] PaymentMethods { get; set; }

        public string PayPalEmailAddress { get; set; }

        public PickupInStoreDetailsType PickupInStoreDetails { get; set; }

        public PictureDetailsType PictureDetails { get; set; }

        public string PostalCode { get; set; }

        public CategoryType PrimaryCategory { get; set; }

        public bool PrivateListing { get; set; }

        public bool PrivateListingSpecified { get; set; }

        public string PrivateNotes { get; set; }

        public ProductListingDetailsType ProductListingDetails { get; set; }

        public bool ProxyItem { get; set; }

        public bool ProxyItemSpecified { get; set; }

        public int Quantity { get; set; }

        public int QuantityAvailable { get; set; }

        public QuantityAvailableHintCodeType QuantityAvailableHint { get; set; }

        public bool QuantityAvailableHintSpecified { get; set; }

        public bool QuantityAvailableSpecified { get; set; }

        public QuantityInfoType QuantityInfo { get; set; }

        public QuantityRestrictionPerBuyerInfoType QuantityRestrictionPerBuyer { get; set; }

        public bool QuantitySpecified { get; set; }

        public int QuantityThreshold { get; set; }

        public bool QuantityThresholdSpecified { get; set; }

        public long QuestionCount { get; set; }

        public bool QuestionCountSpecified { get; set; }

        public ReasonHideFromSearchCodeType ReasonHideFromSearch { get; set; }

        public bool ReasonHideFromSearchSpecified { get; set; }

        public string RegionID { get; set; }

        public bool Relisted { get; set; }

        public bool RelistedSpecified { get; set; }

        public bool RelistLink { get; set; }

        public bool RelistLinkSpecified { get; set; }

        public long RelistParentID { get; set; }

        public bool RelistParentIDSpecified { get; set; }

        public AmountType ReservePrice { get; set; }

        public ReturnPolicyType ReturnPolicy { get; set; }

        public ReviseStatusType ReviseStatus { get; set; }

        public SearchDetailsType SearchDetails { get; set; }

        public CategoryType SecondaryCategory { get; set; }

        public UserType Seller { get; set; }

        public AddressType SellerContactDetails { get; set; }

        public SellerProfilesType SellerProfiles { get; set; }

        public string SellerProvidedTitle { get; set; }

        public string SellerVacationNote { get; set; }

        public SellingStatusType SellingStatus { get; set; }

        public ShippingDetailsType ShippingDetails { get; set; }

        public ShippingOverrideType ShippingOverride { get; set; }

        public ShipPackageDetailsType ShippingPackageDetails { get; set; }

        public ShippingServiceCostOverrideListType ShippingServiceCostOverrideList { get; set; }

        public string[] ShipToLocations { get; set; }

        public SiteCodeType Site { get; set; }

        public int SiteID { get; set; }

        public bool SiteIDSpecified { get; set; }

        public bool SiteSpecified { get; set; }

        public AmountType StartPrice { get; set; }

        public StorefrontType StoreFront { get; set; }

        public string SubTitle { get; set; }

        public string TaxCategory { get; set; }

        public string TimeLeft { get; set; }

        public bool TopRatedListing { get; set; }

        public bool TopRatedListingSpecified { get; set; }

        public long TotalQuestionCount { get; set; }

        public bool TotalQuestionCountSpecified { get; set; }

        public UnitInfoType UnitInfo { get; set; }

        public bool UpdateReturnPolicy { get; set; }

        public bool UpdateReturnPolicySpecified { get; set; }

        public bool UpdateSellerInfo { get; set; }

        public bool UpdateSellerInfoSpecified { get; set; }

        public bool UseTaxTable { get; set; }

        public bool UseTaxTableSpecified { get; set; }

        public string UUID { get; set; }

        public VariationsType Variations { get; set; }

        public VATDetailsType VATDetails { get; set; }

        public string VIN { get; set; }

        public string VINLink { get; set; }

        public string VRM { get; set; }

        public string VRMLink { get; set; }

        public long WatchCount { get; set; }

        public bool WatchCountSpecified { get; set; }
    }

    public class AmountType
    {
        public CurrencyCodeType currencyID { get; set; }

        public double Value { get; set; }
    }

    public class AddressAttributeType
    {
        public AddressAttributeCodeType type { get; set; }

        public bool typeSpecified { get; set; }

        public string Value { get; set; }
    }

    public class AddressType
    {
        public string Name { get; set; }

        public string Street { get; set; }

        public string Street1 { get; set; }

        public string Street2 { get; set; }

        public string CityName { get; set; }

        public string County { get; set; }

        public string StateOrProvince { get; set; }

        public CountryCodeType Country { get; set; }

        public bool CountrySpecified { get; set; }

        public string CountryName { get; set; }

        public string Phone { get; set; }

        public CountryCodeType PhoneCountryCode { get; set; }

        public bool PhoneCountryCodeSpecified { get; set; }

        public string PhoneCountryPrefix { get; set; }

        public string PhoneAreaOrCityCode { get; set; }

        public string PhoneLocalNumber { get; set; }

        public string PostalCode { get; set; }

        public string AddressID { get; set; }

        public AddressOwnerCodeType AddressOwner { get; set; }

        public bool AddressOwnerSpecified { get; set; }

        public AddressStatusCodeType AddressStatus { get; set; }

        public bool AddressStatusSpecified { get; set; }

        public string ExternalAddressID { get; set; }

        public string InternationalName { get; set; }

        public string InternationalStateAndCity { get; set; }

        public string InternationalStreet { get; set; }

        public string CompanyName { get; set; }

        public AddressRecordTypeCodeType AddressRecordType { get; set; }

        public bool AddressRecordTypeSpecified { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Phone2 { get; set; }

        public AddressUsageCodeType AddressUsage { get; set; }

        public bool AddressUsageSpecified { get; set; }

        public string ReferenceID { get; set; }

        public AddressAttributeType[] AddressAttribute { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class AttributeSetType
    {
        public AttributeType[] Attribute { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }

        public int attributeSetID { get; set; }

        public bool attributeSetIDSpecified { get; set; }

        public string attributeSetVersion { get; set; }
    }

    public class AttributeType
    {
        public ValType[] Value { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }

        public int attributeID { get; set; }

        public bool attributeIDSpecified { get; set; }

        public string attributeLabel { get; set; }
    }

    public class BestOfferDetailsType
    {
        public int BestOfferCount { get; set; }

        public bool BestOfferCountSpecified { get; set; }

        public bool BestOfferEnabled { get; set; }

        public bool BestOfferEnabledSpecified { get; set; }

        public AmountType BestOffer { get; set; }

        public BestOfferStatusCodeType BestOfferStatus { get; set; }

        public bool BestOfferStatusSpecified { get; set; }

        public BestOfferTypeCodeType BestOfferType { get; set; }

        public bool BestOfferTypeSpecified { get; set; }

        public bool NewBestOffer { get; set; }

        public bool NewBestOfferSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class BiddingDetailsType
    {
        public AmountType ConvertedMaxBid { get; set; }

        public AmountType MaxBid { get; set; }

        public int QuantityBid { get; set; }

        public bool QuantityBidSpecified { get; set; }

        public int QuantityWon { get; set; }

        public bool QuantityWonSpecified { get; set; }

        public bool Winning { get; set; }

        public bool WinningSpecified { get; set; }

        public bool BidAssistant { get; set; }

        public bool BidAssistantSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class BiddingSummaryType
    {
        public int SummaryDays { get; set; }

        public bool SummaryDaysSpecified { get; set; }

        public int TotalBids { get; set; }

        public bool TotalBidsSpecified { get; set; }

        public int BidActivityWithSeller { get; set; }

        public bool BidActivityWithSellerSpecified { get; set; }

        public int BidsToUniqueSellers { get; set; }

        public bool BidsToUniqueSellersSpecified { get; set; }

        public int BidsToUniqueCategories { get; set; }

        public bool BidsToUniqueCategoriesSpecified { get; set; }

        public int BidRetractions { get; set; }

        public bool BidRetractionsSpecified { get; set; }

        public ItemBidDetailsType[] ItemBidDetails { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class BrandMPNType
    {
        public string Brand { get; set; }

        public string MPN { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class BusinessSellerDetailsType
    {
        public AddressType Address { get; set; }

        public string Fax { get; set; }

        public string Email { get; set; }

        public string AdditionalContactInformation { get; set; }

        public string TradeRegistrationNumber { get; set; }

        public bool LegalInvoice { get; set; }

        public bool LegalInvoiceSpecified { get; set; }

        public string TermsAndConditions { get; set; }

        public VATDetailsType VATDetails { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class BuyerType
    {
        public AddressType ShippingAddress { get; set; }

        public TaxIdentifierType[] BuyerTaxIdentifier { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class BuyerRequirementDetailsType
    {
        public bool ShipToRegistrationCountry { get; set; }

        public bool ShipToRegistrationCountrySpecified { get; set; }

        public bool ZeroFeedbackScore { get; set; }

        public bool ZeroFeedbackScoreSpecified { get; set; }

        public MaximumItemRequirementsType MaximumItemRequirements { get; set; }

        public MaximumUnpaidItemStrikesInfoType MaximumUnpaidItemStrikesInfo { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class BuyerProtectionDetailsType
    {
        public BuyerProtectionSourceCodeType BuyerProtectionSource { get; set; }

        public bool BuyerProtectionSourceSpecified { get; set; }

        public BuyerProtectionCodeType BuyerProtectionStatus { get; set; }

        public bool BuyerProtectionStatusSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }


    public class CalculatedShippingDiscountType
    {
        public DiscountNameCodeType DiscountName { get; set; }

        public bool DiscountNameSpecified { get; set; }

        public DiscountProfileType[] DiscountProfile { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class CalculatedShippingRateType
    {
        public string OriginatingPostalCode { get; set; }

        public MeasurementSystemCodeType MeasurementUnit { get; set; }

        public bool MeasurementUnitSpecified { get; set; }

        public AmountType PackagingHandlingCosts { get; set; }

        public bool ShippingIrregular { get; set; }

        public bool ShippingIrregularSpecified { get; set; }

        public AmountType InternationalPackagingHandlingCosts { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class CharityType
    {
        public string CharityName { get; set; }

        public int CharityNumber { get; set; }

        public bool CharityNumberSpecified { get; set; }

        public float DonationPercent { get; set; }

        public bool DonationPercentSpecified { get; set; }

        public string CharityID { get; set; }

        public string Mission { get; set; }

        public string LogoURL { get; set; }

        public CharityStatusCodeType Status { get; set; }

        public bool StatusSpecified { get; set; }

        public bool CharityListing { get; set; }

        public bool CharityListingSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class CharityAffiliationsType
    {
        public CharityIDType[] CharityID { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class CharityAffiliationDetailType
    {
        public string CharityID { get; set; }

        public CharityAffiliationTypeCodeType AffiliationType { get; set; }

        public bool AffiliationTypeSpecified { get; set; }

        public System.DateTime LastUsedTime { get; set; }

        public bool LastUsedTimeSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class CharityIDType
    {
        public CharityAffiliationTypeCodeType type { get; set; }

        public string Value { get; set; }
    }

    public class ContactHoursDetailsType
    {
        public string TimeZoneID { get; set; }

        public DaysCodeType Hours1Days { get; set; }

        public bool Hours1DaysSpecified { get; set; }

        public bool Hours1AnyTime { get; set; }

        public bool Hours1AnyTimeSpecified { get; set; }

        public System.DateTime Hours1From { get; set; }

        public bool Hours1FromSpecified { get; set; }

        public System.DateTime Hours1To { get; set; }

        public bool Hours1ToSpecified { get; set; }

        public DaysCodeType Hours2Days { get; set; }

        public bool Hours2DaysSpecified { get; set; }

        public bool Hours2AnyTime { get; set; }

        public bool Hours2AnyTimeSpecified { get; set; }

        public System.DateTime Hours2From { get; set; }

        public bool Hours2FromSpecified { get; set; }

        public System.DateTime Hours2To { get; set; }

        public bool Hours2ToSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class CrossPromotionsType
    {
        public string ItemID { get; set; }

        public PromotionSchemeCodeType PrimaryScheme { get; set; }

        public bool PrimarySchemeSpecified { get; set; }

        public PromotionMethodCodeType PromotionMethod { get; set; }

        public bool PromotionMethodSpecified { get; set; }

        public string SellerID { get; set; }

        public bool ShippingDiscount { get; set; }

        public string StoreName { get; set; }

        public PromotedItemType[] PromotedItem { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class CategoryType
    {
        public bool BestOfferEnabled { get; set; }

        public bool BestOfferEnabledSpecified { get; set; }

        public bool AutoPayEnabled { get; set; }

        public bool AutoPayEnabledSpecified { get; set; }

        public bool B2BVATEnabled { get; set; }

        public bool B2BVATEnabledSpecified { get; set; }

        public bool CatalogEnabled { get; set; }

        public bool CatalogEnabledSpecified { get; set; }

        public string CategoryID { get; set; }

        public int CategoryLevel { get; set; }

        public bool CategoryLevelSpecified { get; set; }

        public string CategoryName { get; set; }

        public string[] CategoryParentID { get; set; }

        public string[] CategoryParentName { get; set; }

        public bool ProductSearchPageAvailable { get; set; }

        public bool ProductSearchPageAvailableSpecified { get; set; }

        public ExtendedProductFinderIDType[] ProductFinderIDs { get; set; }

        public CharacteristicsSetType[] CharacteristicsSets { get; set; }

        public bool Expired { get; set; }

        public bool ExpiredSpecified { get; set; }

        public bool IntlAutosFixedCat { get; set; }

        public bool IntlAutosFixedCatSpecified { get; set; }

        public bool LeafCategory { get; set; }

        public bool LeafCategorySpecified { get; set; }

        public bool Virtual { get; set; }

        public bool VirtualSpecified { get; set; }

        public int NumOfItems { get; set; }

        public bool NumOfItemsSpecified { get; set; }

        public bool SellerGuaranteeEligible { get; set; }

        public bool SellerGuaranteeEligibleSpecified { get; set; }

        public bool ORPA { get; set; }

        public bool ORPASpecified { get; set; }

        public bool ORRA { get; set; }

        public bool ORRASpecified { get; set; }

        public bool LSD { get; set; }

        public bool LSDSpecified { get; set; }

        public string Keywords { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class CharacteristicsSetType
    {
        public string Name { get; set; }

        public int AttributeSetID { get; set; }

        public bool AttributeSetIDSpecified { get; set; }

        public string AttributeSetVersion { get; set; }

        public CharacteristicType[] Characteristics { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class CharacteristicType
    {
        public int AttributeID { get; set; }

        public string DateFormat { get; set; }

        public string DisplaySequence { get; set; }

        public string DisplayUOM { get; set; }

        public LabelType Label { get; set; }

        public SortOrderCodeType SortOrder { get; set; }

        public bool SortOrderSpecified { get; set; }

        public ValType[] ValueList { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class DigitalGoodInfoType
    {
        public bool DigitalDelivery { get; set; }

        public bool DigitalDeliverySpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class DiscountPriceInfoType
    {
        public AmountType OriginalRetailPrice { get; set; }

        public AmountType MinimumAdvertisedPrice { get; set; }

        public MinimumAdvertisedPriceExposureCodeType MinimumAdvertisedPriceExposure { get; set; }

        public bool MinimumAdvertisedPriceExposureSpecified { get; set; }

        public PricingTreatmentCodeType PricingTreatment { get; set; }

        public bool PricingTreatmentSpecified { get; set; }

        public bool SoldOneBay { get; set; }

        public bool SoldOffeBay { get; set; }

        public AmountType MadeForOutletComparisonPrice { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class DistanceType
    {
        public int DistanceMeasurement { get; set; }

        public string DistanceUnit { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class DiscountProfileType
    {
        public string DiscountProfileID { get; set; }

        public string DiscountProfileName { get; set; }

        public AmountType EachAdditionalAmount { get; set; }

        public AmountType EachAdditionalAmountOff { get; set; }

        public float EachAdditionalPercentOff { get; set; }

        public bool EachAdditionalPercentOffSpecified { get; set; }

        public MeasureType WeightOff { get; set; }

        public string MappedDiscountProfileID { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ExtendedContactDetailsType
    {
        public ContactHoursDetailsType ContactHoursDetails { get; set; }

        public bool ClassifiedAdContactByEmailEnabled { get; set; }

        public bool ClassifiedAdContactByEmailEnabledSpecified { get; set; }

        public string PayPerLeadPhoneNumber { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ExtendedPictureDetailsType
    {
        public PictureURLsType[] PictureURLs { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ExtendedProductFinderIDType
    {
        public int ProductFinderID { get; set; }

        public bool ProductFinderIDSpecified { get; set; }

        public bool ProductFinderBuySide { get; set; }

        public bool ProductFinderBuySideSpecified { get; set; }
    }

    public class FlatShippingDiscountType
    {
        public DiscountNameCodeType DiscountName { get; set; }

        public bool DiscountNameSpecified { get; set; }

        public DiscountProfileType[] DiscountProfile { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class LabelType
    {
        public string Name { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }

        public bool visible { get; set; }

        public bool visibleSpecified { get; set; }
    }

    public class LineItemType
    {
        public int Quantity { get; set; }

        public bool QuantitySpecified { get; set; }

        public string CountryOfOrigin { get; set; }

        public string Description { get; set; }

        public string ItemID { get; set; }

        public string TransactionID { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ItemBidDetailsType
    {
        public string ItemID { get; set; }

        public string CategoryID { get; set; }

        public int BidCount { get; set; }

        public bool BidCountSpecified { get; set; }

        public string SellerID { get; set; }

        public System.DateTime LastBidTime { get; set; }

        public bool LastBidTimeSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ItemCompatibilityListType
    {
        public ItemCompatibilityType[] Compatibility { get; set; }

        public bool ReplaceAll { get; set; }

        public bool ReplaceAllSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public partial class ItemCompatibilityType
    {
        public bool Delete { get; set; }

        public bool DeleteSpecified { get; set; }

        public NameValueListType[] NameValueList { get; set; }

        public string CompatibilityNotes { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ItemPolicyViolationType
    {
        public long PolicyID { get; set; }

        public bool PolicyIDSpecified { get; set; }

        public string PolicyText { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class InternationalShippingServiceOptionsType
    {
        public string ShippingService { get; set; }

        public AmountType ShippingServiceCost { get; set; }

        public AmountType ShippingServiceAdditionalCost { get; set; }

        public int ShippingServicePriority { get; set; }

        public bool ShippingServicePrioritySpecified { get; set; }

        public string[] ShipToLocation { get; set; }

        public AmountType ShippingInsuranceCost { get; set; }

        public AmountType ImportCharge { get; set; }

        public System.DateTime ShippingServiceCutOffTime { get; set; }

        public bool ShippingServiceCutOffTimeSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ListingDesignerType
    {
        public int LayoutID { get; set; }

        public bool LayoutIDSpecified { get; set; }

        public bool OptimalPictureSize { get; set; }

        public bool OptimalPictureSizeSpecified { get; set; }

        public int ThemeID { get; set; }

        public bool ThemeIDSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ListingDetailsType
    {
        public bool Adult { get; set; }

        public bool AdultSpecified { get; set; }

        public bool BindingAuction { get; set; }

        public bool BindingAuctionSpecified { get; set; }

        public bool CheckoutEnabled { get; set; }

        public bool CheckoutEnabledSpecified { get; set; }

        public AmountType ConvertedBuyItNowPrice { get; set; }

        public AmountType ConvertedStartPrice { get; set; }

        public AmountType ConvertedReservePrice { get; set; }

        public bool HasReservePrice { get; set; }

        public bool HasReservePriceSpecified { get; set; }

        public string RelistedItemID { get; set; }

        public string SecondChanceOriginalItemID { get; set; }

        public System.DateTime StartTime { get; set; }

        public bool StartTimeSpecified { get; set; }

        public System.DateTime EndTime { get; set; }

        public bool EndTimeSpecified { get; set; }

        public string ViewItemURL { get; set; }

        public bool HasUnansweredQuestions { get; set; }

        public bool HasUnansweredQuestionsSpecified { get; set; }

        public bool HasPublicMessages { get; set; }

        public bool HasPublicMessagesSpecified { get; set; }

        public bool BuyItNowAvailable { get; set; }

        public bool BuyItNowAvailableSpecified { get; set; }

        public SellerBusinessCodeType SellerBusinessType { get; set; }

        public bool SellerBusinessTypeSpecified { get; set; }

        public AmountType MinimumBestOfferPrice { get; set; }

        public string MinimumBestOfferMessage { get; set; }

        public string LocalListingDistance { get; set; }

        public string TCROriginalItemID { get; set; }

        public string ViewItemURLForNaturalSearch { get; set; }

        public bool PayPerLeadEnabled { get; set; }

        public bool PayPerLeadEnabledSpecified { get; set; }

        public AmountType BestOfferAutoAcceptPrice { get; set; }

        public EndReasonCodeType EndingReason { get; set; }

        public bool EndingReasonSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class LookupAttributeType
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class MaximumUnpaidItemStrikesInfoType
    {
        public int Count { get; set; }

        public bool CountSpecified { get; set; }

        public PeriodCodeType Period { get; set; }

        public bool PeriodSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class MaximumItemRequirementsType
    {
        public int MaximumItemCount { get; set; }

        public bool MaximumItemCountSpecified { get; set; }

        public int MinimumFeedbackScore { get; set; }

        public bool MinimumFeedbackScoreSpecified { get; set; }
    }

    public class MeasureType
    {
        public string unit { get; set; }

        public MeasurementSystemCodeType measurementSystem { get; set; }

        public bool measurementSystemSpecified { get; set; }

        public decimal Value { get; set; }
    }

    public class MembershipDetailType
    {
        public string ProgramName { get; set; }

        public SiteCodeType Site { get; set; }

        public bool SiteSpecified { get; set; }

        public System.DateTime ExpiryDate { get; set; }

        public bool ExpiryDateSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ModifyNameType
    {
        public string Name { get; set; }

        public string NewName { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }


    public class NameValueListType
    {
        public string Name { get; set; }

        public string[] Value { get; set; }

        public ItemSpecificSourceCodeType Source { get; set; }

        public bool SourceSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class PaymentDetailsType
    {
        public int HoursToDeposit { get; set; }

        public bool HoursToDepositSpecified { get; set; }

        public int DaysToFullPayment { get; set; }

        public bool DaysToFullPaymentSpecified { get; set; }

        public AmountType DepositAmount { get; set; }

        public DepositTypeCodeType DepositType { get; set; }

        public bool DepositTypeSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class PickupInStoreDetailsType
    {
        public bool EligibleForPickupInStore { get; set; }

        public bool EligibleForPickupInStoreSpecified { get; set; }

        public bool EligibleForPickupDropOff { get; set; }

        public bool EligibleForPickupDropOffSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class PictureDetailsType
    {
        public GalleryTypeCodeType GalleryType{ get; set; }

        public bool GalleryTypeSpecified { get; set; }

        public PhotoDisplayCodeType PhotoDisplay { get; set; }

        public bool PhotoDisplaySpecified { get; set; }

        public string[] PictureURL { get; set; }

        public PictureSourceCodeType PictureSource { get; set; }

        public bool PictureSourceSpecified { get; set; }

        public GalleryStatusCodeType GalleryStatus { get; set; }

        public bool GalleryStatusSpecified { get; set; }

        public string GalleryErrorInfo { get; set; }

        public string GalleryURL { get; set; }

        public string[] ExternalPictureURL { get; set; }

        public ExtendedPictureDetailsType ExtendedPictureDetails { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class PictureURLsType
    {
        public string eBayPictureURL { get; set; }

        public string ExternalPictureURL { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ProductListingDetailsType
    {
        public bool IncludeStockPhotoURL { get; set; }

        public bool IncludeStockPhotoURLSpecified { get; set; }

        public bool UseStockPhotoURLAsGallery { get; set; }

        public bool UseStockPhotoURLAsGallerySpecified { get; set; }

        public string StockPhotoURL { get; set; }

        public string[] Copyright { get; set; }

        public string ProductReferenceID { get; set; }

        public string DetailsURL { get; set; }

        public string ProductDetailsURL { get; set; }

        public bool ReturnSearchResultOnDuplicates { get; set; }

        public bool ReturnSearchResultOnDuplicatesSpecified { get; set; }

        public string ISBN { get; set; }

        public string UPC { get; set; }

        public string EAN { get; set; }

        public BrandMPNType BrandMPN { get; set; }

        public TicketListingDetailsType TicketListingDetails { get; set; }

        public bool UseFirstProduct { get; set; }

        public bool UseFirstProductSpecified { get; set; }

        public bool IncludeeBayProductDetails { get; set; }

        public bool IncludeeBayProductDetailsSpecified { get; set; }

        public NameValueListType[] NameValueList { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class PromotedItemType
    {
        public string ItemID { get; set; }

        public string PictureURL { get; set; }

        public int Position { get; set; }

        public bool PositionSpecified { get; set; }

        public PromotionItemSelectionCodeType SelectionType { get; set; }

        public bool SelectionTypeSpecified { get; set; }

        public string Title { get; set; }

        public ListingTypeCodeType ListingType { get; set; }

        public bool ListingTypeSpecified { get; set; }

        public PromotionDetailsType[] PromotionDetails { get; set; }

        public string TimeLeft { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class PromotionDetailsType
    {
        public AmountType PromotionPrice { get; set; }

        public PromotionItemPriceTypeCodeType PromotionPriceType { get; set; }

        public bool PromotionPriceTypeSpecified { get; set; }

        public int BidCount { get; set; }

        public bool BidCountSpecified { get; set; }

        public AmountType ConvertedPromotionPrice { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ProStoresCheckoutPreferenceType
    {
        public bool CheckoutRedirectProStores { get; set; }

        public bool CheckoutRedirectProStoresSpecified { get; set; }

        public ProStoresDetailsType ProStoresDetails { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class PicturesType
    {
        public string VariationSpecificName { get; set; }

        public VariationSpecificPictureSetType[] VariationSpecificPictureSet { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ProStoresDetailsType
    {
        public string SellerThirdPartyUsername { get; set; }

        public string StoreName { get; set; }

        public EnableCodeType Status { get; set; }

        public bool StatusSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class PromotionalSaleDetailsType
    {
        public AmountType OriginalPrice { get; set; }

        public System.DateTime StartTime { get; set; }

        public bool StartTimeSpecified { get; set; }

        public System.DateTime EndTime { get; set; }

        public bool EndTimeSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class PromotionalShippingDiscountDetailsType
    {
        public DiscountNameCodeType DiscountName { get; set; }

        public bool DiscountNameSpecified { get; set; }

        public AmountType ShippingCost { get; set; }

        public AmountType OrderAmount { get; set; }

        public int ItemCount { get; set; }

        public bool ItemCountSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class TicketListingDetailsType
    {
        public string EventTitle { get; set; }

        public string Venue { get; set; }

        public string PrintedDate { get; set; }

        public string PrintedTime { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ReturnPolicyType
    {
        public string RefundOption { get; set; }

        public string Refund { get; set; }

        public string ReturnsWithinOption { get; set; }

        public string ReturnsWithin { get; set; }

        public string ReturnsAcceptedOption { get; set; }

        public string ReturnsAccepted { get; set; }

        public string Description { get; set; }

        public string WarrantyOfferedOption { get; set; }

        public string WarrantyOffered { get; set; }

        public string WarrantyTypeOption { get; set; }

        public string WarrantyType { get; set; }

        public string WarrantyDurationOption { get; set; }

        public string WarrantyDuration { get; set; }

        public string ShippingCostPaidByOption { get; set; }

        public string ShippingCostPaidBy { get; set; }

        public string RestockingFeeValue { get; set; }

        public string RestockingFeeValueOption { get; set; }

        public bool ExtendedHolidayReturns { get; set; }

        public bool ExtendedHolidayReturnsSpecified { get; set; }

        public string InternationalRefundOption { get; set; }

        public string InternationalReturnsAcceptedOption { get; set; }

        public string InternationalReturnsWithinOption { get; set; }

        public string InternationalShippingCostPaidByOption { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class RateTableDetailsType
    {
        public string DomesticRateTable { get; set; }

        public string InternationalRateTable { get; set; }

        public string DomesticRateTableId { get; set; }

        public string InternationalRateTableId { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ReviseStatusType
    {
        public bool ItemRevised { get; set; }

        public bool BuyItNowAdded{ get; set; }

        public bool BuyItNowAddedSpecified { get; set; }

        public bool BuyItNowLowered { get; set; }

        public bool BuyItNowLoweredSpecified { get; set; }

        public bool ReserveLowered { get; set; }

        public bool ReserveLoweredSpecified { get; set; }

        public bool ReserveRemoved { get; set; }

        public bool ReserveRemovedSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class SalesTaxType
    {
        public float SalesTaxPercent { get; set; }

        public bool SalesTaxPercentSpecified { get; set; }

        public string SalesTaxState { get; set; }

        public bool ShippingIncludedInTax { get; set; }

        public bool ShippingIncludedInTaxSpecified { get; set; }

        public AmountType SalesTaxAmount { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class SchedulingInfoType
    {
        public int MaxScheduledMinutes { get; set; }

        public bool MaxScheduledMinutesSpecified { get; set; }

        public int MinScheduledMinutes { get; set; }

        public bool MinScheduledMinutesSpecified { get; set; }

        public int MaxScheduledItems { get; set; }

        public bool MaxScheduledItemsSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class SearchDetailsType
    {
        public bool BuyItNowEnabled { get; set; }

        public bool BuyItNowEnabledSpecified { get; set; }

        public bool Picture { get; set; }

        public bool PictureSpecified { get; set; }

        public bool RecentListing { get; set; }

        public bool RecentListingSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }


    public class SellerType
    {
        public int PaisaPayStatus { get; set; }

        public bool PaisaPayStatusSpecified { get; set; }

        public bool AllowPaymentEdit { get; set; }

        public CurrencyCodeType BillingCurrency { get; set; }

        public bool BillingCurrencySpecified { get; set; }

        public bool CheckoutEnabled { get; set; }

        public bool CIPBankAccountStored { get; set; }

        public bool GoodStanding { get; set; }

        public MerchandizingPrefCodeType MerchandizingPref { get; set; }

        public bool MerchandizingPrefSpecified { get; set; }

        public bool QualifiesForB2BVAT { get; set; }

        public SellerGuaranteeLevelCodeType SellerGuaranteeLevel { get; set; }

        public bool SellerGuaranteeLevelSpecified { get; set; }

        public SellerLevelCodeType SellerLevel { get; set; }

        public bool SellerLevelSpecified { get; set; }

        public AddressType SellerPaymentAddress { get; set; }

        public SchedulingInfoType SchedulingInfo { get; set; }

        public bool StoreOwner { get; set; }

        public string StoreURL { get; set; }

        public SellerBusinessCodeType SellerBusinessType { get; set; }

        public bool SellerBusinessTypeSpecified { get; set; }

        public bool RegisteredBusinessSeller { get; set; }

        public bool RegisteredBusinessSellerSpecified { get; set; }

        public SiteCodeType StoreSite { get; set; }

        public bool StoreSiteSpecified { get; set; }

        public SellerPaymentMethodCodeType PaymentMethod { get; set; }

        public bool PaymentMethodSpecified { get; set; }

        public ProStoresCheckoutPreferenceType ProStoresPreference { get; set; }

        public bool CharityRegistered { get; set; }

        public bool CharityRegisteredSpecified { get; set; }

        public bool SafePaymentExempt { get; set; }

        public bool SafePaymentExemptSpecified { get; set; }

        public int PaisaPayEscrowEMIStatus { get; set; }

        public bool PaisaPayEscrowEMIStatusSpecified { get; set; }

        public CharityAffiliationDetailType[] CharityAffiliationDetails { get; set; }

        public float TransactionPercent { get; set; }

        public bool TransactionPercentSpecified { get; set; }

        public IntegratedMerchantCreditCardInfoType IntegratedMerchantCreditCardInfo { get; set; }

        public FeatureEligibilityType FeatureEligibility { get; set; }

        public bool TopRatedSeller { get; set; }

        public bool TopRatedSellerSpecified { get; set; }

        public TopRatedSellerDetailsType TopRatedSellerDetails { get; set; }

        public RecoupmentPolicyConsentType RecoupmentPolicyConsent { get; set; }

        public bool DomesticRateTable { get; set; }

        public bool DomesticRateTableSpecified { get; set; }

        public bool InternationalRateTable { get; set; }

        public bool InternationalRateTableSpecified { get; set; }

        public SellereBayPaymentProcessStatusCodeType SellereBayPaymentProcessStatus { get; set; }

        public bool SellereBayPaymentProcessStatusSpecified { get; set; }

        public SellereBayPaymentProcessConsentCodeType SellereBayPaymentProcessConsent { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class SellingStatusType
    {
        public int BidCount { get; set; }

        public bool BidCountSpecified { get; set; }

        public AmountType BidIncrement { get; set; }

        public AmountType ConvertedCurrentPrice { get; set; }

        public AmountType CurrentPrice { get; set; }

        public UserType HighBidder { get; set; }

        public int LeadCount { get; set; }

        public bool LeadCountSpecified { get; set; }

        public AmountType MinimumToBid { get; set; }

        public int QuantitySold { get; set; }

        public bool QuantitySoldSpecified { get; set; }

        public bool ReserveMet { get; set; }

        public bool ReserveMetSpecified { get; set; }

        public bool SecondChanceEligible { get; set; }

        public bool SecondChanceEligibleSpecified { get; set; }

        public long BidderCount { get; set; }

        public bool BidderCountSpecified { get; set; }

        public ListingStatusCodeType ListingStatus { get; set; }

        public bool ListingStatusSpecified { get; set; }

        public AmountType FinalValueFee { get; set; }

        public PromotionalSaleDetailsType PromotionalSaleDetails { get; set; }

        public bool AdminEnded { get; set; }

        public bool AdminEndedSpecified { get; set; }

        public bool SoldAsBin { get; set; }

        public bool SoldAsBinSpecified { get; set; }

        public int QuantitySoldByPickupInStore { get; set; }

        public bool QuantitySoldByPickupInStoreSpecified { get; set; }

        public SuggestedBidValueType SuggestedBidValues { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class SuggestedBidValueType
    {
        public AmountType[] BidValue { get; set; }


        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ShippingDetailsType
    {
        public bool AllowPaymentEdit { get; set; }

        public bool AllowPaymentEditSpecified { get; set; }

        public bool ApplyShippingDiscount { get; set; }

        public bool ApplyShippingDiscountSpecified { get; set; }

        public bool GlobalShipping { get; set; }

        public bool GlobalShippingSpecified { get; set; }

        public CalculatedShippingRateType CalculatedShippingRate { get; set; }

        public bool ChangePaymentInstructions { get; set; }

        public bool ChangePaymentInstructionsSpecified { get; set; }

        public bool InsuranceWanted { get; set; }

        public bool InsuranceWantedSpecified { get; set; }

        public bool PaymentEdited { get; set; }

        public bool PaymentEditedSpecified { get; set; }

        public string PaymentInstructions { get; set; }

        public SalesTaxType SalesTax { get; set; }

        public string ShippingRateErrorMessage { get; set; }

        public ShippingRateTypeCodeType ShippingRateType { get; set; }

        public bool ShippingRateTypeSpecified { get; set; }

        public ShippingServiceOptionsType[] ShippingServiceOptions { get; set; }

        public InternationalShippingServiceOptionsType[] InternationalShippingServiceOption { get; set; }

        public ShippingTypeCodeType ShippingType { get; set; }

        public bool ShippingTypeSpecified { get; set; }

        public int SellingManagerSalesRecordNumber { get; set; }

        public bool SellingManagerSalesRecordNumberSpecified { get; set; }

        public bool ThirdPartyCheckout { get; set; }

        public bool ThirdPartyCheckoutSpecified { get; set; }

        public TaxJurisdictionType[] TaxTable { get; set; }

        public bool GetItFast { get; set; }

        public bool GetItFastSpecified { get; set; }

        public string ShippingServiceUsed { get; set; }

        public AmountType DefaultShippingCost { get; set; }

        public string ShippingDiscountProfileID { get; set; }

        public FlatShippingDiscountType FlatShippingDiscount { get; set; }

        public CalculatedShippingDiscountType CalculatedShippingDiscount { get; set; }

        public bool PromotionalShippingDiscount { get; set; }

        public bool PromotionalShippingDiscountSpecified { get; set; }

        public string InternationalShippingDiscountProfileID { get; set; }

        public FlatShippingDiscountType InternationalFlatShippingDiscount { get; set; }

        public CalculatedShippingDiscountType InternationalCalculatedShippingDiscount { get; set; }

        public bool InternationalPromotionalShippingDiscount { get; set; }

        public bool InternationalPromotionalShippingDiscountSpecified { get; set; }

        public PromotionalShippingDiscountDetailsType PromotionalShippingDiscountDetails { get; set; }

        public AmountType CODCost { get; set; }

        public string[] ExcludeShipToLocation { get; set; }

        public bool SellerExcludeShipToLocationsPreference { get; set; }

        public bool SellerExcludeShipToLocationsPreferenceSpecified { get; set; }

        public ShipmentTrackingDetailsType[] ShipmentTrackingDetails { get; set; }

        public RateTableDetailsType RateTableDetails { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ShipmentTrackingDetailsType
    {
        public string ShippingCarrierUsed { get; set; }

        public string ShipmentTrackingNumber { get; set; }

        public ShipmentLineItemType ShipmentLineItem { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ShipmentLineItemType
    {
        public LineItemType[] LineItem { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ShippingOverrideType
    {
        public ShippingServiceCostOverrideListType ShippingServiceCostOverrideList { get; set; }

        public int DispatchTimeMaxOverride { get; set; }

        public bool DispatchTimeMaxOverrideSpecified { get; set; }
    }

    public class ShippingServiceCostOverrideListType
    {
        public ShippingServiceCostOverrideType[] ShippingServiceCostOverride { get; set; }
    
        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ShippingServiceCostOverrideType
    {
        public int ShippingServicePriority { get; set; }

        public bool ShippingServicePrioritySpecified { get; set; }

        public ShippingServiceType ShippingServiceType { get; set; }

        public bool ShippingServiceTypeSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }

        public AmountType ShippingServiceCost { get; set; }

        public AmountType ShippingServiceAdditionalCost { get; set; }

        public AmountType ShippingSurcharge { get; set; }
    }

    public class ShipPackageDetailsType
    {
        public MeasurementSystemCodeType MeasurementUnit { get; set; }

        public bool MeasurementUnitSpecified { get; set; }

        public MeasureType PackageDepth { get; set; }

        public MeasureType PackageLength { get; set; }

        public MeasureType PackageWidth { get; set; }

        public bool ShippingIrregular { get; set; }

        public bool ShippingIrregularSpecified { get; set; }

        public ShippingPackageCodeType ShippingPackage { get; set; }

        public bool ShippingPackageSpecified { get; set; }

        public MeasureType WeightMajor { get; set; }

        public MeasureType WeightMinor { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ShippingServiceOptionsType
    {
        public AmountType ShippingInsuranceCost { get; set; }

        public string ShippingService { get; set; }

        public AmountType ShippingServiceCost { get; set; }

        public AmountType ShippingServiceAdditionalCost { get; set; }

        public int ShippingServicePriority { get; set; }

        public bool ShippingServicePrioritySpecified { get; set; }

        public bool ExpeditedService { get; set; }

        public bool ExpeditedServiceSpecified { get; set; }

        public int ShippingTimeMin { get; set; }

        public bool ShippingTimeMinSpecified { get; set; }

        public int ShippingTimeMax { get; set; }

        public bool ShippingTimeMaxSpecified { get; set; }

        public bool FreeShipping { get; set; }

        public bool FreeShippingSpecified { get; set; }

        public bool LocalPickup { get; set; }

        public bool LocalPickupSpecified { get; set; }

        public AmountType ImportCharge { get; set; }

        public ShippingPackageInfoType[] ShippingPackageInfo { get; set; }

        public System.DateTime ShippingServiceCutOffTime { get; set; }

        public bool ShippingServiceCutOffTimeSpecified { get; set; }

        public string LogisticPlanType { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ShippingPackageInfoType
    {
        public string StoreID { get; set; }

        public string ShippingTrackingEvent { get; set; }

        public System.DateTime ScheduledDeliveryTimeMin { get; set; }

        public bool ScheduledDeliveryTimeMinSpecified { get; set; }

        public System.DateTime ScheduledDeliveryTimeMax { get; set; }

        public bool ScheduledDeliveryTimeMaxSpecified { get; set; }

        public System.DateTime ActualDeliveryTime { get; set; }

        public bool ActualDeliveryTimeSpecified { get; set; }

        public System.DateTime EstimatedDeliveryTimeMin { get; set; }

        public bool EstimatedDeliveryTimeMinSpecified { get; set; }

        public System.DateTime EstimatedDeliveryTimeMax { get; set; }

        public bool EstimatedDeliveryTimeMaxSpecified { get; set; }

        public System.DateTime HandleByTime { get; set; }

        public bool HandleByTimeSpecified { get; set; }

        public System.DateTime MinNativeEstimatedDeliveryTime { get; set; }

        public bool MinNativeEstimatedDeliveryTimeSpecified { get; set; }

        public System.DateTime MaxNativeEstimatedDeliveryTime { get; set; }

        public bool MaxNativeEstimatedDeliveryTimeSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class SellingManagerProductInventoryStatusType
    {
        public int QuantityScheduled { get; set; }

        public bool QuantityScheduledSpecified { get; set; }

        public int QuantityActive { get; set; }

        public bool QuantityActiveSpecified { get; set; }

        public int QuantitySold { get; set; }

        public bool QuantitySoldSpecified { get; set; }

        public int QuantityUnsold { get; set; }

        public bool QuantityUnsoldSpecified { get; set; }

        public float SuccessPercent { get; set; }

        public bool SuccessPercentSpecified { get; set; }

        public AmountType AverageSellingPrice { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class StorefrontType
    {
        public long StoreCategoryID { get; set; }

        public long StoreCategory2ID { get; set; }

        public string StoreCategoryName { get; set; }

        public string StoreCategory2Name { get; set; }

        public string StoreURL { get; set; }

        public string StoreName { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class ValType
    {
        public string ValueLiteral { get; set; }

        public string[] SuggestedValueLiteral { get; set; }

        public int ValueID { get; set; }

        public bool ValueIDSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }


    public class VATDetailsType
    {
        public bool BusinessSeller { get; set; }

        public bool BusinessSellerSpecified { get; set; }

        public bool RestrictedToBusiness { get; set; }

        public bool RestrictedToBusinessSpecified { get; set; }

        public float VATPercent { get; set; }
        
        public bool VATPercentSpecified { get; set; }

        public string VATSite { get; set; }

        public string VATID { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class VariationSpecificPictureSetType
    {
        public string VariationSpecificValue { get; set; }

        public string[] PictureURL { get; set; }

        public string GalleryURL { get; set; }

        public string[] ExternalPictureURL { get; set; }

        public ExtendedPictureDetailsType ExtendedPictureDetails { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class VariationsType
    {
        public VariationType[] Variation { get; set; }

        public PicturesType[] Pictures { get; set; }

        public NameValueListType[] VariationSpecificsSet { get; set; }

        public ModifyNameType[] ModifyNameList { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class VariationType
    {
        public string SKU { get; set; }

        public AmountType StartPrice { get; set; }

        public int Quantity { get; set; }

        public bool QuantitySpecified { get; set; }

        public NameValueListType[] VariationSpecifics { get; set; }

        public int UnitsAvailable { get; set; }

        public bool UnitsAvailableSpecified { get; set; }

        public AmountType UnitCost { get; set; }

        public SellingStatusType SellingStatus { get; set; }

        public string VariationTitle { get; set; }

        public string VariationViewItemURL { get; set; }

        public bool Delete { get; set; }

        public SellingManagerProductInventoryStatusType SellingManagerProductInventoryStatus { get; set; }

        public long WatchCount { get; set; }

        public bool WatchCountSpecified { get; set; }

        public string PrivateNotes { get; set; }

        public DiscountPriceInfoType DiscountPriceInfo { get; set; }

        public VariationProductListingDetailsType VariationProductListingDetails { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class VariationProductListingDetailsType
    {
        public string ISBN { get; set; }

        public string UPC { get; set; }

        public string EAN { get; set; }

        public string ProductReferenceID { get; set; }

        public NameValueListType[] NameValueList { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class QuantityInfoType
    {
        public int MinimumRemnantSet { get; set; }

        public bool MinimumRemnantSetSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class QuantityRestrictionPerBuyerInfoType
    {
        public int MaximumQuantity { get; set; }

        public bool MaximumQuantitySpecified { get; set; }
    }

    public class UserType
    {
        public bool AboutMePage { get; set; }

        public bool AboutMePageSpecified { get; set; }

        public string EIASToken { get; set; }

        public string Email { get; set; }

        public int FeedbackScore { get; set; }

        public bool FeedbackScoreSpecified { get; set; }

        public int UniqueNegativeFeedbackCount { get; set; }

        public bool UniqueNegativeFeedbackCountSpecified { get; set; }

        public int UniquePositiveFeedbackCount { get; set; }

        public bool UniquePositiveFeedbackCountSpecified { get; set; }

        public float PositiveFeedbackPercent { get; set; }

        public bool PositiveFeedbackPercentSpecified { get; set; }

        public bool FeedbackPrivate { get; set; }

        public bool FeedbackPrivateSpecified { get; set; }

        public FeedbackRatingStarCodeType FeedbackRatingStar { get; set; }

        public bool FeedbackRatingStarSpecified { get; set; }

        public bool IDVerified { get; set; }

        public bool IDVerifiedSpecified { get; set; }

        public bool eBayGoodStanding { get; set; }

        public bool eBayGoodStandingSpecified { get; set; }

        public bool NewUser { get; set; }

        public bool NewUserSpecified { get; set; }

        public AddressType RegistrationAddress { get; set; }

        public System.DateTime RegistrationDate { get; set; }

        public bool RegistrationDateSpecified { get; set; }

        public SiteCodeType Site { get; set; }

        public bool SiteSpecified { get; set; }

        public UserStatusCodeType Status { get; set; }

        public bool StatusSpecified { get; set; }

        public string UserID { get; set; }

        public bool UserIDChanged { get; set; }

        public bool UserIDChangedSpecified { get; set; }

        public System.DateTime UserIDLastChanged { get; set; }

        public bool UserIDLastChangedSpecified { get; set; }

        public VATStatusCodeType VATStatus { get; set; }

        public bool VATStatusSpecified { get; set; }

        public BuyerType BuyerInfo { get; set; }

        public SellerType SellerInfo { get; set; }

        public BusinessRoleType BusinessRole { get; set; }

        public bool BusinessRoleSpecified { get; set; }

        public CharityAffiliationsType CharityAffiliations { get; set; }

        public PayPalAccountLevelCodeType PayPalAccountLevel { get; set; }

        public bool PayPalAccountLevelSpecified { get; set; }

        public PayPalAccountTypeCodeType PayPalAccountType { get; set; }

        public bool PayPalAccountTypeSpecified { get; set; }

        public PayPalAccountStatusCodeType PayPalAccountStatus { get; set; }

        public bool PayPalAccountStatusSpecified { get; set; }

        public EBaySubscriptionTypeCodeType[] UserSubscription { get; set; }

        public bool SiteVerified { get; set; }

        public bool SiteVerifiedSpecified { get; set; }

        public string[] SkypeID { get; set; }

        public bool eBayWikiReadOnly { get; set; }

        public bool eBayWikiReadOnlySpecified { get; set; }

        public int TUVLevel { get; set; }

        public bool TUVLevelSpecified { get; set; }

        public string VATID { get; set; }

        public SellerPaymentMethodCodeType SellerPaymentMethod { get; set; }

        public bool SellerPaymentMethodSpecified{ get; set; }

        public BiddingSummaryType BiddingSummary { get; set; }

        public bool UserAnonymized { get; set; }

        public bool UserAnonymizedSpecified { get; set; }

        public int UniqueNeutralFeedbackCount { get; set; }

        public bool UniqueNeutralFeedbackCountSpecified { get; set; }

        public bool EnterpriseSeller { get; set; }

        public bool EnterpriseSellerSpecified { get; set; }

        public string BillingEmail { get; set; }

        public bool QualifiesForSelling { get; set; }

        public bool QualifiesForSellingSpecified { get; set; }

        public string StaticAlias { get; set; }

        public AddressType ShippingAddress { get; set; }

        public MembershipDetailType[] Membership { get; set; }

        public string UserFirstName { get; set; }

        public string UserLastName { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class TaxIdentifierType
    {
        public ValueTypeCodeType Type { get; set; }

        public bool TypeSpecified { get; set; }

        public string ID { get; set; }

        public TaxIdentifierAttributeType[] Attribute { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class TaxIdentifierAttributeType
    {
        public TaxIdentifierAttributeCodeType name { get; set; }

        public bool nameSpecified { get; set; }

        public string Value { get; set; }
    }

    public class TaxJurisdictionType
    {
        public string JurisdictionID { get; set; }

        public float SalesTaxPercent { get; set; }

        public bool SalesTaxPercentSpecified { get; set; }

        public bool ShippingIncludedInTax { get; set; }

        public bool ShippingIncludedInTaxSpecified { get; set; }

        public string JurisdictionName { get; set; }

        public string DetailVersion { get; set; }

        public System.DateTime UpdateTime { get; set; }

        public bool UpdateTimeSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    public class UnitInfoType
    {
        public string UnitType { get; set; }

        public double UnitQuantity { get; set; }

        public bool UnitQuantitySpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }

    }

    public class AmountType
    {
        public CurrencyCodeType currencyID { get; set; }

        public double Value { get; set; }
    }

    public class BestOfferDetailsType
    {
        public int BestOfferCount { get; set; }

        public bool BestOfferCountSpecified { get; set; }

        public bool BestOfferEnabled { get; set; }

        public bool BestOfferEnabledSpecified { get; set; }

        public AmountType BestOffer { get; set; }

        public BestOfferStatusCodeType BestOfferStatus { get; set; }

        public bool BestOfferStatusSpecified { get; set; }

        public BestOfferTypeCodeType BestOfferType { get; set; }

        public bool BestOfferTypeSpecified { get; set; }

        public bool NewBestOffer { get; set; }

        public bool NewBestOfferSpecified { get; set; }

        public System.Xml.XmlElement[] Any { get; set; }
    }
