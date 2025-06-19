namespace MedUtils.Features.Deneb
{
    public static class DenebEndpoints
    {
        public static void MapDenebEndpoints(this WebApplication app)
        {
            var BasicGroup = app.MapGroup("/Deneb");
           
            BasicGroup.MapGet("/token", DenebHandlers.GetToken);
            BasicGroup.MapGet("/event/{id}", DenebHandlers.GetEventFromId);

        }
    }
}
