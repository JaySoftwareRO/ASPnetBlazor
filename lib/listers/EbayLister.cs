using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib.listers
{
    public class EbayLister: Lister
    {
        public static void List()
        {
        }

        public string Categories()
        {
            var client = new RestClient("https://ebaybukativ1.p.rapidapi.com/getCategories");
            var request = new RestRequest(Method.POST);
            request.AddHeader("x-rapidapi-host", "eBayBukatiV1.p.rapidapi.com");
            request.AddHeader("x-rapidapi-key", "51c50d1a15msh5f7b5c92161bf31p1c3eb0jsn43cdf69115bc");
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            IRestResponse response = client.Execute(request);

            return response.Content;
        }
    }
}
