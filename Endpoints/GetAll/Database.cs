using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ociusApi.Models;

namespace ociusApi
{
    public static class Database
    {
        private static readonly AmazonDynamoDBClient client = new AmazonDynamoDBClient();

        public async static Task<List<string>> GetSupportedDrones()
        {
            var supportedDronesRequest = Query.CreateSupportedDronesRequest();
            var response = await client.QueryAsync(supportedDronesRequest);
            return Query.ParseSupportedDroneResponse(response);
        }

        public async static Task<int> GetDelay()
        {
            var delayRequest = Query.CreateDelayRequest();
            var delayResponse = await client.QueryAsync(delayRequest);
            return Query.ParseDelayResponse(delayResponse);
        }

        [Obsolete]
        public async static Task<List<DroneSensor>> GetLatest(string date, long timestamp, List<string> supportedDroneNames)
        {
            var droneRequestTasks = new List<Task<DroneSensor>>();

            foreach (var droneName in supportedDroneNames)
            {
                droneRequestTasks.Add(QueryClientForDroneAsync(date, timestamp, droneName));
            }

            var drones = await Task.WhenAll(droneRequestTasks);

            return new List<DroneSensor>(drones.Where(drone => DroneSensor.IsValidDrone(drone)));
        }

        public async static Task<List<DroneSensor>> GetDroneByDateTimeAsync(DateTimeOffset dateTime, List<string> supportedDroneNames)
        {
            var partitionDate = dateTime.ToString("yyyyMMdd");
            var timestamp = dateTime.ToUnixTimeMilliseconds();

            var droneRequestTasks = new List<Task<DroneSensor>>();

            foreach (var droneName in supportedDroneNames)
            {
                droneRequestTasks.Add(QueryClientForDroneAsync(partitionDate, timestamp, droneName));
            }

            var drones = await Task.WhenAll(droneRequestTasks);

            return new List<DroneSensor>(drones.Where(drone => DroneSensor.IsValidDrone(drone)));
        }

        public async static Task<List<string>> GetContactsByDateTimeAsync(DateTimeOffset dateTime, List<string> supportedDroneNames)
        {
            var partitionDate = dateTime.ToString("yyyyMMdd");
            var timestamp = dateTime.ToUnixTimeMilliseconds();


            var contactsRequestTasks = new List<Task<string>>();

            foreach (var droneName in supportedDroneNames)
            {
                contactsRequestTasks.Add(QueryClientForContactsAsync(partitionDate, timestamp, droneName));
            }

            var contacts = await Task.WhenAll(contactsRequestTasks);

            return new List<string>(contacts.AsEnumerable());
        }

        public async static Task<List<DroneLocation>> GetByTimespan(string partitionDate, List<string> supportedDroneNames, DateTimeOffset earliest, DateTimeOffset latest)
        {
            var droneTimespans = new List<DroneLocation>();


            var timespanRequestTasks = new List<Task<List<DroneLocation>>>();

            foreach (var droneName in supportedDroneNames)
            {
                timespanRequestTasks.Add(QueryClientForTimespanAsync(partitionDate, droneName, earliest.ToUnixTimeMilliseconds(), latest.ToUnixTimeMilliseconds()));
            }

            var databaseResponses = await Task.WhenAll(timespanRequestTasks);

            foreach (List<DroneLocation> droneTimespan in databaseResponses)
            {
                droneTimespans.AddRange(droneTimespan);
            }

            return droneTimespans;
        }

        public static bool IsValidTimePeriod(string timespan)
        {
            var validTimespans = new List<string> { "minute", "hour", "day" };

            return (validTimespans.Contains(timespan));
        }

        public static long TimespanToMilliseconds(string timeSpan)
        {
            var oneMinuteMilliseconds = 60000;
            var oneHourMilliseconds = 3600000;
            var oneDayMilliseconds = 86400000;

            if (timeSpan == "day")
                return oneDayMilliseconds;

            if (timeSpan == "hour")
                return oneHourMilliseconds;

            return oneMinuteMilliseconds;
        }

        private async static Task<DroneSensor> QueryClientForDroneAsync(string date, long timestamp, string droneName)
        {
            var latestDronesRequest = Query.CreateLatestDronesRequest(date, timestamp, droneName);
            QueryResponse databaseResponse = await client.QueryAsync(latestDronesRequest);

            if (!Query.IsValidResponse(databaseResponse))
            {
                Console.WriteLine($"No entries found for {droneName}");
                return new DroneSensor();
            }

            return Query.ParseLatestDroneRequest(databaseResponse);
        }


        private async static Task<string> QueryClientForContactsAsync(string date, long timestamp, string droneName)
        {
            var latestDronesRequest = Query.CreateLatestDronesRequest(date, timestamp, droneName);
            QueryResponse databaseResponse = await client.QueryAsync(latestDronesRequest);

            if (!Query.IsValidResponse(databaseResponse))
            {
                Console.WriteLine($"No entries found for {droneName}");
                return string.Empty;
            }

            return databaseResponse.Items[0]["Contacts"]?.S ?? string.Empty;
        }

        private async static Task<List<DroneLocation>> QueryClientForTimespanAsync(string date, string droneName, long earliest, long latest)
        {
            var dronesByTimespanRequest = Query.CreateDroneByTimeRequest(date, droneName, earliest, latest);
            QueryResponse databaseResponse = await client.QueryAsync(dronesByTimespanRequest);

            if (!Query.IsValidResponse(databaseResponse))
            {
                Console.WriteLine($"No timeline found for {droneName} on {date} from {earliest} to {latest}");
                return new List<DroneLocation>();
            }
            return Query.ParseDroneByTimeRequest(databaseResponse);
        }
    }
}
