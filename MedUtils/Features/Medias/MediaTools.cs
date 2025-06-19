using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.AspNetCore.Components.Forms;
using NAudio.MediaFoundation;
using NAudio.Wave;

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
            public static string mediaPath = "Z:\\conferences\\";
            public static HashSet<string> Prefixes = new HashSet<string> { "CMAU", "PLAU", "PPAU", "CMVI", "PLVI", "PPVI" };
        }

        /// test 
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
                MediaPath += ".mp4";
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
        public static async Task<List<string>> MergeAudioFilesWithNaudio(string outputFile, List<string> inputFiles)
        {
            List<string> mergeFiles = new List<string>();
            if (inputFiles.Count == 0)
            {
                return mergeFiles;
            }
            List <string> patternsToExclude = ["_IA", "_CC"];
            var filterdInputFiles = inputFiles.Where(file => !patternsToExclude.Any(pattern => file.Contains(pattern))).ToList();
            string[] fileArray = filterdInputFiles.ToArray();
            Stream outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None);


            foreach (string file in fileArray)
            {
                Mp3FileReader reader = new Mp3FileReader(file);
                if ((outputStream.Position == 0) && (reader.Id3v2Tag != null))
                {
                    outputStream.Write(reader.Id3v2Tag.RawData, 0, reader.Id3v2Tag.RawData.Length);
                }
                Mp3Frame frame;
                while ((frame = reader.ReadNextFrame()) != null)
                {
                    outputStream.Write(frame.RawData, 0, frame.RawData.Length);
                }
                mergeFiles.Add(file);
            }
            outputStream.Close();
            outputStream.Dispose();
            return mergeFiles;
            //return await MediaTools.GetFileDuration(outputFile);
        }
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
            UrlStream = UrlStream.Substring(15);
            return MediaParams.StreamDomainName + UrlStream + "/playlist.m3u8";
        }
    }
}
