using MARC;
using MedUtils.Features.Medias;
using MedUtils.Features.Syracuse;
using MedUtils.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace MedUtils.Features.IAConferences
{
    public class IAConferencesTools
    {

        ///<summary>
        ///API to get StrapiRecord from IdSyracuse and Media itself (duration)
        ///</summary>
        /// <param name="idSyracuse">The Syracuse ID of the media</param>
        /// <returns>StrapiRecord</returns>

        public static async Task<string> GetStrapiRecordAsync(string idSyracuse)
        {
            StrapiRecord strapiRecord = new StrapiRecord
            {
                aloes_id = idSyracuse
            };
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // ignore nulls
            };
            idSyracuse = SyracuseTools.CleanSyracuseId(idSyracuse);
            string xmlData = await SyracuseTools.getRecordFromId(idSyracuse);
            FileMARCXML fileMARCXML = new FileMARCXML(xmlData);
            if ((string.IsNullOrEmpty( xmlData )) || (fileMARCXML.Count==0)) {
                strapiRecord.error = "Record not found in Syracuse";
                strapiRecord.aloes_id = null;
                EmptyPropertyTools.CleanEmptiesToNull(strapiRecord);
                return JsonSerializer.Serialize(strapiRecord, options);
            }
            string AdvancedXmlData = await SyracuseTools.getAdvancedRecordFromId(idSyracuse);
            string recordType = SyracuseTools.GetNoticeTypeFromXML(AdvancedXmlData);
            strapiRecord.type = SyracuseTools.getAudioOrVideoTypeFromNoticeType(recordType);
            if (strapiRecord.type == "record is neither audio or video type")
            {
                strapiRecord.error = strapiRecord.type;
                strapiRecord.type = null;
                strapiRecord.aloes_id = null;
                EmptyPropertyTools.CleanEmptiesToNull(strapiRecord);
                return JsonSerializer.Serialize(strapiRecord, options);
            }
            strapiRecord.title = MarcDataField.getValues(fileMARCXML, "200$a")[0];
            strapiRecord.subtitle = MarcDataField.getValues(fileMARCXML, "200$e")[0];
            strapiRecord.date = MarcDataField.getValues(fileMARCXML, "946$a")[0];
            strapiRecord.description = MarcDataField.getValues(fileMARCXML, "300$a")[0];
            string duration = MarcDataField.getValues(fileMARCXML, "215$a")[0];
            if (!string.IsNullOrEmpty(duration))
            {
                strapiRecord.duration = ParseDuration(duration);
            }
            strapiRecord.creator = MarcDataField.getValues(fileMARCXML, "200$f")[0];
            strapiRecord.contributors = MarcDataField.getValues(fileMARCXML, "200$g");
            strapiRecord.keywords = MarcDataField.getValues(fileMARCXML, "911$a");
            List<MarcDataField.Person> persons = new List<MarcDataField.Person>();
            if (strapiRecord.type == "audio")
            {
                strapiRecord.personal = MarcDataField.getPhysicalPersons(fileMARCXML, "700");
                strapiRecord.speakers = MarcDataField.getValues(fileMARCXML, "200$f");
            }
            else //video
            {
                if ((recordType == "MPV1") || (recordType == "DAP1") || (recordType == "SVV1"))
                {
                    strapiRecord.personal = MarcDataField.getPhysicalPersons(fileMARCXML, "703");
                    strapiRecord.speakers = MarcDataField.getValues(fileMARCXML, "200$g");
                }
                else
                {
                    strapiRecord.personal = MarcDataField.getPhysicalPersons(fileMARCXML, "702");
                    strapiRecord.speakers = MarcDataField.getValues(fileMARCXML, "200$f");
                }
            }
            if (SyracuseTools.RecordHasChilds(fileMARCXML)) {
                strapiRecord.children = SyracuseTools.getChildsIds(fileMARCXML);
            }
            strapiRecord.urns = MarcDataField.getValues(fileMARCXML, "856$d");
            strapiRecord.uri = MarcDataField.getValues(fileMARCXML, "856$b")[0];
            strapiRecord.excerpts = MarcDataField.getValues(fileMARCXML, "948$m");


            EmptyPropertyTools.CleanEmptiesToNull(strapiRecord);
            return JsonSerializer.Serialize(strapiRecord, options);
        }


        /// <summary>
        /// Main API method to get media information from Syracuse and Media themselves
        /// </summary>
        /// <param name="idSyracuse">The Syracuse ID of the media</param>
        public static async Task<MediaInfos> GetMediaInfosFromId(string idSyracuse)
        {


            MediaInfos mediaInfos = new MediaInfos
            {
                idSyracuse = idSyracuse
            };

            idSyracuse = SyracuseTools.CleanSyracuseId(idSyracuse);
            string xmlData = await SyracuseTools.getRecordFromId(idSyracuse);
            string AdvancedXmlData = await SyracuseTools.getAdvancedRecordFromId(idSyracuse);

            FileMARCXML fileMARCXML = new FileMARCXML(xmlData);
            mediaInfos.RecordType = SyracuseTools.GetNoticeTypeFromXML(AdvancedXmlData);
            mediaInfos.s856b = MarcDataField.getValues(fileMARCXML, "856$b")[0];
            mediaInfos.s856d = MarcDataField.getValues(fileMARCXML, "856$d")[0];

            if ((mediaInfos.RecordType == "MPV1") || (mediaInfos.RecordType == "MPV2") || (mediaInfos.RecordType == "DAP1") || (mediaInfos.RecordType == "DAP2"))
            {
                if (!String.IsNullOrEmpty(mediaInfos.s856b))
                {
                    mediaInfos.idDocNum = mediaInfos.s856b;
                }
                else
                {
                    mediaInfos.idDocNum = mediaInfos.s856d[..11] + "00";
                }
                List<string> mediaFiles = MediaTools.GetAllMediaFiles(mediaInfos.idDocNum);
                foreach (string mediaFile in mediaFiles)
                {
                    if (mediaFile.Contains("_00_HQ.mp4"))
                    {
                        mediaInfos.path = mediaFile;
                    }
                }
                mediaInfos.stream = MediaTools.GetStreamUrl(mediaInfos.path);
                if ((mediaInfos.RecordType == "MPV2") || (mediaInfos.RecordType == "DAP2"))
                {
                    VideoTCInfos infos = await MediaTools.GetVideoTCInfos(idSyracuse);
                    mediaInfos.tcin = infos.tcin;
                    mediaInfos.tcout = infos.tcout;
                    mediaInfos.duration = infos.duration;
                    mediaInfos.image = MediaTools.GetImageUrl(mediaInfos.s856d);
                }
                else //MPV1 ou DAP1
                {
                    mediaInfos.tcin = 0;
                    mediaInfos.tcout = mediaInfos.duration = (double)await MediaTools.GetFileDuration(mediaInfos.path);
                    mediaInfos.image = MediaTools.GetImageUrl(mediaInfos.idDocNum);
                }
            }
            if ((mediaInfos.RecordType == "MPA1") || (mediaInfos.RecordType == "MPA2") || (mediaInfos.RecordType == "MPA3"))
            {
                if (!String.IsNullOrEmpty(mediaInfos.s856b))
                {
                    mediaInfos.idDocNum = mediaInfos.s856b;
                }
                else
                {
                    mediaInfos.idDocNum = mediaInfos.s856d[..11] + "00";
                }
                //take the 4 first char of idDocum to get DocNumPrefix 
                //DocNumPrefix = mediaInfos.idDocNum.Substring(0, 4);
                if (mediaInfos.idDocNum.Contains("XXXXXXXXX"))
                {
                    string localIdDocNum = MarcDataField.getValues(fileMARCXML, "945$a")[0];
                    mediaInfos.s945a = localIdDocNum;
                    mediaInfos.idDocNum = localIdDocNum[..11] + "00"; // Change from something like "CMAU000157001#02-10" to "CMAU000157000" 
                }
                List<string> mediaFiles = MediaTools.GetAllMediaFiles(mediaInfos.idDocNum);
                string outputFilePath = mediaFiles[0].Substring(0, 42) + mediaInfos.idDocNum + "_" + idSyracuse + "_IA.mp3";

                if (SyracuseTools.RecordHasChilds(fileMARCXML))
                {
                    if (mediaFiles.Count == 1) // Simple case : Record has childs but just one File
                    {
                        mediaInfos.path = mediaFiles[0];
                    }
                    else // Record has childs and multiple files
                    {
                        mediaInfos.path = outputFilePath;
                    }
                }
                else // Simple case : No childs
                {
                    mediaInfos.path = MediaTools.getMediaPathFromIdDocnum(mediaInfos.s856d);
                }
                mediaInfos.stream = MediaTools.GetStreamUrl(mediaInfos.path);
                mediaInfos.duration = (double)await MediaTools.GetFileDuration(mediaInfos.path);
                mediaInfos.tcin = 0;
                mediaInfos.tcout = mediaInfos.duration;
                // mediaInfos.image = MediaTools.GetImageUrl(mediaInfos.idDocNum);
            }
            return mediaInfos;
        }

        /// <summary>
        /// API method to merge files from IdSyracuse. Warning ! this will create new files on disk
        /// </summary>

        public static async Task<List<string>> MergeFilesFromId(string idSyracuse)


        {
            MediaInfos mediaInfos = new MediaInfos
            {
                idSyracuse = idSyracuse
            };
            List<string> mergeFiles = new List<string>();
            string xmlData = await SyracuseTools.getRecordFromId(idSyracuse);
            string AdvancedXmlData = await SyracuseTools.getAdvancedRecordFromId(idSyracuse);
            string DocNumPrefix = "";

            FileMARCXML fileMARCXML = new FileMARCXML(xmlData);
            mediaInfos.RecordType = SyracuseTools.GetNoticeTypeFromXML(AdvancedXmlData);
            mediaInfos.s856b = MarcDataField.getValues(fileMARCXML, "856$b")[0];
            mediaInfos.s856d = MarcDataField.getValues(fileMARCXML, "856$d")[0];

            if ((mediaInfos.RecordType == "MPA1") || (mediaInfos.RecordType == "MPA2"))
            {
                if (!String.IsNullOrEmpty(mediaInfos.s856b))
                {
                    mediaInfos.idDocNum = mediaInfos.s856b;
                }
                else
                {
                    mediaInfos.idDocNum = mediaInfos.s856d[..11] + "00";
                }
                //take the 4 first char of idDocum to get DocNumPrefix 
                //DocNumPrefix = mediaInfos.idDocNum.Substring(0, 4);
                if (mediaInfos.idDocNum.Contains("XXXXXXXXX"))
                {
                    string localIdDocNum = MarcDataField.getValues(fileMARCXML, "945$a")[0];
                    mediaInfos.s945a = localIdDocNum;
                    mediaInfos.idDocNum = localIdDocNum[..11] + "00"; //Change from something like "CMAU000157001#02-10" to "CMAU000157000" 
                }
                List<string> mediaFiles = MediaTools.GetAllMediaFiles(mediaInfos.idDocNum);
                string outputFilePath = mediaFiles[0].Substring(0, 42) + mediaInfos.idDocNum + "_" + idSyracuse + "_IA.mp3";

                if (SyracuseTools.RecordHasChilds(fileMARCXML))
                {
                    if (mediaFiles.Count == 1) //Record has childs but just one File
                    {
                        //Nothing to be merged
                    }
                    else // Record has childs and multiple files
                    {
                        if (mediaInfos.RecordType == "MPA1") // Merge all files
                        {
                            mergeFiles = await MediaTools.MergeAudioFilesWithFFMpegCore(mediaFiles, outputFilePath);
                        }
                        else // MPA2 with childs
                        {
                            List<string> childs = SyracuseTools.getChildsIds(fileMARCXML);
                            List<string> ListOfFilesToMerge = new List<string>();
                            foreach (string child in childs)
                            {
                                string idDocnum = await SyracuseTools.getIdDocnumFromId(child);
                                string fileToMerge = MediaTools.getMediaPathFromIdDocnum(idDocnum);
                                ListOfFilesToMerge.Add(fileToMerge);
                            }
                            mergeFiles = await MediaTools.MergeAudioFilesWithFFMpegCore(ListOfFilesToMerge, outputFilePath);
                        }
                    }
                }
                else //Simple case : No childs
                {
                    if (mediaFiles.Count != 1)
                    {
                        mediaInfos.path = "something went wrong, no childs but multiple or no media files found";
                    }
                    else
                    {
                        mediaInfos.path = mediaFiles[0];
                        mediaInfos.stream = MediaTools.GetStreamUrl(mediaInfos.path);
                        mediaInfos.duration = (double)await MediaTools.GetFileDuration(mediaInfos.path);
                        mediaInfos.tcin = 0;
                        mediaInfos.tcout = mediaInfos.duration;
                        mediaInfos.image = MediaTools.GetImageUrl(mediaInfos.idDocNum);
                    }
                }
                return mergeFiles;
            }

            return mergeFiles;
        }

        /// <summary>
        /// API method to merge All files from RootIdDocNum. Warning ! this will create (potentially a lot of) new files on disk
        /// </summary>

        public static async Task<List<string>> MergeFilesFromRootIdDocnum(string IdDocnum)
        {
            List<string> result = new List<string>();
            List<string> ids = new List<string>();
            if (IdDocnum != null)
            {
                string record = await SyracuseTools.getRecordFromIdDocnum(IdDocnum);
                if (record != null)
                {
                    FileMARCXML fileMARCXML = new FileMARCXML(record);
                    ids = await SyracuseTools.getChildsAndSubChilds(fileMARCXML);
                    foreach (string id in ids)
                    {
                        result.Add(id);
                        List<string> mergeFiles = await MergeFilesFromId(id);
                        foreach (string mergeFile in mergeFiles)
                        {
                            result.Add(mergeFile);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// API method to sync the conferences database with Syracuse
        /// </summary>
        /// 
        public static async Task<string> SyncConferencesDatabaseFromIdSyracuse(string idSyracuse)
        {

            if (idSyracuse != null)
            {
                // Call sync API, passing the idSyracuse and return true if successful
                while (idSyracuse.Length < 7)
                {
                    idSyracuse = "0" + idSyracuse; // Ensure idSyracuse is at least 10 characters long
                }
                string syncUrl = $"http://poc-conferences:X%25k%5E%2Fy.gV%3EUre%3F%25U!m2v%3B%3AtnMF%3A%40Z.@poc-conferences.philharmoniedeparis.fr/api/recordings/sync?update&aloes_id={idSyracuse}" + "&streamsUrl=http://192.168.23.58/IAConferences/query/id/{aloes_id}"; 
                using HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(syncUrl);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    // Optionally, you can log or process the responseBody if needed
                    return syncUrl + "\n"; // Sync was successful
                }
                else
                {
                    // Handle the case where the sync failed
                    return  $"Sync failed for Id {idSyracuse} with status code: {response.StatusCode}" + "\n" + $"API request {syncUrl}";
                }
            }
            return "";
        }

        public static async Task<List<string>> SyncConferencesFromIdDocnum(string RootIdDocnum)
        {
            FileMARCXML xmlRecord = new FileMARCXML(await SyracuseTools.getRecordFromIdDocnum(RootIdDocnum));
            List<string> ids = await SyracuseTools.getChildsAndSubChilds(xmlRecord);
            List<string>  Results = new List<string>();
            foreach (string id in ids)
            {
                string result = await SyncConferencesDatabaseFromIdSyracuse(id);
                if (!String.IsNullOrEmpty(result))
                {
                    Results.Add(result);
                }
            }
            return Results;
        }

        /// <summary>
        /// Parses a duration string and converts it into the total number of seconds.
        /// </summary>
        /// <remarks>This method normalizes the input string by trimming whitespace and replacing common
        /// abbreviations (e.g., "min", "mn", "sec") with their respective shorthand ("m", "s"). If the last segment of
        /// the input is a number without a unit, it is assumed to represent seconds.</remarks>
        /// <param name="value">A string representing a duration, which may include hours, minutes, and seconds. Supported formats include
        /// combinations of "h" (hours), "m" (minutes), and "s" (seconds), with optional spaces or abbreviations (e.g.,
        /// "1h 30m", "90m", "45s").</param>
        /// <returns>The total duration in seconds. Returns 0 if the input is null, empty, or contains only whitespace.</returns>
        /// <exception cref="ArgumentException">Thrown if the input string contains an invalid duration format.</exception>
        public static int ParseDuration(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            var normalized = value.Trim()
                .Replace("  ", " ")
                .Replace(" h", "h")
                .Replace(" min", "m")
                .Replace(" mn", "m")
                .Replace(" mi", "m")
                .Replace(" sec", "s");

            var items = normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            if (items.Count == 0)
                return 0;

            // If the last item is just a number, add "s"
            if (int.TryParse(items[^1], out int lastNumber))
            {
                items[^1] = $"{lastNumber}s";
            }

            int result = 0;

            foreach (var item in items)
            {
                if (item.EndsWith("h"))
                {
                    result += int.Parse(item[..^1]) * 60 * 60;
                }
                else if (item.EndsWith("m"))
                {
                    result += int.Parse(item[..^1]) * 60;
                }
                else if (item.EndsWith("s"))
                {
                    result += int.Parse(item[..^1]);
                }
                else
                {
                    throw new ArgumentException($"Wrong duration format: {value}");
                }
            }

            return result;
        }


        public class StrapiRecord   
        {
            [JsonPropertyName("aloes_id")]
            public string aloes_id { get; set; }

            [JsonPropertyName("type")]
            public string type { get; set; }

            [JsonPropertyName("title")]
            public string title { get; set; }

            [JsonPropertyName("subtitle")]
            public string subtitle { get; set; }

            // Stays string because it's "YYYYMMDD" in your JSON.
            [JsonPropertyName("date")]
            public string date { get; set; }

            [JsonPropertyName("description")]
            public string description { get; set; }

            [JsonPropertyName("duration")]
            public int? duration { get; set; }

            [JsonPropertyName("creator")]
            public string creator { get; set; }

            [JsonPropertyName("contributors")]
            public List<string> contributors { get; set; } = new();

            [JsonPropertyName("keywords")]
            public List<string> keywords { get; set; } = new();

            [JsonPropertyName("personal")]
            public List<MarcDataField.Person> personal { get; set; } = new();

            [JsonPropertyName("speakers")]
            public List<string> speakers { get; set; } = new();

            [JsonPropertyName("children")]
            public List<string> children { get; set; } = new();

            [JsonPropertyName("uri")]
            public string uri { get; set; }

            [JsonPropertyName("urns")]
            public List<string> urns { get; set; } = new();

            [JsonPropertyName("excerpts")]
            public List<string> excerpts { get; set; } = new();

            public string error { get; set; }

            // Optional helper to parse the date string (YYYYMMDD) into DateOnly
            [JsonIgnore]
            public DateOnly? ParsedDate =>
                DateOnly.TryParseExact(date, "yyyyMMdd", out var d) ? d : (DateOnly?)null;
        }

        public class MediaInfos
        {
            public string idSyracuse { get; set; }
            public string RecordType { get; set; }
            public string idDocNum { get; set; }
            public string s856b { get; set; }
            public string s856d { get; set; }
            public string s945a { get; set; }
            public string path { get; set; }
            public string stream { get; set; }
            public double duration { get; set; }
            public double tcin { get; set; }
            public double tcout { get; set; }
            public int chapIdx { get; set; }
            public string? image { get; set; }
            public string info { get; set; }
        }
        public class VideoTCInfos
        {
            public double duration { get; set; }
            public double tcin { get; set; }
            public double tcout { get; set; }
        }

    }
}
