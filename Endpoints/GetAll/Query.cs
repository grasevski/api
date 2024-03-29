﻿using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using ociusApi.Models;

namespace ociusApi
{
    public class Query
    {
        public static bool IsValidResponse(QueryResponse queryResponse)
        {
            return queryResponse != null && queryResponse.Items != null && queryResponse.Items.Any();
        }

        public static QueryRequest CreateSupportedDronesRequest()
        {
            return new QueryRequest
            {
                TableName = "APIConfiguration",
                KeyConditionExpression = "Setting = :partitionKeyVal",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":partitionKeyVal", new AttributeValue { S = "Drones" } } },
                Limit = 1
            };
        }

        public static QueryRequest CreateDelayRequest()
        {
            return new QueryRequest
            {
                TableName = "APIConfiguration",
                KeyConditionExpression = "Setting = :partitionKeyVal",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":partitionKeyVal", new AttributeValue { S = "Delay" } } },
                Limit = 1
            };
        }

        public static int ParseDelayResponse(QueryResponse delayResponse)
        {
            // assumes every drone has a name, this is a valid assumpuption since the name is the partition key
            // If the table is changed, this may not be a valid assumption
            if (!IsValidResponse(delayResponse))
            {
                Console.WriteLine("Invalid Delay Response");
                return 0;
            }

            return GetDelayValue(delayResponse.Items[0]);
        }

        public static List<string> ParseSupportedDroneResponse(QueryResponse supportedDronesResponse)
        {
            // assumes every drone has a name, this is a valid assumpuption since the name is the partition key
            // If the table is changed, this may not be a valid assumption
            if (!IsValidResponse(supportedDronesResponse))
            {
                Console.WriteLine("Invalid supported drones response");
                return new List<string>();
            }
            List<string> droneNames = supportedDronesResponse.Items[0]["Value"]?.S.Split(",").ToList();
            return droneNames;
        }

        public static QueryRequest CreateLatestDronesRequest(string date, long timestamp, string droneName)
        {

            var partitionKeyValue = droneName + date;
            return new QueryRequest
            {
                TableName = "DroneDataSensors",
                KeyConditionExpression = "#partitionKeyName = :partitionKeyValue and #sortKeyName <= :timestamp",
                ExpressionAttributeNames = new Dictionary<string, string> { { "#partitionKeyName", "DroneName+Date" }, { "#sortKeyName", "Timestamp" } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    { ":partitionKeyValue", new AttributeValue { S = partitionKeyValue } },
                    { ":timestamp", new AttributeValue { N = timestamp.ToString()} }
                },
                ScanIndexForward = false,
                Limit = 1
            };
        }

        public static DroneSensor ParseLatestDroneRequest(QueryResponse queryResponse)
        {
            if (!IsValidResponse(queryResponse))
            { // Double validity check Database.cs:35
                Console.WriteLine("Invalid latest drone response");
                return new DroneSensor();
            }
            return DroneSensor.CreateDrone(queryResponse.Items[0]);
        }

        public static List<DroneLocation> ParseDroneByTimeRequest(QueryResponse queryResponse)
        {
            if (!IsValidResponse(queryResponse))
            {
                Console.WriteLine("Invalid drone time span response");
                return new List<DroneLocation>();
            }
            return queryResponse.Items.Select(loc => DroneLocation.CreateDrone(loc)).ToList();
        }

        public static QueryRequest CreateDroneByTimeRequest(string date, string droneName, long earliest, long latest)
        {
            var partitionKeyValue = droneName + date;
            return new QueryRequest
            {
                TableName = "DroneDataLocations",
                KeyConditionExpression = "#partitionKeyName = :partitionKeyValue and #sortKeyName BETWEEN :earliest AND :latest",
                ExpressionAttributeNames = new Dictionary<string, string> { { "#sortKeyName", "Timestamp" }, { "#partitionKeyName", "DroneName+Date" } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    { ":partitionKeyValue", new AttributeValue { S = partitionKeyValue } },
                    { ":earliest", new AttributeValue { N = earliest.ToString() } },
                    { ":latest", new AttributeValue { N = latest.ToString()} },
                    { ":false", new AttributeValue { BOOL = false } }
                },
                FilterExpression = "IsSensitive = :false",
                ScanIndexForward = false
            };
        }


        private static int GetDelayValue(Dictionary<string, AttributeValue> delayItem) {
            var delay = delayItem["Override"].BOOL ?  delayItem["OverrideValue"].N : delayItem["Value"].N;
            return Convert.ToInt32(delay);
        }
    }
}
