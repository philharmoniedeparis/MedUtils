using MARC;
using MedUtils.Features.Syracuse;
using System.Text.Json;

namespace MedUtils.Features.IAConferences
{
    public class IAConferencesHandlers
    {
        public static async Task<IResult> GetInfosFromId(string id)
        {
            IAConferencesTools.MediaInfos infos = await IAConferencesTools.GetMediaInfosFromId(id);
            return Results.Json(infos);
        }
        public static async Task<IResult> MergeMediaFilesOnDisk(string idSyracuse)
        {
            List<string> mergeFiles = await IAConferencesTools.MergeFilesFromId(idSyracuse);
            string json = JsonSerializer.Serialize(mergeFiles, new JsonSerializerOptions { WriteIndented = true });
            return Results.Content(json);
        }

        public static async Task<IResult> MergeMediaFilesFromIdDocNumOnDisk(string RootIdDocnum)
        {
            List<string> mergeFiles = await IAConferencesTools.MergeFilesFromRootIdDocnum(RootIdDocnum);
            string json = JsonSerializer.Serialize(mergeFiles, new JsonSerializerOptions { WriteIndented = true });
            return Results.Content(json);
        }
        public static async Task<IResult> SyncConferencesDatabaseFromId(string id)
        {
            string result = await IAConferencesTools.SyncConferencesDatabaseFromIdSyracuse(id);
            //return Results.Json(new { Success = result });
            return Results.Json(result);
        }
        public static async Task<IResult> SyncConferencesDatabaseFromIdDocN(string rootIdDocnum)
        {
            List<string> results = await IAConferencesTools.SyncConferencesFromIdDocnum(rootIdDocnum);
            //return Results.Json(new { Success = result });
            return Results.Json(results);
        }
        public static async Task<IResult> GetStrapiRec(string id) 
        {
            string results = await IAConferencesTools.GetStrapiRecordAsync(id);
            //return Results.Json(new { Success = result });
            return Results.Content(results);
        }
    }
}
