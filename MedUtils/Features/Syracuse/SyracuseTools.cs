using MARC;
using System;
using System.Diagnostics.Eventing.Reader;
using System.Text.Json.Serialization;
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

        public static HashSet<string> AudioTypes = new HashSet<string> { "MPA1", "MPA2", "MPA3", "DSON", "DSP1", "DSP2", "SVA1", "SVA2", "SVA3" };
        public static HashSet<string> VideoTypes = new HashSet<string> { "MPV1", "MPV2", "DAP1", "DAP2", "SVV1","SVV2" };

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
                IdDocNums = await MarcDataField.getValues(idSyracuse, "856", 'd');
                if (IdDocNums.Count > 0) 
                { 
                    return IdDocNums.First(); 
                }
                else
                {
                    return "";
                }
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
                IdDocNums = MarcDataField.getValues(xmlMarcRecords, "856", 'd');
                if (IdDocNums.Count > 0)
                {
                    return IdDocNums.First();
                }
                else
                {
                    return "";
                }
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

        public static string getAudioOrVideoTypeFromNoticeType(string  noticeType)
        {
            string AudioOrVideoType = "record is neither audio or video type";
            if (AudioTypes.Contains(noticeType)) AudioOrVideoType = "audio";
            if (VideoTypes.Contains(noticeType)) AudioOrVideoType = "video";
            return AudioOrVideoType;
        }

        // Check if Notice Has Childs
        public static bool RecordHasChilds(FileMARCXML xmlRecord)
        {
            bool hasChilds = false;
            List<string> childs =  MarcDataField.getValues(xmlRecord, "959$3");
            if ((childs.Count > 0) && (childs.First() != ""))  {
                hasChilds = true;
            }
            return hasChilds;
        }
        // Get list of Childs Ids
        public static List<string> getChildsIds(FileMARCXML xmlRecord)
        {
            List<string> childs = new List<string>();
            if (RecordHasChilds(xmlRecord))
            {
                childs = MarcDataField.getValues(xmlRecord, "959$3");
            }
            return childs;
        }
        public static async Task<List<string>> getChildsAndSubChilds(FileMARCXML xmlRecord)
        {
            List<string> AllChilds = new List<string>();
            AllChilds.Add(getIdFromXML(xmlRecord));
            if (RecordHasChilds(xmlRecord))
            {
                List<string> Childs = new List<string>();
                Childs = getChildsIds(xmlRecord);
                foreach (string child in Childs)
                {
                    if (!string.IsNullOrEmpty(child))
                    {
                        AllChilds.Add(child);
                    }
                    string SubRecord = await getRecordFromId(child);
                    if (SubRecord != null)
                    {
                        FileMARCXML xmlSubRecord = new FileMARCXML(SubRecord);
                        if (RecordHasChilds(xmlSubRecord)) 
                        {
                            List<string> subChilds = getChildsIds(xmlSubRecord);
                            foreach (string subchild in subChilds)
                            {
                                if (!string.IsNullOrEmpty(subchild))
                                {
                                    AllChilds.Add(subchild);
                                }
                            }
                        }

                    }
                }
            }
            return AllChilds;
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
            if (fields.Count == 0)
            {
                values.Add(""); // Add empty string if no fields found
                return values; 
            }
            if ((myField == "001") || (myField == "01") || (myField == "1")) //We are looking for the Id of the record
            {
                ControlField controlField = (ControlField)fields[0];
                values.Add(controlField.Data);
                return values;
            }
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
            if (values.Count == 0) values.Add("");
            return values;
        }

        //Get Persons from MARC XML record
        //Works only with 700, 701, 702, 703
        public static List<Person> getPhysicalPersons(FileMARCXML xmlMarcRecords, string PersonField)
        {
            var AllowedPersonFields = new HashSet<string> { "700", "701", "702", "703" };

            List<Person> persons = new List<Person>();
            if (AllowedPersonFields.Contains(PersonField))
            {
                Record xmlRecord = xmlMarcRecords[0];
                List<Field> fields = xmlRecord.GetFields(PersonField);
                foreach (DataField dataField in fields)
                {
                    Person person = new Person();
                    Subfield Id = dataField['3'];
                    Subfield LastName = dataField['a'];
                    Subfield FirstName = dataField['b'];
                    Subfield Role = dataField['4'];
                    person.id = Id.Data;
                    person.first_name = FirstName.Data;
                    person.role = Role.Data;
                    person.last_name = LastName.Data;
                    persons.Add(person);
                }
            }
            return persons;
        
        }
        public class Person
        {
            [JsonPropertyName("id")]
            public string id { get; set; }

            [JsonPropertyName("first_name")]
            public string first_name { get; set; }

            [JsonPropertyName("last_name")]
            public string last_name { get; set; }

            [JsonPropertyName("role")]
            public string role { get; set; }
        }
    }
}
