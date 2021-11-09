using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;

namespace GetCameraNames
{

    public class EndpointAuthDetails
    {
        public string Endpoint { set; get; } = "";
        public string Username { set; get; } = "";
        public string Password { set; get; } = "";

        public EndpointAuthDetails()
        {
            Endpoint = Environment.GetEnvironmentVariable("PUBLIC_ENDPOINT");
            Username = Environment.GetEnvironmentVariable("PUBLIC_AUTH_USER");
            Password = Environment.GetEnvironmentVariable("PUBLIC_AUTH_PASSWORD");
        }

    }


    [Obsolete]
    public static class Api
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<string> GetXml(string endpoint)
        {
            return await httpClient.GetStringAsync(endpoint);
        }
    }

    public static class AuthApi
    {
        private static readonly EndpointAuthDetails details = new EndpointAuthDetails();
        private static string AuthScheme => "Basic";
        private static readonly HttpClient httpClient = setupClient();

        private static HttpClient setupClient() {
            
            var client = new HttpClient();
            var encodedAuth = Encoding.ASCII.GetBytes($"{details.Username}:{details.Password}");
            var header = new AuthenticationHeaderValue(AuthScheme, Convert.ToBase64String(encodedAuth));

            client.DefaultRequestHeaders.Authorization = header;

            return client;
        }


        public static async Task<string> GetXml(string querystring)
        {
            return await httpClient.GetStringAsync(details.Endpoint + querystring);
        }

    }
}
