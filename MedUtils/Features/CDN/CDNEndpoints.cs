using System.Runtime.CompilerServices;

namespace MedUtils.Features.CDN
{
    public static class CDNEndpoints
    {
        public static void MapCDNEndpoints(this WebApplication app) 
        {
            var BasicGroup = app.MapGroup("/CDN/UploadFileTest");
            BasicGroup.MapGet("/mediaFile/{sourceFilePath}", CDNHandlers.UploadMedia);
        }
    }
}
