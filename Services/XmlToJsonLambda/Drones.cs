using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace XmlToJson
{
    public class Drones
    {
        public JToken Data { get; private set; }
        public Dictionary<string, string> Names { get; private set; }
        public int Delay { get; private set; }

        public static async Task<Drones> GetDrones()
        {
            var data = await GetDroneData();
            var nameJson = await GetDroneNameJson();
            var names = MapIdToName(nameJson);
            var delay = GetDelay(nameJson);
            return new Drones { Data = data, Names = names, Delay=delay };
        }

        #region Private methods



        private static async Task<JToken> GetDroneNameJson()
        {
            var droneNames = await AuthApi.GetXml("?listrobots&nodeflate");

            var data = Json.FromXml(droneNames);
            var nameJson = JsonConvert.DeserializeObject(data) as JObject;
            

            return nameJson["Response"]["robot"];
        }


        private static int GetDelay(JToken robots)
        {
            Console.WriteLine($"delay: {robots.First["delay_days"]}");
            return robots.HasValues ? robots.First["delay_days"].ToObject<int>() : 0;
        }


        private static async Task<JToken> GetDroneData()
        {

            var droneStatus = await AuthApi.GetXml("?mavstatus&nodeflate");
            var statusJson = Json.FromXml(droneStatus);
            
            return GetDroneJson(statusJson);
        }

        private static Dictionary<string, string> MapIdToName(JToken robots)
        {
            var result = new Dictionary<string, string>();

            foreach (var robot in robots)
            {
                var id = robot["sysid"].ToString();
                var name = robot["robotid"].ToString();
                result.Add(id, name);
            }

            return result;
        }

        private static JToken GetDroneJson(string rawData)
        {
            var json = JsonConvert.DeserializeObject(rawData) as JObject;
            var data = json["Response"]["File"];
            return data["Vehicle"];
        }

        [Obsolete]
        private static async Task<Dictionary<string, string>> GetDroneNames()
        {
            var droneNames = await AuthApi.GetXml("?listrobots&nodeflate");
            var nameJson = Json.FromXml(droneNames);
            return MapIdToName(nameJson);
        }

        #endregion
    }
}
