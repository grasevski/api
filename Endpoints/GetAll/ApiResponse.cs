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

        private static string Today => GetDate(0);
        private static string Yesterday => GetDate(-1);

        public static async Task<ApiResponse> GetLatest()
        {
            Console.WriteLine("Loading latest data");
            var supportedDroneNames = await Database.GetSupportedDrones();
            var delay = await Database.GetDelay();

            //TODO fix
            var date = GetDate(-delay);
            var timestamp = DateTimeOffset.UtcNow.AddDays(-delay).ToUnixTimeMilliseconds();

            //Date and timestamp both needed?
            var drones = await Database.GetLatest(date, timestamp, supportedDroneNames);
            var dronesJson = JsonConvert.SerializeObject(drones);
            //dronesJson = $"[{dronesJson}, {{\"delay\": \"{delay}\"}}, {{\"new_timestamp\": \"{timestamp}\"}}]";
            return CreateApiResponse(dronesJson);
        }



        public static async Task<ApiResponse> GetContacts()
        {
            Console.WriteLine("Loading Contacts");
            var supportedDroneNames = await Database.GetSupportedDrones();
            var delay = await Database.GetDelay();

            //TODO fix
            var date = GetDate(-delay);
            var timestamp = DateTimeOffset.UtcNow.AddDays(-delay).ToUnixTimeMilliseconds();


            var contacts = await Database.GetContacts(date, timestamp, supportedDroneNames);

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
            var supportedDroneNames = await Database.GetSupportedDrones();
            var timespan = queryString.ToObject<Timespan>() ?? new Timespan();
            if (!Database.IsValidTimePeriod(timespan.Value)) return null;

            var delay = await Database.GetDelay();

            //TODO fix
            var date = GetDate(-delay);
            var timestamp = DateTimeOffset.UtcNow.AddDays(-delay).ToUnixTimeMilliseconds();

            // Can simplify this logic: always 2 partitions worth of trail
            var ticks = Database.GetTimespan(timespan.Value);

            //Debug
            Console.WriteLine($"Log 1: {DateTimeOffset.UtcNow.AddDays(-delay)}");
            Console.WriteLine($"Log 2: {DateTime.UtcNow.AddDays(-delay)}");
            // 
            var delayed_ticks = ticks + timestamp - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            Console.WriteLine("TICKS " + ticks);

            var utcMidnight = new DateTimeOffset(DateTime.UtcNow.AddDays(-delay).Date).ToUnixTimeMilliseconds();
            Console.WriteLine("UTC MIDNIGHT timestamp: " + utcMidnight);

            var droneTimespans = await Database.GetByTimespan(GetDate(-delay), supportedDroneNames, delayed_ticks, timestamp);
            if (delayed_ticks < utcMidnight)
            {
                Console.WriteLine("FROM YESTERDAY");
                var dataFromYesterday = await Database.GetByTimespan(GetDate(-delay - 1), supportedDroneNames, delayed_ticks, timestamp);
                droneTimespans.AddRange(dataFromYesterday);
            }

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


        public static string GetDate(int offset)
        {
            return DateTime.UtcNow.AddDays(offset).ToString("yyyyMMdd");
        }
    }
}
