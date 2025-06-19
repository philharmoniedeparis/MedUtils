using MedUtils.Features.Syracuse;

namespace MedUtils.Features.IAConferences
{
    public static class IAConferencesEndPoints
    {
        public static void MapIAConferencesEndPoints(this WebApplication app)
        {
            var BasicGroup = app.MapGroup("/IAConferences/query");
            BasicGroup.MapGet("/id/{id}", IAConferencesHandlers.GetInfosFromId);
        }
    }
}
