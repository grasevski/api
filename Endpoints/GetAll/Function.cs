using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ociusApi
{
    public class Function
    {
        public async Task<ApiResponse> FunctionHandler(JObject request)
        {
            var queryString = request["queryStringParameters"];
            var resource = request["resource"].ToString();
            var page = resource.Split('/').Last();

            // Console.WriteLine($"resource: {resource}");
            switch (page)
            {
                case "contacts":
                    return await ApiResponse.GetContacts();
                case "locations":
                    //todo default behaviour no query
                    return await ApiResponse.GetByTimespan(queryString);
                default:
                    return await ApiResponse.GetLatest();
            }

            // return queryString.HasValues
            //     ? await ApiResponse.GetByTimespan(queryString)
            //     : await ApiResponse.GetLatest();
        }
    }
}
