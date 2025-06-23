namespace MedUtils.Features.Deneb
{
    public static class DenebHandlers
    {
      
        public static async Task<IResult> GetToken()
        {
            string jsonData = await DenebTools.GetToken();
            return Results.Content(jsonData, "application/json");

        //    string jsonData = "{\"token\":\"toto\"}";
        //    return Results.Content(jsonData, "application/json");
        }

        public static async Task<IResult> GetEventFromId(string id)
        {
            string jsonData = DenebTools.GetEventFromId(id);
            return Results.Content(jsonData, "application/json");
        }


    }
}

