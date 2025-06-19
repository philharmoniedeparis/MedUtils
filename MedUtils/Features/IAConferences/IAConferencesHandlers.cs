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
            string json = JsonSerializer.Serialize(infos, new JsonSerializerOptions { WriteIndented = true });
            return Results.Content(json);
        }
    }
}
