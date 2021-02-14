using System;
using System.Collections.Generic;

namespace lib.poshmark_client
{
    public class PoshmarkItem
    {
        public string ID { get; set; }
        public string CreatorID { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
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
        public string Title { get; set; }
        public string Description { get; set; }
        public string PriceAmountVal { get; set; }
        public string PriceAmountCurrencyCode { get; set; }
        public string PriceAmountCurrencySymbol { get; set; }
        public string OriginalPriceAmountVal { get; set; }
        public string OriginalPriceAmountCurrencyCode { get; set; }
        public string OriginalPriceAmountCurrencySymbol { get; set; }
        public string Size { get; set; }
        public string Brand { get; set; }
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
        public int Price { get; set; }
        public int OriginalPrice { get; set; }
        public int ShareCount { get; set; }
        public int PostEventHostSharesPage { get; set; }
        public string OriginalDomain { get; set; }
        public List<string> DestinationDomains { get; set; }
        public bool HasOffer { get; set; }
        public bool HasSellerOffer { get; set; }
        public string PictureUrl { get; set; }
        public string AggregatesShares { get; set; }
        public string AggregatesComments { get; set; }
        public string AggregatesLikes { get; set; }
        public List<string> Comments { get; set; }
        public List<string> Events { get; set; }
        public List<string> EventsHostShares { get; set; }
        public List<string> Likes { get; set; }
        public bool PoshPassEligible { get; set; }
        public string CreatorUsername  { get; set; }
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
    }

    public class PoshmarkClient
    {
        public List<string> GetCategories(PoshmarkItem result) 
        {
            List<string> categories = new List<string>();

            categories.Add(result.DepartmentDisplay);
            //categories.Add(result.CategoryFeaturesDisplay);
            categories.Add(result.CategoryV2Display);

            return categories;
        }

        public List<string> GetColors(PoshmarkItem result)
        {
            List<string> colors = new List<string>();

            //colors.Add(result.ColorsName);

            return colors;
        }

        public string GetCreatedDate(PoshmarkItem result)
        {
            string date = result.CreatedAt;

            return date.Substring(0, 10);
        }

        public string GetHasOffer(PoshmarkItem result)
        {
            string hasOffer = String.Empty;

            if (result.HasOffer == true)
            {
                hasOffer = "Yes";
            } 
            else if (result.HasOffer == false)
            {
                hasOffer = "No";
            }
                
            return hasOffer;
        }

        // Should be PoshmarkItem
        public List<PoshmarkItem> List(string accountName) 
        {
            return PoshmarkAPI.Request(accountName);
        }
    }
}
