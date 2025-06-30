using MedUtils.Features.Syracuse;

namespace MedUtils.Features.IAConferences
{
    public static class IAConferencesEndPoints
    {
        public static void MapIAConferencesEndPoints(this WebApplication app)
        {
            var BasicGroup = app.MapGroup("/IAConferences/query");
            var MergeFilesGroup = app.MapGroup("/IAConferences/MergeFiles");
            var SyncConfDatabaseGroup = app.MapGroup("/IAConferences/Sync");
            BasicGroup.MapGet("/id/{id}", IAConferencesHandlers.GetInfosFromId);

            MergeFilesGroup.MapGet("/idSyracuse/{idSyracuse}", IAConferencesHandlers.MergeMediaFilesOnDisk);
            MergeFilesGroup.MapGet("/idDocnum/{RootIdDocnum}", IAConferencesHandlers.MergeMediaFilesFromIdDocNumOnDisk);
            SyncConfDatabaseGroup.MapGet("/id/{id}", IAConferencesHandlers.SyncConferencesDatabaseFromId);
            SyncConfDatabaseGroup.MapGet("/idDocnum/{rootIdDocnum}", IAConferencesHandlers.SyncConferencesDatabaseFromIdDocN);
        }
    }
}
