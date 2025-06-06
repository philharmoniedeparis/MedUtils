using MARC;
using System.Xml.Linq;

namespace MedUtils.Features.Syracuse
{

    public class SyracuseTools
    {
        //Parameters for Syracuse API
        //test

        private static IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        public static class SyracuseParams
        {

            static IConfiguration conf = configuration.GetSection("Syracuse");
            public static string? apiUrl = conf["apiUrl"];
            public static string? accessKey = conf["accessKey"];
        }
        //Request to Syracuse API
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
        // Extract XML from JSON response from Syracuse API
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

        // Clean Syracuse ID by removing leading zeros
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

        // Get a record from Syracuse by Id
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
        // Get a record from Syracuse by IdDocnum
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

        //Get IdDocum from Syracuse by Id
        public static async Task<string> getIdDocnumFromId(string idSyracuse)
        {
            List<string> IdDocNums = await MarcDataField.getValues(idSyracuse, "856", 'b');
            if (IdDocNums.Count > 0)
            {
                return IdDocNums.First();
            }
            else
            {
                return "";
            }
        }
        //Get IdDocnum from xmlRecord 
        public static string getIdDocnumFromXML(FileMARCXML xmlMarcRecords)
        {
            List<string> IdDocNums = MarcDataField.getValues(xmlMarcRecords, "856", 'b');
            if (IdDocNums.Count > 0)
            {
                return IdDocNums.First();
            }
            else
            {
                return "";
            }
        }

        //Get Id from IdDocnum
        public static async Task<string> getIdFromIdDocnum(string idDocnum)
        {
            string xmlRecord = await getRecordFromIdDocnum(idDocnum);
            FileMARCXML xmlMarcRecords = new FileMARCXML(xmlRecord);
            List<string> Ids = MarcDataField.getValues(xmlMarcRecords, "001", 'a');
            if (Ids.Count > 0)
            {
                return Ids.First();
            }
            else
            {
                return "";
            }
        }

        // Get Id from xmlRecord
        public static string getIdFromXML(FileMARCXML xmlMarcRecords)
        {
            List<string> Ids = MarcDataField.getValues(xmlMarcRecords, "001", 'a');
            if (Ids.Count > 0)
            {
                return Ids.First();
            }
            else
            {
                return "";
            }
        }

        //AdvancedFindDocuments

        // Get from Syracuse by Id Using AdvancedFindDocuments
        public static async Task<string> getAdvancedRecordFromId(string idSyracuse)
        {
            string xmlRecord = "";
            string requestResponse = "";
            string apiUrlFindDocs = SyracuseParams.apiUrl + "AdvancedFindDocuments";
            string id = CleanSyracuseId(idSyracuse);
            var requestData = new
            {
                query = $"IDNO={id}",
                start = 0,
                limit = 10,
                sort = "ANPA",
                sortAscending = true
            };
            requestResponse = await postRequest(requestData, apiUrlFindDocs);
            xmlRecord = AvancedExtractXMLFromJson(requestResponse);
            return xmlRecord;

        }

        // Extract XML from JSON response (from Syracuse API AdvancedFindDocuments)
        public static string AvancedExtractXMLFromJson(string jsonResponse)
        {
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonResponse);
            var root = jsonDoc.RootElement;
            if (root.TryGetProperty("d", out var d) && d.TryGetProperty("results", out var results))
            {
                // Get the XML string from the first result
                string xmlData = results[0].ToString();
                if (string.IsNullOrEmpty(xmlData))
                {
                    return "No XML data found in the response";
                }
                return xmlData;
            }
            else
            {
                return "No results properties found in the response from Syracuse";
            }
        }
        // Get noticeType from an XML record (from Syracuse API AdvancedFindDocuments)
        public static string GetNoticeTypeFromXML(string xmlRecord)

        {
            if (string.IsNullOrEmpty(xmlRecord))
            {
                return "No XML record provided";
            }
            try
            {
                XDocument doc = XDocument.Parse(xmlRecord);
                string? noticeType = doc.Root?.Attribute("noticeType")?.Value;
                if (noticeType != null)
                {
                    return noticeType;
                }
                else
                {
                    return "No noticeType found in the XML record";
                }
            }
            catch (Exception ex)
            {
                return $"Error parsing XML: {ex.Message}";
            }
        }
        // Get noticeType by IdSyracuse (from Syracuse API AdvancedFindDocuments)
        public static async Task<string> GetNoticeTypeFromId(string idSyracuse)
        {
            string xmlRecord = await getAdvancedRecordFromId(idSyracuse);
            return GetNoticeTypeFromXML(xmlRecord);
        }

        // Check if Notice Has Childs
        public static bool RecordHasChilds(FileMARCXML xmlRecord)
        {
            bool hasChilds = false;
            List<string> childs =  MarcDataField.getValues(xmlRecord, "959$3");
            if (childs.Count > 0) {
                hasChilds = true;
            }
            return hasChilds;
        }
        // Get list of Childs Ids
        public static List<string> getRecordChilds(FileMARCXML xmlRecord)
        {
            List<string> childs = MarcDataField.getValues(xmlRecord, "959$3");
            return childs;
        }
    }


    public class MarcDataField
    {
        /// Get values from a MARC XML record based on idSyracuse, field and subfield.
        public static async Task<List<string>> getValues(string idSyracuse, string myField, char mySubField)
        {
            string xmldata = await SyracuseTools.getRecordFromId(idSyracuse);
            FileMARCXML xmlMarcRecords = new FileMARCXML(xmldata);
            List<string> values = getValues(xmlMarcRecords, myField, mySubField);
            return values;
        }
        /// Get values from a MARC XML record based on the MARC XML itself, field and subfield
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
        /// Get values from a MARC XML record based on idSyracuse and field with '$' (e.g. "100$a")
        public static async Task<List<string>> getValues(string idSyracuse, string myFieldAndSubfield)
        {
            string xmldata = await SyracuseTools.getRecordFromId(idSyracuse);

            if (string.IsNullOrEmpty(myFieldAndSubfield) || !myFieldAndSubfield.Contains('$'))
                throw new ArgumentException("myFieldAndSubfield must be in the format 'field$subfield'");

            var parts = myFieldAndSubfield.Split('$');
            if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || parts[1].Length != 1)
                throw new ArgumentException("myFieldAndSubfield must be in the format 'field$subfield'");

            string myField = parts[0];
            char mySubField = parts[1][0];

            FileMARCXML xmlMarcRecords = new FileMARCXML(xmldata);
            List<string> values = getValues(xmlMarcRecords, myField, mySubField);
            return values;
        }
        /// Get values from a MARC XML record based on the MARC XML itself and field with '$' (e.g. "100$a")
        public static List<string> getValues(FileMARCXML xmlMarcRecords, string myFieldAndSubfield)
        {


            if (string.IsNullOrEmpty(myFieldAndSubfield) || !myFieldAndSubfield.Contains('$'))
                throw new ArgumentException("myFieldAndSubfield must be in the format 'field$subfield'");

            var parts = myFieldAndSubfield.Split('$');
            if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || parts[1].Length != 1)
                throw new ArgumentException("myFieldAndSubfield must be in the format 'field$subfield'");

            string myField = parts[0];
            char mySubField = parts[1][0];

            Record xmlRecord = xmlMarcRecords[0];
            List<string> values = new List<string>();
            List<Field> fields = xmlRecord.GetFields(myField);
            foreach (DataField dataField in fields)
            {
                foreach (Subfield subfield in dataField.Subfields)
                {
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
