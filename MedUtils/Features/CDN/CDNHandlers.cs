namespace MedUtils.Features.CDN
{
    public class CDNHandlers
    {
        public static async Task<IResult> UploadMedia(string sourceFilePath) 
        { 
            bool IsUploadOk = await CDNTools.UploadMediaByFilePath(sourceFilePath);
            return Results.Json(IsUploadOk);
        }
    }
}
