using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GetCameraImages
{
    public static class Database
    {
        private static readonly AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        private static readonly Table table = Table.LoadTable(client, "CameraImageUrls");

        public async static Task<IEnumerable<string>> InsertCameraUrls(string date, long timestamp, DroneCamera drone, List<string> urls)
        {
            var validUrls = urls.Where(url => !url.StartsWith(Constants.ErrorPrefix)).Select(url => "https://images.ocius.com.au/" + url);
            var value = string.Join(",", validUrls);
            var cameraImageDocument = CreateCameraDocument(date, timestamp, drone, value);

            try
            {
                await table.PutItemAsync(cameraImageDocument);
                return validUrls;
            }
            catch (Exception ex)
            {
                var error = "Failed to save drone. Exception: " + ex;
                Console.WriteLine(error);
                return new List<string> { error };
            }
        }

        public async static Task<List<DroneCamera>> GetDroneCameras()
        {
            var cameraQuery = CreateCameraQuery();
            var response = await client.QueryAsync(cameraQuery);
            var duplicates = GetValuesFromResponse(response);
            //remove duplicates
            return duplicates.GroupBy(x => x.Name).Select(x => x.First()).ToList();
        }


        public static List<DroneCamera> GetValuesFromResponse(QueryResponse queryResponse)
        {
            var result = new List<DroneCamera>();

            if (!IsValidResponse(queryResponse)) return result;


            foreach (var item in queryResponse.Items)
            {
                var drone = ParseResponse(item);
                result.Add(drone);
            }

            return result;
        }

        public static DroneCamera ParseResponse(Dictionary<string, AttributeValue> attributes)
        {
            var result = new DroneCamera();

            foreach (KeyValuePair<string, AttributeValue> attribute in attributes)
            {
                switch (attribute.Key)
                {
                    case "Id": result.Id = attribute.Value?.S ?? ""; break;
                    case "Name": result.Name = attribute.Value?.S ?? ""; break;
                    case "Aliases": result.Aliases = attribute.Value?.S ?? ""; break;
                    case "Cameras":
                        var rawCameras = attribute.Value?.S ?? "";
                        result.Cameras = rawCameras.Trim(',').Split(',').ToList();
                        break;
                }
            }

            return result;
        }

        private static bool IsValidResponse(QueryResponse queryResponse)
        {
            return queryResponse != null && queryResponse.Items != null && queryResponse.Items.Any();
        }

        private static Document CreateCameraDocument(string date, long timestamp, DroneCamera drone, string cameras)
        {
            return new Document
            {
                ["Date"] = date,
                ["Timestamp"] = timestamp,
                ["Name"] = drone.Name,
                ["Cameras"] = cameras,
                ["Aliases"] = drone.Aliases,
            };
        }

        private static QueryRequest CreateCameraQuery()
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");

            return new QueryRequest
            {
                TableName = "CameraNames",
                KeyConditionExpression = "#date = :date",
                ExpressionAttributeNames = new Dictionary<string, string> { { "#date", "Date" } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":date", new AttributeValue { N = date } } },
                ScanIndexForward = false,
                Limit = 10 //If we have more than 10 drones, we'll need to revist this solution
            };
        }
    }
}
