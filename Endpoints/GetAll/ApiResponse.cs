using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ociusApi.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ociusApi
{
    public class ApiResponse
    {
        #region Properties

        [JsonProperty("isBase64Encoded")]
        public bool IsBase64Encoded = false;

        [JsonProperty("statusCode")]
        public int StatusCode { get; private set; }

        [JsonProperty("body")]
        public string Body { get; private set; }

        [JsonProperty("headers")]
        public IDictionary<string, string> Headers { get; private set; }

        #endregion


        public static async Task<ApiResponse> GetDronesDelayed()
        {
            Console.WriteLine("Loading latest data");
            var supportedDroneNamesTask = Database.GetSupportedDrones();
            var delayTask = Database.GetDelay();

            var delay = GetDateTime(await delayTask);
            var drones = await Database.GetDroneByDateTimeAsync(delay, await supportedDroneNamesTask);
            var dronesJson = JsonConvert.SerializeObject(drones);
            return CreateApiResponse(dronesJson);
        }

        public static async Task<ApiResponse> GetContacts()
        {
            Console.WriteLine("Loading Contacts");
            var supportedDroneNamesTask = Database.GetSupportedDrones();
            var delayTask = Database.GetDelay();

            var delay = GetDateTime(await delayTask);
            var contacts = await Database.GetContactsByDateTimeAsync(delay, await supportedDroneNamesTask);

            var contactsEnumerable =
                (from c in contacts
                 let json = JsonConvert.DeserializeObject(c) as JArray
                 where json != null
                 select json).SelectMany(jobj => jobj);

            var contactsJson = JsonConvert.SerializeObject(contactsEnumerable);
            return CreateApiResponse(contactsJson);
        }

        public static async Task<ApiResponse> GetByTimespan(JToken queryString)
        {
            Console.WriteLine("Loading timespan data");
            var supportedDroneNamesTask = Database.GetSupportedDrones();
            var delayTask = Database.GetDelay();

            var timespan = queryString.ToObject<Timespan>() ?? new Timespan();
            if (!Database.IsValidTimePeriod(timespan.Value)) return null;

            var latest = GetDateTime(await delayTask);
            var supportedDroneNames = await supportedDroneNamesTask;

            var earliest = latest.AddMilliseconds(-Database.TimespanToMilliseconds(timespan.Value));

            var droneTimespans = await Database.GetByTimespan(latest.ToString("yyyyMMdd"), supportedDroneNames, earliest, latest);

            Console.WriteLine("FROM YESTERDAY");
            var dataFromYesterday = await Database.GetByTimespan(earliest.ToString("yyyyMMdd"), supportedDroneNames, earliest, latest);
            droneTimespans.AddRange(dataFromYesterday);

            var dronesJson = JsonConvert.SerializeObject(FilterLocations(droneTimespans));
            return CreateApiResponse(dronesJson);
        }

        private static IEnumerable<ociusApi.DroneLocation> FilterLocations(List<DroneLocation> timespans)
        {
            return timespans.Where(x => x.Lat != "0");
        }
        private static ApiResponse CreateApiResponse(string json)
        {
            var headers = new Dictionary<string, string>() { { "Access-Control-Allow-Origin", "*" } };
            return new ApiResponse { StatusCode = 200, Body = json, Headers = headers };
        }

        private static DateTimeOffset GetDateTime(int dayOffset)
        {
            return DateTimeOffset.UtcNow.AddDays(-dayOffset);
        }

    }
}
