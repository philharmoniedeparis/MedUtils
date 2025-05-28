using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static MedUtils.Features.Deneb.DenebTools;
using static MedUtils.Features.Syracuse.SyracuseTools;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;

namespace MedUtils.Features.Deneb
{
    public class DenebTools
    {
       private static IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

        private static IConfiguration DenebUser = configuration.GetSection("Deneb");

     
        public static string GetEventFromId(string idEvent)
        {
            string jsonEvent = "";

            return jsonEvent;

            //string xmlRecord = "";
            //string requestResponse = "";
            //string apiUrlGetDoc = SyracuseParams.apiUrl + "GetDocument";
            ////test

            //var requestData = new
            //{
            //    listIds = new List<string> { CleanSyracuseId(idEvent) }
            //};

            //requestResponse = await postRequest(requestData, apiUrlGetDoc);
            //xmlRecord = ExtractXMLFromJson(requestResponse);
            //return xmlRecord;

        }

        public static async Task<string> GetToken()
        {
            string token = string.Empty;
            string requestResponse = "";

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string apiDenebUrl = "https://deneb.philharmoniedeparis.fr/api-login";
            Uri denebUri = new Uri(apiDenebUrl);

            string DenebU = DenebUser.GetValue<string>("User");
            string DenebPWD = DenebUser.GetValue<string>("PWD");

            string postContent = "{\"username\":\"" + DenebU  + "\", \"password\":\"" + DenebPWD + "\"}";

            ObjToken objToken = new ObjToken();


            //requestResponse = await postRequest(postContent, apiDenebUrl);

            using(var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), apiDenebUrl))
                {
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    request.Content = new StringContent(postContent);
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                    using(HttpResponseMessage response = httpClient.SendAsync(request).Result)
                    {
                        using(HttpContent content = response.Content)
                        {
                            var json = content.ReadAsStringAsync().Result;
                            objToken = JsonSerializer.Deserialize<ObjToken>(json);
                            
                        }
                    }
                }
            }

            token = objToken.token;

            return token;
        }

        public static async Task<string> postRequest(object requestData, string Url)
        {
            using var httpClient = new HttpClient();

            var requestContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestData),
                System.Text.Encoding.UTF8,
                "application/json");

            try
            {
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                var response = await httpClient.PostAsync(Url, requestContent);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return jsonResponse;
            }
            catch (HttpRequestException e)
            {
                // Handle exception
                return e.Message;
            }
        }

        private class ObjToken
        {
            public string? token { get; set; }
        }

    }
}
