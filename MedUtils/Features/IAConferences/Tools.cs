using MARC;
using MedUtils.Features.Syracuse;
namespace MedUtils.Features.IAConferences
{
    public class IAConferencesTools
    {

        public static async Task<MediaInfos> GetMediaInfosFromId(string idSyracuse)
        {
            MediaInfos mediaInfos = new MediaInfos
            {
                idSyracuse = idSyracuse
            };
            string xmlData = await SyracuseTools.getRecordFromId(idSyracuse);
            string AdvancedXmlData = await SyracuseTools.getAdvancedRecordFromId(idSyracuse);
            FileMARCXML fileMARCXML = new FileMARCXML(xmlData);
            mediaInfos.RecordType = SyracuseTools.GetNoticeTypeFromXML(AdvancedXmlData);
            mediaInfos.s856b = MarcDataField.getValues(fileMARCXML, "856$b")[0];
            mediaInfos.s856d = MarcDataField.getValues(fileMARCXML, "856$d")[0];
            return mediaInfos;
        }
        public class MediaInfos
        {
            public string idSyracuse { get; set; }
            public string RecordType { get; set; }
            public string idDocNum { get; set; }
            public string s856b { get; set; }
            public string s856d { get; set; }
            public string s972a { get; set; }
            public string path { get; set; }
            public string stream { get; set; }
            public int duration { get; set; }
            public int tcin { get; set; }
            public int tcout { get; set; }
            public int chapIdx { get; set; }
            public string? image { get; set; }
            public string info { get; set; }
        }

    }
}
