using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RawDataToClientData.Repositories
{
    public static class CameraRepository
    {
        private static readonly AmazonDynamoDBClient client = new AmazonDynamoDBClient();

        public static async Task<Dictionary<string, (string, string)>> GetCamerasByDate(DateTime date)
        {
            var cameraQuery = CreateCameraQueryByDate(date);
            var response = await client.QueryAsync(cameraQuery);
            return ParseCamerasResponse(response);
        }

        private static QueryRequest CreateCameraQueryByDate(DateTime date)
        {
            var formattedDateToPrimaryKey = date.ToString("M/d/yy");
            return new QueryRequest
            {
                TableName = "CameraImageUrls",
                KeyConditionExpression = "#date = :date",
                ExpressionAttributeNames = new Dictionary<string, string> { { "#date", "Date" } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":date", new AttributeValue { S = formattedDateToPrimaryKey } } },
                ScanIndexForward = false,
                Limit = 10 //TODO: make this handle any number of drone cameras
            };
        }

        [Obsolete("Use GetCamerasByDate instead")]
        public static async Task<Dictionary<string, (string, string)>> GetCameras()
        {
            var cameraQuery = CreateCameraQuery();
            var response = await client.QueryAsync(cameraQuery);
            return ParseCamerasResponse(response);
        }

        [Obsolete("Use CreateCameraQueryByDate instead")]
        private static QueryRequest CreateCameraQuery()
        {
            var date = DateTime.UtcNow;
            var formattedDateToPrimaryKey = date.ToString("M/d/yy");
            return new QueryRequest
            {
                TableName = "CameraImageUrls",
                KeyConditionExpression = "#date = :date",
                ExpressionAttributeNames = new Dictionary<string, string> { { "#date", "Date" } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":date", new AttributeValue { S = formattedDateToPrimaryKey } } },
                ScanIndexForward = false,
                Limit = 10 //TODO: make this handle any number of drone cameras
            };
        }

        private static Dictionary<string, (string, string)> ParseCamerasResponse(QueryResponse queryResponse)
        {
            if (!IsValidResponse(queryResponse))
            {
                Console.WriteLine("Invalid camera query response, returning empty dictionary");
                return new Dictionary<string, (string, string)>();
            }

            var cameras = new Dictionary<string, (string, string)>(); //TODO replace with class

            foreach (var item in queryResponse.Items)
            {
                var (droneName, cameraUrls, aliases) = ParseCameraResponseItem(item);
                Console.WriteLine($"Found camera for {droneName} Aliases:({aliases}) URL: {cameraUrls}");
                cameras.TryAdd(droneName, (aliases, cameraUrls));
            }

            return cameras;
        }

        private static (string, string, string) ParseCameraResponseItem(Dictionary<string, AttributeValue> attributes)
        {
            var droneName = string.Empty;
            var cameras = string.Empty;
            var aliases = string.Empty;

            foreach (KeyValuePair<string, AttributeValue> attribute in attributes)
            {
                if (attribute.Key == "Name") droneName = attribute.Value?.S ?? "";

                if (attribute.Key == "Cameras") cameras = attribute.Value?.S ?? "";

                if (attribute.Key == "Aliases") aliases = attribute.Value?.S ?? "";
            }

            return (droneName, cameras, aliases);
        }

        private static bool IsValidResponse(QueryResponse queryResponse)
        {
            return queryResponse != null && queryResponse.Items != null && queryResponse.Items.Any();
        }
    }
}
