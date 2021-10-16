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

namespace RawDataToClientData {
        // Defines serialisation format
    public class ContactOutputFormat : DefaultContractResolver
    {

        public static ContactOutputFormat Instance { get; } = new ContactOutputFormat();

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


    public abstract class Contact
    {
        [JsonProperty("sensorid")]
        public string SensorId { get; set; } = "0";

        [JsonProperty("contactid")]
        public string ContactId { get; set; } = "0";

        [JsonProperty("phase")]
        public string Phase { get; set; } = "0";

        private string _name = "";


        [JsonProperty("name")]
        public string Name
        {
            get { return _name; }
            set { _name = value.Trim(); }
        }

        public Dictionary<string, string> Location = new Dictionary<string, string>
        {
            {"Lat","0"},
            {"Lon","0"},
        };

        [JsonProperty("lat")]
        private string Lat { set { Location["Lat"] = value; } }

        [JsonProperty("lon")]
        private string Lon { set { Location["Lon"] = value; } }

        [JsonProperty("alt")]
        public string Alt { get; set; } = "0";

        [JsonProperty("hdg")]
        public string Heading { get; set; } = "0";

        [JsonProperty("cog")]
        public string Cog { get; set; } = "0";

        [JsonProperty("vel")]
        public string Vel { get; set; } = "0";

        [JsonProperty("sog")]
        private string Sog { set { Vel = value; } }

        [JsonProperty("range")]
        public string Range { get; set; } = "0";

        [JsonProperty("bearing")]
        public string Bearing { get; set; } = "0";

        // [JsonProperty("our_lat")]
        // public string OurLat { get; set; } = "0";

        // [JsonProperty("our_lon")]
        // public string OurLon { get; set; } = "0";

        // [JsonProperty("our_alt")]
        // public string OurAlt { get; set; } = "0";

        // [JsonProperty("our_hdg")]
        // public string OurHdg { get; set; } = "0";

        [JsonProperty("lup")]
        public string LastUpdated { get; set; } = "0";

        [JsonProperty("fup")]
        public string FirstSeen { get; set; } = "0";

        [JsonProperty("age")]
        public string Age { get; set; } = "0";

        // [JsonProperty("init_time")]
        // public string InitTime { get; set; } = "0";

        // [JsonProperty("info_time")]
        // public string InfoTime { get; set; } = "0";


        [JsonIgnore]
        public abstract bool TimedOut { get; }
    }

    public class Radar_Contact : Contact
    {
        public string SensorType => "RADAR";
        public override bool TimedOut => !(Int32.Parse(Age) <= 30 && Int32.Parse(Phase) == 3);
    }

    public class Unknown_Contact : Contact
    {
        public string SensorType => "Unknown";
        public override bool TimedOut => true;
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
        private string EmitterType { set; get; } = "0";

        public string VehicleType => EmitterTypeADSB[int.Parse(EmitterType ?? "0")];

        public string Url { get; set; } = "";

        public string CallSign => Name;

        public string CallsignUrl { get; set; } = "";

        public override bool TimedOut => !(Int32.Parse(Age) <= 300);

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
        public string Mmsi { get; set; } = "0";

        [JsonProperty("st")]
        private string ShipType { get; set; } = "";

        public string VehicleType => ShipType;

        [JsonProperty("len")]
        public string Length { get; set; } = "0";

        [JsonProperty("beam")]
        public string Beam { get; set; } = "0";

        // [JsonProperty("ps")]
        // public string Ps { get; set; } = "";

        // [JsonProperty("pb")]
        // public string Pb { get; set; } = "";

        [JsonProperty("class")]
        public string AisClass { get; set; } = "";

        [JsonProperty("cs")]
        public string Callsign { get; set; } = "";

        public string Url { get; set; } = "";

        public override bool TimedOut => !(Int32.Parse(Age) <= 1800);

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
}