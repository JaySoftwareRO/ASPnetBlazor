using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Helpers;

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

        public static List<dynamic> Request(string username)
        {
            var result = new List<dynamic>();
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

        private static List<dynamic> Request(string username, string maxID, out string newMaxID)
        {
            newMaxID = string.Empty;
            var result = new List<dynamic>();

            var baseURL = RequestUri(username);
            var filter = GetRequestFilter(itemsPerPage, maxID);

            var uBuilder = new UriBuilder(baseURL);
            uBuilder.Query = $"request={filter}";


            var json = new WebClient().DownloadString(uBuilder.Uri);
            dynamic deserializedJson = JsonConvert.DeserializeObject<dynamic>(json);

            foreach (var item in deserializedJson.data)
            {
                result.Add(item);
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
