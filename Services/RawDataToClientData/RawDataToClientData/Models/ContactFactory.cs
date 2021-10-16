using Newtonsoft.Json.Linq;
using System.Collections.Generic;


namespace RawDataToClientData {

    public class ContactFactory
    {
        public const string AIS_ID = "1";
        public const string ADSB_ID = "8";
        public const string ARPA_ID = "12";
        public const string MARPA_ID = "13";

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
                case AIS_ID:
                    contact = contactJson.ToObject<AIS_Contact>();
                    break;
                case ADSB_ID:
                    contact = contactJson.ToObject<ADSB_Contact>();
                    break;
                case ARPA_ID:
                case MARPA_ID:
                    contact = contactJson.ToObject<Radar_Contact>();
                    break;
                default:
                    contact = new Unknown_Contact();
                    break;
            }

            return contact;
        }
    }

}