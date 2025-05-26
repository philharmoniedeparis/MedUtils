namespace MedUtils.Features.Syracuse
{
    public static class SyracuseEndpoints
    {
        public static void MapSyracuseEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/Syracuse/query");

            group.MapGet("/id/{id}", SyracuseHandlers.GetRecordFromId);
            group.MapGet("/idDocnum/{idDocnum}", SyracuseHandlers.GetRecordFromIdDocnum);
            group.MapGet("/id/field/subfield/{id}/{field}/{subfield}", SyracuseHandlers.GetValuesFromRecord);
            group.MapGet("/id/xxx$x/{id}/{fieldandsubfield}", SyracuseHandlers.GetValuesFromRecordWithDollar);
        }
    }
}