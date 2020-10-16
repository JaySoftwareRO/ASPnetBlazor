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

        // Ebay only fields
        public string ApplicationData { get; set; }

        public ebayws.BuyerProtectionDetailsType ApplyBuyerProtection { get; set; }

        public ebayws.AttributeType[] AttributeArray { get; set; }

        public ebayws.AttributeSetType[] AttributeSetArray { get; set; }

        public bool AutoPay { get; set; }

        public bool AutoPaySpecified { get; set; }

        public bool AvailableForPickupDropOff { get; set; }

        public bool AvailableForPickupDropOffSpecified { get; set; }

        public BestOfferDetailsType BestOfferDetails { get; set; }
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

    public enum BestOfferTypeCodeType
    {
        /// <remarks/>
        BuyerBestOffer,
        
        /// <remarks/>
        BuyerCounterOffer,
        
        /// <remarks/>
        SellerCounterOffer,
        
        /// <remarks/>
        CustomCode,
    }

    public enum BestOfferStatusCodeType
    {
        /// <remarks/>
        Pending,

        /// <remarks/>
        Accepted,

        /// <remarks/>
        Declined,

        /// <remarks/>
        Expired,

        /// <remarks/>
        Retracted,

        /// <remarks/>
        AdminEnded,

        /// <remarks/>
        Active,

        /// <remarks/>
        Countered,

        /// <remarks/>
        All,

        /// <remarks/>
        PendingBuyerPayment,

        /// <remarks/>
        PendingBuyerConfirmation,

        /// <remarks/>
        CustomCode,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.2")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "urn:ebay:apis:eBLBaseComponents")]
    public enum CurrencyCodeType
    {

        /// <remarks/>
        AFA,

        /// <remarks/>
        ALL,

        /// <remarks/>
        DZD,

        /// <remarks/>
        ADP,

        /// <remarks/>
        AOA,

        /// <remarks/>
        ARS,

        /// <remarks/>
        AMD,

        /// <remarks/>
        AWG,

        /// <remarks/>
        AZM,

        /// <remarks/>
        BSD,

        /// <remarks/>
        BHD,

        /// <remarks/>
        BDT,

        /// <remarks/>
        BBD,

        /// <remarks/>
        BYR,

        /// <remarks/>
        BZD,

        /// <remarks/>
        BMD,

        /// <remarks/>
        BTN,

        /// <remarks/>
        INR,

        /// <remarks/>
        BOV,

        /// <remarks/>
        BOB,

        /// <remarks/>
        BAM,

        /// <remarks/>
        BWP,

        /// <remarks/>
        BRL,

        /// <remarks/>
        BND,

        /// <remarks/>
        BGL,

        /// <remarks/>
        BGN,

        /// <remarks/>
        BIF,

        /// <remarks/>
        KHR,

        /// <remarks/>
        CAD,

        /// <remarks/>
        CVE,

        /// <remarks/>
        KYD,

        /// <remarks/>
        XAF,

        /// <remarks/>
        CLF,

        /// <remarks/>
        CLP,

        /// <remarks/>
        CNY,

        /// <remarks/>
        COP,

        /// <remarks/>
        KMF,

        /// <remarks/>
        CDF,

        /// <remarks/>
        CRC,

        /// <remarks/>
        HRK,

        /// <remarks/>
        CUP,

        /// <remarks/>
        CYP,

        /// <remarks/>
        CZK,

        /// <remarks/>
        DKK,

        /// <remarks/>
        DJF,

        /// <remarks/>
        DOP,

        /// <remarks/>
        TPE,

        /// <remarks/>
        ECV,

        /// <remarks/>
        ECS,

        /// <remarks/>
        EGP,

        /// <remarks/>
        SVC,

        /// <remarks/>
        ERN,

        /// <remarks/>
        EEK,

        /// <remarks/>
        ETB,

        /// <remarks/>
        FKP,

        /// <remarks/>
        FJD,

        /// <remarks/>
        GMD,

        /// <remarks/>
        GEL,

        /// <remarks/>
        GHC,

        /// <remarks/>
        GIP,

        /// <remarks/>
        GTQ,

        /// <remarks/>
        GNF,

        /// <remarks/>
        GWP,

        /// <remarks/>
        GYD,

        /// <remarks/>
        HTG,

        /// <remarks/>
        HNL,

        /// <remarks/>
        HKD,

        /// <remarks/>
        HUF,

        /// <remarks/>
        ISK,

        /// <remarks/>
        IDR,

        /// <remarks/>
        IRR,

        /// <remarks/>
        IQD,

        /// <remarks/>
        ILS,

        /// <remarks/>
        JMD,

        /// <remarks/>
        JPY,

        /// <remarks/>
        JOD,

        /// <remarks/>
        KZT,

        /// <remarks/>
        KES,

        /// <remarks/>
        AUD,

        /// <remarks/>
        KPW,

        /// <remarks/>
        KRW,

        /// <remarks/>
        KWD,

        /// <remarks/>
        KGS,

        /// <remarks/>
        LAK,

        /// <remarks/>
        LVL,

        /// <remarks/>
        LBP,

        /// <remarks/>
        LSL,

        /// <remarks/>
        LRD,

        /// <remarks/>
        LYD,

        /// <remarks/>
        CHF,

        /// <remarks/>
        LTL,

        /// <remarks/>
        MOP,

        /// <remarks/>
        MKD,

        /// <remarks/>
        MGF,

        /// <remarks/>
        MWK,

        /// <remarks/>
        MYR,

        /// <remarks/>
        MVR,

        /// <remarks/>
        MTL,

        /// <remarks/>
        EUR,

        /// <remarks/>
        MRO,

        /// <remarks/>
        MUR,

        /// <remarks/>
        MXN,

        /// <remarks/>
        MXV,

        /// <remarks/>
        MDL,

        /// <remarks/>
        MNT,

        /// <remarks/>
        XCD,

        /// <remarks/>
        MZM,

        /// <remarks/>
        MMK,

        /// <remarks/>
        ZAR,

        /// <remarks/>
        NAD,

        /// <remarks/>
        NPR,

        /// <remarks/>
        ANG,

        /// <remarks/>
        XPF,

        /// <remarks/>
        NZD,

        /// <remarks/>
        NIO,

        /// <remarks/>
        NGN,

        /// <remarks/>
        NOK,

        /// <remarks/>
        OMR,

        /// <remarks/>
        PKR,

        /// <remarks/>
        PAB,

        /// <remarks/>
        PGK,

        /// <remarks/>
        PYG,

        /// <remarks/>
        PEN,

        /// <remarks/>
        PHP,

        /// <remarks/>
        PLN,

        /// <remarks/>
        USD,

        /// <remarks/>
        QAR,

        /// <remarks/>
        ROL,

        /// <remarks/>
        RUB,

        /// <remarks/>
        RUR,

        /// <remarks/>
        RWF,

        /// <remarks/>
        SHP,

        /// <remarks/>
        WST,

        /// <remarks/>
        STD,

        /// <remarks/>
        SAR,

        /// <remarks/>
        SCR,

        /// <remarks/>
        SLL,

        /// <remarks/>
        SGD,

        /// <remarks/>
        SKK,

        /// <remarks/>
        SIT,

        /// <remarks/>
        SBD,

        /// <remarks/>
        SOS,

        /// <remarks/>
        LKR,

        /// <remarks/>
        SDD,

        /// <remarks/>
        SRG,

        /// <remarks/>
        SZL,

        /// <remarks/>
        SEK,

        /// <remarks/>
        SYP,

        /// <remarks/>
        TWD,

        /// <remarks/>
        TJS,

        /// <remarks/>
        TZS,

        /// <remarks/>
        THB,

        /// <remarks/>
        XOF,

        /// <remarks/>
        TOP,

        /// <remarks/>
        TTD,

        /// <remarks/>
        TND,

        /// <remarks/>
        TRL,

        /// <remarks/>
        TMM,

        /// <remarks/>
        UGX,

        /// <remarks/>
        UAH,

        /// <remarks/>
        AED,

        /// <remarks/>
        GBP,

        /// <remarks/>
        USS,

        /// <remarks/>
        USN,

        /// <remarks/>
        UYU,

        /// <remarks/>
        UZS,

        /// <remarks/>
        VUV,

        /// <remarks/>
        VEB,

        /// <remarks/>
        VND,

        /// <remarks/>
        MAD,

        /// <remarks/>
        YER,

        /// <remarks/>
        YUM,

        /// <remarks/>
        ZMK,

        /// <remarks/>
        ZWD,

        /// <remarks/>
        ATS,

        /// <remarks/>
        CustomCode,
    }

}
