using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using RawDataToClientData.Models;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawDataToClientData.Repositories;
using System.Runtime.Serialization;

using System.ComponentModel; // Debug
using System.Text;
namespace RawDataToClientData
{

    // Defines serialisation format
    public class ContactOutputFormat : DefaultContractResolver
    {

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);

            foreach (var prop in properties)
            {
                prop.PropertyName = prop.UnderlyingName;
            }

            return properties;
        }
    }

    public class Contact
    {

        // this.age = Infinity;
        [JsonProperty("sensorid")]
        public string SensorId { get; set; }

        [JsonProperty("contactid")]
        public string ContactId { get; set; }

        [JsonProperty("phase")]
        public string Phase { get; set; }


        [JsonProperty("name")]
        public string Name { get; set; } = ""; //TODO set defaults for all

        [JsonProperty("lat")]
        public string Lat { get; set; }

        [JsonProperty("lon")]
        public string Lon { get; set; }// lng?


        //TODO handle missing case
        [JsonProperty("alt")]
        public string Alt { get; set; }


        [JsonProperty("hdg")]
        public string Hdg { get; set; }

        [JsonProperty("cog")]
        public string Cog { get; set; }

        [JsonProperty("sog")]
        private string Sog { set { Vel = value; } }

        [JsonProperty("vel")]
        public string Vel { get; set; }


        [JsonProperty("range")]
        public string Range { get; set; }

        [JsonProperty("bearing")]
        public string Bearing { get; set; }

        [JsonProperty("our_lat")]
        public string OurLat { get; set; }

        [JsonProperty("our_lon")]
        public string OurLon { get; set; }

        [JsonProperty("our_alt")]
        public string OurAlt { get; set; }

        [JsonProperty("our_hdg")]
        public string OurHdg { get; set; }

        [JsonProperty("lup")]
        public string Lup { get; set; }

        [JsonProperty("fup")]
        public string Fup { get; set; }

        [JsonProperty("age")]
        public string Age { get; set; }

        [JsonProperty("init_time")]
        public string InitTime { get; set; }

        [JsonProperty("info_time")]
        public string InfoTime { get; set; }
    }

    public class Radar_Contact : Contact
    {
        public string SensorType => "RADAR";
    }

    public class Unknown_Contact : Contact
    {
        public string SensorType => "Unknown";
    }


    //TODO ondeserialise
    public class ADSB_Contact : Contact
    {

        public static readonly IList<string> EmitterTypeADSB = new List<string>
        {
            "No Info", "Light", "Small", "Large", "High Vortex Large", "Heavy", "Highly Maneuverable",
            "Rotocraft", "Unassigned", "Glider", "Lighter Air", "Parachute", "Ultra Light", "Unassigned2",
            "UAV", "Space", "Unassigned3", "Emergency Surface", "Service Surface", "Point Obstacle"
        };

        public string SensorType => "ADSB";

        private int _icao = 0;

        //  = parseFloat(xml.find('icao').text()).toString(16).toUpperCase(); //adsb specific
        [JsonProperty("icao")]
        public string Icao
        {
            set { _icao = int.Parse(value); }
            get { return _icao.ToString("X"); }
        }

        //  = EmitterTypeADSB[parseInt(xml.find('et').text())]; //https://mavlink.io/en/messages/common.html#ADSB_EMITTER_TYPE
        [JsonProperty("et")]
        [JsonIgnore]
        private string EmitterType { set; get; }

        public string VehicleType { get { return EmitterTypeADSB[int.Parse(EmitterType ?? "0")]; } }

        public string Url { get; set; } = "";

        public string CallsignUrl { get; set; } = "";

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            //TODO change empty handling
            if (Icao != null)
            {
                Url = "https://opensky-network.org/aircraft-profile?icao24=" + Icao.Trim();
            }

            if (Name != null)
            {
                CallsignUrl = "https://www.flightradar24.com/" + Name.Trim();
            }
        }
    }

    public class AIS_Contact : Contact
    {

        public string SensorType => "AIS";

        [JsonProperty("mmsi")]
        public string Mmsi { get; set; }

        [JsonProperty("st")]
        public string ShipType { get; set; }

        public string VehicleType { get { return ShipType; } }

        [JsonProperty("len")]
        public string Len { get; set; }

        [JsonProperty("beam")]
        public string Beam { get; set; }

        [JsonProperty("ps")]
        public string Ps { get; set; }

        [JsonProperty("pb")]
        public string Pb { get; set; }

        [JsonProperty("class")]
        public string AisClass { get; set; }

        [JsonProperty("cs")]
        public string Callsign { get; set; }

        public string Url { get; set; } = "";

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            //TODO change empty handling
            if (Mmsi != null)
            {
                Url = "http://www.marinetraffic.com/en/ais/details/ships/mmsi:" + Mmsi;
                // I could parse this into href tag
                // or into a nested object
            }
        }
    }

    public class ContactFactory
    {

        public static IList<Contact> DeserialiseContact(JToken json)
        {
            if (json is JArray)
            {
                return DeserialiseContact(json as JArray);
            }

            if (json is JObject)
            {
                return new List<Contact> { DeserialiseContact(json as JObject) };
            }

            return new List<Contact> { };
        }

        public static IList<Contact> DeserialiseContact(JArray json)
        {
            var contacts = new List<Contact> { };
            foreach (JObject contactJson in json)
            {
                contacts.Add(DeserialiseContact(contactJson));
            }

            return contacts;
        }


        public static Contact DeserialiseContact(JObject contactJson)
        {
            var sensorType = contactJson["sensortype"]?.ToString();

            Contact contact;
            switch (sensorType)
            {
                case "1":
                    contact = contactJson.ToObject<AIS_Contact>();
                    break;
                case "8":
                    contact = contactJson.ToObject<ADSB_Contact>();
                    break;
                case "12":
                case "13":
                    contact = contactJson.ToObject<Radar_Contact>();
                    break;
                default:
                    contact = new Unknown_Contact(); //TODO replace
                    break;
            }

            return contact;
        }
    }

    public class DroneSensors
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string Air_temp { get; set; }
        public string Water_depth { get; set; }
        public string Water_temp { get; set; }
        public string Wind_speed { get; set; }
        public string Wind_direction { get; set; }
        public string Current_speed { get; set; }
        public string Current_direction { get; set; }
        public string Boat_speed { get; set; }
        public string Heading { get; set; }
        public string Lat { get; set; }
        public string Lon { get; set; }
        public string Batteries { get; set; }
        public string BatteryPercentages { get; set; }

        public string CameraAliases { get; set; }
        public string Cameras { get; set; }
        public bool IsSensitive { get; set; }
        public string Contacts { get; set; }

        public async static Task<string> GetSensors(string name, string data, string aliases, string cameras)
        {
            var json = JsonConvert.DeserializeObject(data) as JObject;
            var mavpos = json["mavpos"] ?? new JObject();

            var water_depth = mavpos["water_dep"] ?? "0";
            var water_temp = mavpos["water_tmp"] ?? "0";
            var boat_speed = mavpos["groundspeed"] ?? "0";

            var weatherData = mavpos["WEATHER_DATA"] ?? new JObject();
            var wind_speed = weatherData["wind_spd"] ?? "0";
            var wind_direction = weatherData["wind_dir"] ?? "0";
            var air_temp = weatherData["air_temp"] ?? "0";

            var waterVelocity = mavpos["WATER_VELOCITY"] ?? new JObject();
            var current_speed = waterVelocity["curr_spd"] ?? "0";
            var current_direction = waterVelocity["curr_dir"] ?? "0";

            var batteryVoltages = new List<string>();
            var batteryPercentages = new List<string>();

            // var contacts_json = json["contacts"]?["contact"] ?? new JObject();
            var contactsJson = (json["contacts"] as JObject)?["contact"] ?? new JObject();
            parseContacts(contactsJson);


            if (json.ContainsKey("tqb"))
            {
                Console.WriteLine($"found tqb for {name}");
                var batteryData = new Batteries { };
                if (json["tqb"].Type == JTokenType.Array)
                {
                    batteryData = JsonConvert.DeserializeObject<Batteries>(data);
                }
                else
                {
                    batteryData.Tqb.Add(JsonConvert.DeserializeObject<Battery>(json["tqb"].ToString()));
                }
                batteryVoltages = batteryData.Tqb.Select(battery => DroneUtils.ParseVoltage(battery)).ToList();
                batteryPercentages = batteryData.Tqb.Select(battery => battery.Pcnt).ToList();
            }


            var isSensitive = await SensitivityRepository.GetDroneSensitivity(name);

            var location = await DroneLocation.GetLocation(name, data);
            var lat = location.Lat;
            var lon = location.Lon;
            var heading = location.Heading;

            var llLastMsg = mavpos["llLastMsg"] ?? "0";
            var status = IsActive(llLastMsg.Value<long>()) ? "Active" : "Inactive";


            var sensors = new DroneSensors
            {
                Name = name,
                Status = status,
                Water_depth = water_depth.ToString(),
                Air_temp = air_temp.ToString(),
                Water_temp = water_temp.ToString(),
                Wind_speed = wind_speed.ToString(),
                Wind_direction = wind_direction.ToString(),
                Current_speed = current_speed.ToString(),
                Current_direction = current_direction.ToString(),
                Boat_speed = boat_speed.ToString(),
                Heading = heading.ToString(),
                Lat = lat,
                Lon = lon,
                Batteries = String.Join(',', batteryVoltages),
                BatteryPercentages = String.Join(',', batteryPercentages),
                CameraAliases = aliases.Trim(','),
                Cameras = cameras,
                IsSensitive = isSensitive,
                Contacts = contactsJson.ToString()
            };
            return JsonConvert.SerializeObject(sensors);
        }

        private static void parseContacts(JToken json)
        {

            var settings = new JsonSerializerSettings { ContractResolver = new ContactOutputFormat() };

            var contacts = ContactFactory.DeserialiseContact(json).Where( c => !(c is Unknown_Contact));

            var groups = contacts.GroupBy(c => c.Name).Where(g => g.Count() > 1);

            foreach (var group in groups) {
                Console.WriteLine(group.Key);
                foreach (var item in group) {
                    Console.WriteLine(JsonConvert.SerializeObject(item, settings));
                }
            }




            // Console.WriteLine("Contact factory:");
            // foreach (var contact in contacts)
            // {
            //     Console.WriteLine(JsonConvert.SerializeObject(contact, settings));
            // }
        }


        private static bool IsActive(long usvTimestamp)
        {
            var oneHourMilliseconds = 3600000;
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - oneHourMilliseconds;

            return usvTimestamp > timestamp;
        }
    }
}
