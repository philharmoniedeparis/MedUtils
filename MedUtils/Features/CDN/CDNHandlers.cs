namespace MedUtils.Features.CDN
{
    public class CDNHandlers
    {

        public static async Task<IResult> UploadMedia(string sourceFilePath, CDNTools cdn) 
        { 
            bool IsUploadOk = await cdn.UploadMediaByFilePath(sourceFilePath);
            return Results.Json(IsUploadOk);
        }
    }
}
