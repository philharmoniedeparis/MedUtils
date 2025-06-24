using MARC;
using MedUtils.Features.Medias;
using MedUtils.Features.Syracuse;
using System.Net.Security;
using System.Runtime.CompilerServices;
using static MedUtils.Features.IAConferences.IAConferencesTools;
namespace MedUtils.Features.IAConferences
{
    public class IAConferencesTools
    {
        /// <summary>
        /// Main API method to get media information from Syracuse and Media themselves
        /// </summary>
        public static async Task<MediaInfos> GetMediaInfosFromId(string idSyracuse)
        {
            MediaInfos mediaInfos = new MediaInfos
            {
                idSyracuse = idSyracuse
            };
            string xmlData = await SyracuseTools.getRecordFromId(idSyracuse);
            string AdvancedXmlData = await SyracuseTools.getAdvancedRecordFromId(idSyracuse);
            string DocNumPrefix = "";

            FileMARCXML fileMARCXML = new FileMARCXML(xmlData);
            mediaInfos.RecordType = SyracuseTools.GetNoticeTypeFromXML(AdvancedXmlData);
            mediaInfos.s856b = MarcDataField.getValues(fileMARCXML, "856$b")[0];
            mediaInfos.s856d = MarcDataField.getValues(fileMARCXML, "856$d")[0];

            if ((mediaInfos.RecordType=="MPA1")||(mediaInfos.RecordType == "MPV1"))
            {
                mediaInfos.idDocNum = mediaInfos.s856b;
                //take the 4 first char of idDocum to get DocNumPrefix 
                DocNumPrefix = mediaInfos.idDocNum.Substring(0, 4);
                List<string> mediaFiles = MediaTools.GetAllMediaFiles(mediaInfos.s856b);
                if (SyracuseTools.RecordHasChilds(fileMARCXML))
                {
                    if (mediaFiles.Count == 1) //Simple case : Record has childs but just one File
                    {
                        mediaInfos.path = mediaFiles[0];
                        mediaInfos.stream = MediaTools.GetStreamUrl(mediaInfos.path);
                        mediaInfos.duration = (double)await MediaTools.GetFileDuration(mediaInfos.path);
                        mediaInfos.tcin = 0;
                        mediaInfos.tcout = mediaInfos.duration;
                        mediaInfos.image = MediaTools.GetImageUrl(mediaInfos.idDocNum);
                    }
                    else // Record has childs and multiple files
                    {
                        
                    }
                }
                else //Simple case : No childs
                {
                    if (mediaFiles.Count != 1) 
                    {
                        mediaInfos.path = "something went wrong, no childs but multiple or no media files found, mediaInfos.count = " + mediaFiles.Count.ToString();
                        mediaInfos.image = "Current MediaPath : " + MediaTools.testMediaPath(mediaInfos.s856b);
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
                string outputFilePath = mediaFiles[0].Substring(0,42) + mediaInfos.idDocNum + "_" + idSyracuse + "_IA.mp3";

                if (SyracuseTools.RecordHasChilds(fileMARCXML))
                {
                    if (mediaFiles.Count == 1) //Record has childs but just one File
                    {    
                        //Nothing merge
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
        public class TechMediaInfos
        {
            public string path { get; set; }
            public string stream { get; set; }
            public int duration { get; set; }
            public int tcin { get; set; }
            public int tcout { get; set; }
            public string? image { get; set; }
        }

    }
}
