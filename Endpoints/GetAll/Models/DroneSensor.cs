﻿using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace ociusApi
{
    public class DroneSensor
    {
        public string Name { get; set; }
        public string Timestamp { get; set; }
        public string Status { get; set; }
        public string Mode { get; set; }
        public string Sail_mode { get; set; }
        public Props Props { get; set; }
        
        public static string ToJson(QueryResponse queryResponse)
        {
            if (!IsValidResponse(queryResponse)) return "There were no results for that time range";

            var drones = queryResponse.Items.Select(item => CreateDrone(item));

            return JsonConvert.SerializeObject(drones);
        }

        private static bool IsValidResponse(QueryResponse queryResponse)
        {
            return queryResponse != null && queryResponse.Items != null && queryResponse.Items.Any();
        }

        private static DroneSensor CreateDrone(Dictionary<string, AttributeValue> attributes)
        {
            var drone = new DroneSensor();
            var coordinates = new Coordinates();
            var location = new Location();
            var props = new Props();

            foreach (KeyValuePair<string, AttributeValue> kvp in attributes)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                if (key == "Name") drone.Name = value?.S ?? "";
                if (key == "Timestamp") drone.Timestamp = value?.N ?? "";
                if (key == "Status") drone.Status = value?.S ?? "";
                if (key == "Mode") drone.Mode = value?.S ?? "";

                if (key == "Water_depth") props.Water_depth = value?.S ?? "";
                if (key == "Water_speed") props.Water_speed = value?.S ?? "";
                if (key == "Water_temp") props.Water_temp = value?.S ?? "";
                if (key == "Wind_Speed") props.Wind_Speed = value?.S ?? "";
                if (key == "Wind_direction") props.Wind_direction = value?.S ?? "";
                if (key == "Heading") props.Heading = value?.S ?? "";

                if (key == "Lat") coordinates.Lat = value?.S ?? "";
                if (key == "Lon") coordinates.Lon = value?.S ?? "";
            }

            location.Coordinates = coordinates;
            props.Location = location;
            drone.Props = props;

            return drone;
        }
    }
}