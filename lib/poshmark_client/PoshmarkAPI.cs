using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Web;

namespace lib.poshmark_client
{
    public class PoshmarkAPI
    {
        private const int itemsPerPage = 50;

        public static string GetRequestFilter(int count, string maxID)
        {
            IDictionary<string, string> x = new Dictionary<string, string>()
            {
                {"count", count.ToString()},
            };

            if (!string.IsNullOrWhiteSpace(maxID))
            {
                x["max_id"] = maxID;
            }

            return HttpUtility.UrlEncode(JsonConvert.SerializeObject(x));
        }

        public static Uri RequestUri(string username)
        {
            return new Uri($"https://poshmark.com/vm-rest/users/{username}/posts/filtered");
        }

        public static List<PoshmarkItem> Request(string username)
        {
            var result = new List<PoshmarkItem>();
            string maxID = string.Empty;
            string newMaxID;
            int returnedCount = itemsPerPage;

            while (returnedCount == itemsPerPage)
            {
                var items = Request(username, maxID, out newMaxID);
                maxID = newMaxID;
                returnedCount = items.Count;
                result.AddRange(items);
            }

            return result;
        }

        private static List<PoshmarkItem> Request(string username, string maxID, out string newMaxID)
        {
            newMaxID = string.Empty;
            var result = new List<PoshmarkItem>();

            var baseURL = RequestUri(username);
            var filter = GetRequestFilter(itemsPerPage, maxID);

            var uBuilder = new UriBuilder(baseURL);
            uBuilder.Query = $"request={filter}";


            var json = new WebClient().DownloadString(uBuilder.Uri);
            dynamic deserializedJson= JsonConvert.DeserializeObject<dynamic>(json);

            try
            {
                foreach (var item in deserializedJson.data)
                {
                    PoshmarkItem poshmarkItem = new PoshmarkItem();

                    poshmarkItem.ID = item.id;
                    poshmarkItem.CreatorID = item.creator_id;
                    poshmarkItem.Category = item.category;
                    poshmarkItem.Status = item.status;
                    poshmarkItem.StatusChangedAt = item.status_changed_at;
                    poshmarkItem.PublishCount = item.publish_count;
                    poshmarkItem.App = item.app;
                    poshmarkItem.AppVersion = item.app_version;
                    poshmarkItem.InventoryStatus = item.inventory.status;
                    poshmarkItem.InventoryStatusChangedAt = item.inventory.status_changed_at;

                    foreach (var tempItem in item.inventory.size_quantities)
                    {
                        poshmarkItem.InventorySizeQuantitiesSizeID = tempItem.size_id;
                        poshmarkItem.InventorySizeQuantitiesQuantityAvailable = tempItem.quantity_available;
                        poshmarkItem.InventorySizeQuantitiesQuantityReserved = tempItem.quantity_reserved;
                        poshmarkItem.InventorySizeQuantitiesQuantitySold = tempItem.quantity_sold;
                        poshmarkItem.InventorySizeQuantitiesSizeRef = tempItem.size_ref;
                        poshmarkItem.InventorySizeQuantitiesSizeObjID = tempItem.size_obj.id;
                        poshmarkItem.InventorySizeQuantitiesSizeObjDisplay = tempItem.size_obj.display;
                        poshmarkItem.InventorySizeQuantitiesSizeObjDisplayWithSizeSet = tempItem.size_obj.display_with_size_set;
                        foreach(var tag in tempItem.size_set_tags)
                        {
                            poshmarkItem.InventorySizeQuantitiesSizeSetTags = tag;
                        }
                    }

                    poshmarkItem.InventorySizeQuantityRevision = item.inventory.size_quantity_revision;
                    poshmarkItem.InventoryLastUnitReservedAt = item.inventory.last_unit_reserved_at;
                    poshmarkItem.InventoryMultiItem = item.inventory.multi_item;
                    poshmarkItem.InventoryNfsReason = item.inventory.nfs_reason;

                    poshmarkItem.CatalogDepartment = item.catalog.department;
                    poshmarkItem.CatalogCategory = item.catalog.category;

                    foreach (var feature in item.catalog.category_features)
                    {
                        poshmarkItem.CatalogCategoryFeatures = feature;
                    }

                    poshmarkItem.CatalogDepartmentObjID = item.catalog.department_obj.id;
                    poshmarkItem.CatalogDepartmentObjDisplay = item.catalog.department_obj.display;
                    poshmarkItem.CatalogDepartmentObjSlug = item.catalog.department_obj.slug;

                    poshmarkItem.CatalogCategoryObjID = item.catalog.category_obj.id;
                    poshmarkItem.CatalogCategoryObjDisplay = item.catalog.category_obj.display;
                    poshmarkItem.CatalogCategoryObjSlug = item.catalog.category_obj.slug;

                    foreach (var obj in item.catalog.category_feature_objs)
                    {
                        poshmarkItem.CatalogCategoryFeatureObjID = obj.id;
                        poshmarkItem.CatalogCategoryFeatureObjDisplay = obj.display;
                        poshmarkItem.CatalogCategoryFeatureObjSlug = obj.slug;
                    }

                    foreach (var color in item.colors)
                    {
                        poshmarkItem.ColorsName.Add(color.name);
                        poshmarkItem.ColorsRGB.Add(color.rgb);
                    }

                    poshmarkItem.CatalogSource = item.catalog_source;
                    poshmarkItem.UpdatedAt = item.updated_at;

                    foreach (var picture in item.pictures)
                    {
                        poshmarkItem.PicturesID.Add(picture.id);
                        poshmarkItem.PicturesPicture.Add(picture.picture);
                        poshmarkItem.PicturesPath.Add(picture.path);
                        poshmarkItem.PicturesPathSmall.Add(picture.path_small);
                        poshmarkItem.PicturesPathLarge.Add(picture.path_large);
                        poshmarkItem.PicturesContentType.Add(picture.content_type);
                        poshmarkItem.PicturesStorageLocation.Add(picture.storage_location);
                        poshmarkItem.PicturesMd5Hash.Add(picture.md5_hash);
                        poshmarkItem.PicturesCreatedAt.Add(picture.created_at);
                        poshmarkItem.PicturesUrl.Add(picture.url);
                        poshmarkItem.PicturesUrlSmall.Add(picture.url_small);
                        poshmarkItem.PicturesUrlLarge.Add(picture.url_large);
                    }

                    poshmarkItem.Title = item.title;
                    poshmarkItem.Description = item.description;

                    poshmarkItem.PriceAmountVal = item.price_amount.val;
                    poshmarkItem.PriceAmountCurrencyCode = item.price_amount.currency_code;
                    poshmarkItem.PriceAmountCurrencySymbol = item.price_amount.currency_symbol;

                    poshmarkItem.OriginalPriceAmountVal = item.original_price_amount.val;
                    poshmarkItem.OriginalPriceAmountCurrencyCode = item.original_price_amount.currency_code;
                    poshmarkItem.OriginalPriceAmountCurrencySymbol = item.original_price_amount.currency_symbol;

                    poshmarkItem.Size = item.size;
                    poshmarkItem.Brand = item.brand;
                    poshmarkItem.BrandID = item.brand_id;
                    poshmarkItem.Condition = item.condition;

                    poshmarkItem.CoverShotID = item.cover_shot.id;
                    poshmarkItem.CoverShotPicture = item.cover_shot.picture;
                    poshmarkItem.CoverShotPath = item.cover_shot.path;
                    poshmarkItem.CoverShotPathSmall = item.cover_shot.path;
                    poshmarkItem.CoverShotPathLarge = item.cover_shot.path_large;
                    poshmarkItem.CoverShotContentType = item.cover_shot.content_type;
                    poshmarkItem.CoverShotStorageLocation = item.cover_shot.storage_location;
                    poshmarkItem.CoverShotMd5Hash = item.cover_shot.md5_hash;
                    poshmarkItem.CoverShotCreatedAt = item.cover_shot.created_at;
                    poshmarkItem.CoverShotUrl = item.cover_shot.url;
                    poshmarkItem.CoverShotUrlSmall = item.cover_shot.url_small;
                    poshmarkItem.CoverShotUrlLarge = item.cover_shot.url_large;

                    poshmarkItem.PostLikePage = item.post_like_page;
                    poshmarkItem.LikeCount = item.like_count;
                    poshmarkItem.PostCommentPage = item.post_comment_page;
                    poshmarkItem.CommentCount = item.comment_count;

                    //poshmarkItem.FirstEventShareEventID = item.first_event_share.event_id;
                    //poshmarkItem.FirstEventShareSharedAt = item.first_event_share.shared_at;

                    poshmarkItem.InventoryUnitID = item.inventory_unit_id;
                    poshmarkItem.PostEventPage = item.post_event_page;
                    poshmarkItem.CreatedAt = item.created_at;
                    poshmarkItem.Price = item.price;
                    poshmarkItem.OriginalPrice = item.original_price;
                    poshmarkItem.ShareCount = item.share_count;
                    poshmarkItem.PostEventHostSharesPage = item.post_event_host_shares_page;
                    poshmarkItem.OriginalDomain = item.origin_domain;

                    poshmarkItem.HasOffer = item.has_offer;
                    poshmarkItem.HasSellerOffer = item.has_seller_offer;
                    poshmarkItem.PictureUrl = item.picture_url;
                    poshmarkItem.AggregatesShares = item.aggregates.shares;
                    poshmarkItem.AggregatesComments= item.aggregates.comments;
                    poshmarkItem.AggregatesLikes = item.aggregates.likes;

                    foreach (var comment in item.comments)
                    {
                        poshmarkItem.Comments.Add(comment);
                    }
                    foreach (var evnt in item.events)
                    {
                        poshmarkItem.Events.Add(evnt);
                    }
                    foreach (var share in item.event_host_shares)
                    {
                        poshmarkItem.Comments.Add(share);
                    }
                    foreach (var like in item.likes)
                    {
                        poshmarkItem.Likes.Add(like);
                    }

                    poshmarkItem.PoshPassEligible = item.posh_pass_eligible;
                    poshmarkItem.CreatorUsername = item.creator_username;
                    poshmarkItem.CreatorDisplayHandle = item.creator_display_handle;
                    poshmarkItem.CreatorFullName = item.creator_full_name;
                    poshmarkItem.CreatorFbID = item.creator_fb_id;
                    poshmarkItem.CreatorPictureURL = item.creator_picture_url;
                    

                    poshmarkItem.DepartmentID = item.department.id;
                    poshmarkItem.DepartmentDisplay = item.department.display;
                    poshmarkItem.DepartmentSlug = item.department.slug;

                    poshmarkItem.CategoryV2ID = item.category_v2.id;
                    poshmarkItem.CategoryV2Display = item.category_v2.display;
                    poshmarkItem.CategoryV2Slug = item.category_v2.slug;

                    foreach (var category in item.category_features)
                    {
                        poshmarkItem.CategoryFeaturesID.Add(category.id);
                        poshmarkItem.CategoryFeaturesDisplay.Add(category.display);
                        poshmarkItem.CategoryFeaturesSlug.Add(category.slug);
                    }

                    poshmarkItem.SizeObjID = item.size_obj.id;
                    poshmarkItem.SizeObjDisplay = item.size_obj.display;
                    poshmarkItem.SizeObjDisplayWithSizeSet = item.size_obj.display_with_size_set;

                    poshmarkItem.BrandObjID = item.brand_obj.id;
                    poshmarkItem.BrandObjCanonicalName = item.brand_obj.canonical_name;
                    poshmarkItem.BrandObjSlug = item.brand_obj.slug;

                    result.Add(poshmarkItem);
                }
            } 
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (result.Count < itemsPerPage)
            {
                return result;
            }

            var x = JObject.Parse(json);
            newMaxID = x["more"]["next_max_id"].ToString();

            return result;
        }
    }
}
