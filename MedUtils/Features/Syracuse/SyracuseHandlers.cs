namespace MedUtils.Features.Syracuse
{
    public static class SyracuseHandlers
    {
        public static async Task<IResult> GetRecordFromId(string id)
        {
            string xmlData = await SyracuseTools.getRecordFromId(id);
            return Results.Content(xmlData, "application/xml");
        }

        public static async Task<IResult> GetRecordFromIdDocnum(string idDocnum)
        {
            string xmlData = await SyracuseTools.getRecordFromIdDocnum(idDocnum);
            return Results.Content(xmlData, "application/xml");
        }

        public static async Task<IResult> GetValuesFromRecord(string id, string field, char subfield)
        {
            List<string> result = await MarcDataField.getValues(id, field, subfield);
            return Results.Json(result);
        }
        public static async Task<IResult> GetValuesFromRecordWithDollar(string id, string fieldandsubfield)
        {
            List<string> result = await MarcDataField.getValues(id, fieldandsubfield);
            return Results.Json(result);
        }
        //GetAdvancedRecordFromId
        public static async Task<IResult> GetAdvancedRecordFromId(string id)
        {
            string xmlData = await SyracuseTools.getAdvancedRecordFromId(id);
            return Results.Content(xmlData, "application/xml");
        }
        //GetNoticeTypeFromId
        public static async Task<IResult> GetNoticeTypeFromId(string id)
        {
            string noticeType = await SyracuseTools.GetNoticeTypeFromId(id);
            return Results.Json(noticeType);
        }
    }
}