using FFMpegCore;
using MedUtils.Features.IAConferences;
using System.Globalization;
using static MedUtils.Features.IAConferences.IAConferencesTools;

namespace MedUtils.Features.Medias
{
    public class MediaTools
    {

        public static class MediaParams
        {
            public static string ffmpegBinPath = "bin\\Release\\net8.0";
            public static string ffmpegTmpPath = "Features\\Medias\\tmp";
            public static string UrlCDNimages = "https://cdn.philharmoniedeparis.fr/http/images/poster/";
            public static string StreamDomainName = "https://stream.philharmoniedeparis.fr/conferences/_definst_/mp3:";
            public static string mediaPath = "\\\\10.0.2.1\\conferences\\";
            public static string APIUrlForMedia = "https://otoplayer.philharmoniedeparis.fr/fr/conference/";

            public static HashSet<string> Prefixes = new HashSet<string> { "CMAU", "PLAU", "PPAU", "CMVI", "PLVI", "PPVI" };
        }


        ///summary>
        ///Generic method to get data from an API using HttpClient.
        ///</summary>
        ///<param name="input"></param>
        ///<param name="Url"></param>
        ///<returns></returns>
        ///
        public static async Task<string> getFromAPI(string input, string Url)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            if (string.IsNullOrEmpty(Url)) return string.Empty;
            using HttpClient client = new HttpClient();
            try
            {
                var response = await client.GetAsync(Url + input);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching data: {ex.Message}");
                return string.Empty;
            }
        }
        public static double ParseDoubleFromJson(System.Text.Json.JsonDocument JsonInput, string element) 
        { 
            double result = 0;
            var root = JsonInput.RootElement;
            if (root.TryGetProperty(element, out var el))
            {
                // Get the XML string from the first result
                string elem = el.ToString();
                if (string.IsNullOrEmpty(elem))
                {
                    result = 0;
                }
                else
                {
                    elem = elem.Replace(",", ".");
                    if (!double.TryParse(elem, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedElem))
                    {
                        parsedElem = 0;
                    }
                    else
                    {
                        result = parsedElem;
                    }
                }

            }
            else
            {
                result = 0;
            }

            return result;
        }


        ///summary>
        ///Get Video Time codes from IDDocnum using Otoplayer API.
        ///</summary>
        ///<param name="idDocNum"></param>
        ///<returns></returns>
        ///
        public static async Task<IAConferencesTools.VideoTCInfos> GetVideoTCInfos(string idSyracuse)
        {
            IAConferencesTools.VideoTCInfos videoTCInfos = new IAConferencesTools.VideoTCInfos();
            if (string.IsNullOrEmpty(idSyracuse)) return videoTCInfos;
            var jsonResponse = await getFromAPI(idSyracuse, MediaParams.APIUrlForMedia);
            if (string.IsNullOrEmpty(jsonResponse)) return videoTCInfos;
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonResponse);
            var root = jsonDoc.RootElement;
            videoTCInfos.duration = ParseDoubleFromJson(jsonDoc, "duration");
            videoTCInfos.tcin = ParseDoubleFromJson(jsonDoc, "tcin");
            videoTCInfos.tcout = ParseDoubleFromJson(jsonDoc, "tcout");
            return videoTCInfos;
        }

        /// <summary>
        /// Get Media Path from IdDocnum
        /// </summary>
        /// <param name="IdDocnum"></param>
        /// <returns></returns>

        public static string getMediaPathFromIdDocnum(string idDocNum)
        {
            string MediaPath = "";
            if (string.IsNullOrEmpty(idDocNum)) return MediaPath;
            string DocNumPrefix = idDocNum[..4];
            if (!(MediaParams.Prefixes.Contains(DocNumPrefix)))
            {
                DocNumPrefix = "XXVI";
            }
            string RootIdDocNum = idDocNum[..11] + "00";
            string SubFolderDocNum = idDocNum[..13];
            MediaPath = Path.Combine(MediaParams.mediaPath, DocNumPrefix, RootIdDocNum,SubFolderDocNum,idDocNum);
            if (idDocNum.Contains("AU"))
            {
                MediaPath += ".mp3"; 
            }
            else 
            {
                MediaPath += "_HD.mp4";
            }
            return MediaPath;
        }

        /// <summary>
        /// Get a list of all media files (mp3, mp4) for a specified idDocnum in the form XXXX00000100, including _CC, _IA, _E files.
        /// </summary>
        /// <param name="rootFolder"></param>
        /// <returns></returns>
        public static List<string> GetAllMediaFiles(string RootIdDocNum)
        {

            string DocNumPrefix = RootIdDocNum[..4];
            var KnownPrefixes = new HashSet<string> { "CMAU", "PLAU", "PPAU", "CMVI", "PLVI", "PPVI" };
            if (!(MediaParams.Prefixes.Contains(DocNumPrefix)))
            {
                DocNumPrefix = "XXVI";
            }
            string rootFolder = Path.Combine(MediaParams.mediaPath, DocNumPrefix, RootIdDocNum);
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp3", ".mp4"
            };
            var files = new List<string>();
            try
            {
                // Add files in the current directory that match allowed extensions
                files.AddRange(
                    Directory.GetFiles(rootFolder)
                        .Where(f => allowedExtensions.Contains(Path.GetExtension(f)))
                );

                // Recursively add files from subdirectories
                foreach (var dir in Directory.GetDirectories(rootFolder))
                {
                    files.AddRange(GetAllMediaFiles(dir));
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Optionally handle folders you can't access
            }
            return files;
        }
        /// <summary>
        /// Get the file duration in seconds from the specified file path.  
        /// </summary> 
        /// <param name="filePath"></param>
        /// returns></returns>
        public static async Task<double> GetFileDuration(string filePath)
        {
           GlobalFFOptions.Configure(options => options.BinaryFolder = MediaParams.ffmpegBinPath);
           var FFMpegMediaInfo = await FFProbe.AnalyseAsync(filePath);
           return Math.Floor((double)FFMpegMediaInfo.Duration.TotalMicroseconds / 1000000);
        }
        /// <summary>
        /// Merge multiple audio files into a single file using FFMPEGCore.
        /// </summary> 
        /// <param name="inputFiles"></param>
        /// <param name="outputFile" ></param>
        /// <returns>outputFile duration</returns>

        public static async Task<List<string>> MergeAudioFilesWithFFMpegCore(List<string> inputFiles, string outputFile)
        {
            if (inputFiles == null || inputFiles.Count == 0)
                return [];
            GlobalFFOptions.Configure(options => options.BinaryFolder = MediaParams.ffmpegBinPath);
            List<string> patternsToExclude = ["_IA", "_CC", "_e", "_E"];
            var filterdInputFiles = inputFiles.Where(file => !patternsToExclude.Any(pattern => file.Contains(pattern))).ToList();
            var ffmpeg = FFMpegArguments
                .FromConcatInput(filterdInputFiles)
                .OutputToFile(outputFile, overwrite: true)
                .NotifyOnProgress(progress => { /* Optionally handle progress */ });
                    
            await ffmpeg.ProcessAsynchronously();

            return filterdInputFiles;


        }
        /// <summary>
        /// Merge multiple audio files into a single file using Naudio.
        /// </summary> 
        /// <param name="outputFile" ></param>
        /// <param name="inputFiles"></param>
        /// <returns>outputFile duration</returns>
        public static string GetImageUrl(string idDocNum) 
        {
            if (idDocNum.Substring(2, 2) != "VI")
            {
                return "";
            }
            else
            {
                return MediaParams.UrlCDNimages + idDocNum + ".png";
            }
        }
        public static string GetStreamUrl(string path)
        {
            string UrlStream = path.Replace("\\", "/");
            UrlStream = UrlStream.Substring(23);
            return MediaParams.StreamDomainName + UrlStream + "/playlist.m3u8";
        }
    }
}
