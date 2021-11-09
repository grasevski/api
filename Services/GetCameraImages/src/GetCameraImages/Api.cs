using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;

namespace GetCameraImages
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

    public static class AuthApi
    {
        public static readonly EndpointAuthDetails details = new EndpointAuthDetails();
        private static string AuthScheme => "Basic";
        private static readonly HttpClient httpClient = setupClient();

        private static HttpClient setupClient() {
            
            var client = new HttpClient();
            var encodedAuth = Encoding.ASCII.GetBytes($"{details.Username}:{details.Password}");
            var header = new AuthenticationHeaderValue(AuthScheme, Convert.ToBase64String(encodedAuth));

            client.DefaultRequestHeaders.Authorization = header;

            return client;
        }


        public static async Task<System.Net.Http.HttpResponseMessage> GetAsync(string querystring)
        {
            return await httpClient.GetAsync(details.Endpoint + querystring);
        }

    }
}
