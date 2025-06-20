﻿using MARC;
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
    }
}
