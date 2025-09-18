namespace MedUtils.Features.CDN
{
    public class CDNHandlers
    {

        public static async Task<IResult> UploadImages(string idDocNum, CDNTools cdn) 
        { 
            List<string> FTPPath = await cdn.UploadImagesByIdDocNum(idDocNum);
            return Results.Json(FTPPath);
        }
    }
}
