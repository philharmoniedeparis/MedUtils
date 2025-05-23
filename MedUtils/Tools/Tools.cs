using MARC;

namespace MedUtils.Tools
{
    public class SyracuseTools
    {
        public static class SyracuseParams
        {
            public const string apiUrl = "https://sigb.philharmoniedeparis.fr/sdk/documentService.svc/";
            public const string accessKey = "85kxBW8Lm2vQi9";
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

        public static string CleanSyracuseId(string idSyracuse)
        {
            // Remove all "0" at the beginning of the string
            if (string.IsNullOrEmpty(idSyracuse))
            {
                return idSyracuse;
            }

            int index = 0;
            while (index < idSyracuse.Length && idSyracuse[index] == '0')
            {
                index++;
            }

            return idSyracuse.Substring(index);
        }
        public static async Task<string> getRecordFromId(string idSyracuse)
        {
            string xmlRecord = "";
            string requestResponse = "";
            string apiUrlGetDoc = SyracuseParams.apiUrl + "GetDocument";
            //test

            var requestData = new
            {
                listIds = new List<string> { CleanSyracuseId(idSyracuse) }
            };

            requestResponse = await postRequest(requestData, apiUrlGetDoc);
            xmlRecord = ExtractXMLFromJson(requestResponse);
            return xmlRecord;
        }
        public static async Task<string> getRecordFromIdDocnum(string idDocnum)
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
            requestResponse = await postRequest(requestData, apiUrlFindDocs);
            xmlRecord = ExtractXMLFromJson(requestResponse);
            return xmlRecord;
  
        }

    }

    public class MarcDataField
    {
        
        public static async Task<List<string>> getValues(string idSyracuse, string myField, char mySubField)
        {
            string xmldata = await SyracuseTools.getRecordFromId(idSyracuse);
            FileMARCXML xmlMarcRecords = new FileMARCXML(xmldata);
            List<string> values = getValues(xmlMarcRecords, myField, mySubField);
            return values;
        }

        public static List<string> getValues(FileMARCXML xmlMarcRecords, string myField, char mySubField)
        {
            // Get the first record from the list. We do not need to loop through all records for the moment.
            Record xmlRecord = xmlMarcRecords[0];
            List<string> values = new List<string>();
            List<Field> fields = xmlRecord.GetFields(myField);
            foreach (DataField dataField in fields) {
                foreach (Subfield subfield in dataField.Subfields) { 
                if (subfield.Code == mySubField)
                    {
                        values.Add(subfield.Data);
                    }
                }
            }
            return values;
        }
    }
}
