using MedUtils.Features.Syracuse;

namespace MedUtils.Features.IAConferences
{
    public static class IAConferencesEndPoints
    {
        public static void MapIAConferencesEndPoints(this WebApplication app)
        {
            var BasicGroup = app.MapGroup("/IAConferences/query");
            var MergeFilesGroup = app.MapGroup("/IAConferences/MergeFiles");
            BasicGroup.MapGet("/id/{id}", IAConferencesHandlers.GetInfosFromId);

            MergeFilesGroup.MapGet("/idSyracuse/{idSyracuse}", IAConferencesHandlers.MergeMediaFilesOnDisk);
            MergeFilesGroup.MapGet("/idDocnum/{RootIdDocnum}", IAConferencesHandlers.MergeMediaFilesFromIdDocNumOnDisk);
        }
    }
}
