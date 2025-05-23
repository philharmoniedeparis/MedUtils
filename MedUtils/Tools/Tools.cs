using MARC;

namespace MedUtils.Tools
{
    public class Tools
    {
        public static class SyracuseParams
        {
            public const string apiUrl = "https://sigb.philharmoniedeparis.fr/sdk/documentService.svc/";
            public const string accessKey = "85kxBW8Lm2vQi9";
        }
        public static async Task<string> postSyracuseRequest(object requestData, string Url)
        {
            using var httpClient = new HttpClient();

            var requestContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestData),
                System.Text.Encoding.UTF8,
                "application/json");

            try
            {
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("accessKey", SyracuseParams.accessKey);
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

        public static string ExtractXMLFromJson(string jsonResponse)
        {
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonResponse);
            var root = jsonDoc.RootElement;
            if (root.TryGetProperty("d", out var d))
            {
                // Get the XML string from the first result
                string xmlData = d.ToString();
                if (string.IsNullOrEmpty(xmlData))
                {
                    return "No XML data found in the response";
                }
                return xmlData;
            }
            else
            {
                return "No 'd' property found in the response";
            }
        }

        public static async Task<string> getSyracuseRecordFromId(string idSyracuse)
        {
            string xmlRecord = "";
            string requestResponse = "";
            string apiUrlGetDoc = SyracuseParams.apiUrl + "GetDocument";
            //test

            var requestData = new
            {
                listIds = new List<string> { idSyracuse }
            };

            requestResponse = await postSyracuseRequest(requestData, apiUrlGetDoc);
            xmlRecord = ExtractXMLFromJson(requestResponse);
            return xmlRecord;
        }
        public static async Task<string> getSyracuseRecordFromIdDocnum(string idDocnum)
        {
            string xmlRecord = "";
            string requestResponse = "";
            string apiUrlFindDocs = SyracuseParams.apiUrl + "FindDocuments";
            var requestData = new
            {
                query = $"IDOCNUM856B={idDocnum}",
                start = 0,
                limit = 10,
                sort = "ANPA",
                sortAscending = true
            };
            requestResponse = await postSyracuseRequest(requestData, apiUrlFindDocs);
            xmlRecord = ExtractXMLFromJson(requestResponse);
            return xmlRecord;
        }
    }
}
