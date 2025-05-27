namespace MedUtils.Features.Syracuse
{
    public static class SyracuseEndpoints
    {
        public static void MapSyracuseEndpoints(this WebApplication app)
        {
            var BasicGroup = app.MapGroup("/Syracuse/query");
            var AdvancedGroup = app.MapGroup("/Syracuse/advancedQuery");

            BasicGroup.MapGet("/id/{id}", SyracuseHandlers.GetRecordFromId);
            BasicGroup.MapGet("/idDocnum/{idDocnum}", SyracuseHandlers.GetRecordFromIdDocnum);
            BasicGroup.MapGet("/id/field/subfield/{id}/{field}/{subfield}", SyracuseHandlers.GetValuesFromRecord);
            BasicGroup.MapGet("/id/xxx$x/{id}/{fieldandsubfield}", SyracuseHandlers.GetValuesFromRecordWithDollar);

            AdvancedGroup.MapGet("/id/{id}", SyracuseHandlers.GetAdvancedRecordFromId);
            AdvancedGroup.MapGet("/getNoticeType/id/{id}", SyracuseHandlers.GetNoticeTypeFromId);
        }
    }
}