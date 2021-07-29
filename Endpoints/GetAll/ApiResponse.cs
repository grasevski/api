﻿using Amazon.DynamoDBv2.Model;
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
            var drones = await Database.GetLatest(Today, supportedDroneNames);
            var dronesJson = JsonConvert.SerializeObject(drones);
            return CreateApiResponse(dronesJson);
        }

        public static async Task<ApiResponse> GetByTimespan(JToken queryString)
        {
            Console.WriteLine("Loading timespan data");
            var supportedDroneNames = await Database.GetSupportedDrones();
            var timespan = queryString.ToObject<Timespan>();

            if (!Database.IsValidTimePeriod(timespan.Value)) return null;

            var ticks = Database.GetTimespan(timespan.Value);

            Console.WriteLine("TICKS " + ticks);

            var utcMidnight = new DateTimeOffset(DateTime.UtcNow.Date).ToUnixTimeMilliseconds();
            Console.WriteLine("UTC MIDNIGHT timestamp: " + utcMidnight);

            var droneTimespans = await Database.GetByTimespan(Today, supportedDroneNames, timespan.Value);
            if (ticks < utcMidnight)
            {
                Console.WriteLine("FROM YESTERDAY");
                var dataFromYesterday = await Database.GetByTimespan(Yesterday, supportedDroneNames, timespan.Value);
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
