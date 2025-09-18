using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MedUtils.Features.Medias;
using Microsoft.AspNetCore.Hosting;           // IWebHostEnvironment lives here
using Microsoft.Extensions.Configuration;     // IConfiguration
using Renci.SshNet;


namespace MedUtils.Features.CDN
{
    public class CDNTools
    {

        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        public CDNTools(IWebHostEnvironment env, IConfiguration config) 
        { 
            _env = env;
            _config = config;
        }

        // No parameter needed — we already have _env
        public string GetRootPath() => _env.ContentRootPath;        // project root
        // If you need wwwroot instead:  _env.WebRootPath

        public string GetKeyFilePath()
        {
            // appsettings.json:  "CDN": { "CDNkeyPath": "MedUtils/Features/CDN/cdn.ppk" }
            var configured = _config["CDN:CDNkeyPath"];
            if (string.IsNullOrWhiteSpace(configured))
                throw new InvalidOperationException("Missing configuration key: CDN:CDNkeyPath");

            // If the value is absolute, use it as-is; otherwise make it relative to ContentRoot
            return Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(_env.ContentRootPath, configured);
        }

        public string GetContentRootPath()
        { 
            return _env.ContentRootPath;
        }
 
        // Connection settings 
        private static readonly string Host = "citemus.atanar.net";
        private static readonly int Port = 444;
        private static readonly string Username = "vod";
        //private static readonly string KeyFilePath = "..\\..\\..\\..\\MedUtils\\Features\\CDN\\cdn.ppk"; // Consider moving this to user secrets


        /// <summary>
        /// Upload images files to CDN
        /// </summary>
        /// <param name="idDocNum">Example : PPVI000123400</param>
        /// <returns>List of uploaded files</returns>
        public async Task<List<string>> UploadImagesByIdDocNum(string idDocNum)
        {
            string LiveMediaPath = MediaTools.MediaParams.LiveMediaPath;
            string MediaPath = MediaTools.MediaParams.MediaPath;
            var Prefixes = MediaTools.MediaParams.Prefixes;
            var imagesDirectories = new HashSet<string> { "hd", "home", "ipad", "mini", "poster", "vignette", "docthumbs" };
            string ImagesRootDir = Path.Combine(LiveMediaPath, "images");
            string remoteRootDirectory = "/http/images/";
            string remoteDirectory;
            string remoteFileName;
            string remoteFilePath;
            string uploadedFile;
            List<string> filesUploaded = new();
            foreach (string imageDirectory in imagesDirectories)
            {
                List<string> images = getAllImages(idDocNum, Path.Combine(ImagesRootDir, imageDirectory));
                remoteDirectory = remoteRootDirectory + imageDirectory;
                foreach (string image in images) {
                    remoteFileName = Path.GetFileName(image);
                    remoteFilePath = Path.Combine(remoteDirectory, remoteFileName).Replace("\\", "/");
                    uploadedFile = await Task.Run(() => UploadFileWithSftp(image, remoteFilePath));
                    filesUploaded.Add(uploadedFile);
                }
            }
            return filesUploaded;
        }

        public static List<string> getAllImages(string idDocNum, string imageDirectory)
        {
            List<string> ListOfimages = new List<string>();            
            string idDocNumRoot = idDocNum[..11];
            if (Directory.Exists(imageDirectory))
            {

                var extensions = new[] { ".png", ".jpg", ".jpeg" };

                ListOfimages = Directory
                    .GetFiles(imageDirectory, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .Where(f => Path.GetFileName(f).StartsWith(idDocNumRoot, StringComparison.OrdinalIgnoreCase)) // optional filter
                    .ToList();
            }
            return ListOfimages;

        }

        /// <summary>
        /// Upload a file using SFTP with SSH key authentication
        /// </summary>
        private  string UploadFileWithSftp(string localFilePath, string remoteFilePath)
        {
            string KeyFilePath = GetKeyFilePath();

            try
            {
                using var client = new SftpClient(Host, Port, Username, new PrivateKeyFile(KeyFilePath));
                client.Connect();
                
                if (!client.IsConnected)
                {
                    Console.WriteLine("Failed to connect to SFTP server.");
                    return ("Failed to connect to SFTP server.");
                }
                
                // Make sure the directory exists
                EnsureDirectoryExists(client, Path.GetDirectoryName(remoteFilePath).Replace("\\", "/"));
                
                // Upload the file
                using var fileStream = new FileStream(localFilePath, FileMode.Open);
                client.UploadFile(fileStream, remoteFilePath, true); // Overwrite if exists
                fileStream.Close();
                client.Disconnect();
                return remoteFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
                return ($"Error uploading file: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensure directory exists on remote server, creating it if needed
        /// </summary>
        private static void EnsureDirectoryExists(SftpClient client, string directory)
        {
            string[] paths = directory.Split('/');
            string currentPath = "/";
            
            foreach (string path in paths.Where(p => !string.IsNullOrEmpty(p)))
            {
                currentPath = currentPath + path + "/";
                
                if (!DirectoryExists(client, currentPath))
                {
                    try
                    {
                        client.CreateDirectory(currentPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Cannot create directory {currentPath}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Check if directory exists on remote server
        /// </summary>
        private static bool DirectoryExists(SftpClient client, string path)
        {
            try
            {
                return client.Exists(path);
            }
            catch
            {
                return false;
            }
        }
    }
}
