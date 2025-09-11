using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
 
        // Connection settings 
        private static readonly string Host = "citemus.atanar.net";
        private static readonly int Port = 444;
        private static readonly string Username = "vod";
        //private static readonly string KeyFilePath = "..\\..\\..\\..\\MedUtils\\Features\\CDN\\cdn.ppk"; // Consider moving this to user secrets


        /// <summary>
        /// Upload media files to CDN by media type
        /// </summary>
        /// <param name="MediaType">Type of media to upload (e.g., "audio", "video")</param>
        /// <param name="sourceFilePath">Local file path to upload</param>
        /// <param name="remoteDirectory">Target directory on remote server</param>
        /// <returns>True if upload was successful</returns>
        public async Task<bool> UploadMediaByFilePath(string sourceFilePath)
        {
            
            string rootPath = "C:\\Users\\rbailly\\Downloads\\";
            sourceFilePath = Path.Combine(rootPath, sourceFilePath);
            if (!File.Exists(sourceFilePath))
            {
                Console.WriteLine($"Source file not found: {sourceFilePath}");
                return false;
            }
            string remoteDirectory = "/http/test/";
            // Create a remote filename based on the source filename
            string remoteFileName = Path.GetFileName(sourceFilePath);
            string remoteFilePath = Path.Combine(remoteDirectory, remoteFileName).Replace("\\", "/");
            
            // Handle different media types
            //switch (MediaType.ToLower())
            //{
            //    case "audio":
            //        // Specific handling for audio files
            //        remoteDirectory = "/audio";
            //        remoteFilePath = Path.Combine(remoteDirectory, remoteFileName).Replace("\\", "/");
            //        break;
            //    case "video":
            //        // Specific handling for video files
            //        remoteDirectory = "/video";
            //        remoteFilePath = Path.Combine(remoteDirectory, remoteFileName).Replace("\\", "/");
            //        break;
            //}

            return await Task.Run(() => UploadFileWithSftp(sourceFilePath, remoteFilePath));
        }

        /// <summary>
        /// Upload a file using SFTP with SSH key authentication
        /// </summary>
        private  bool UploadFileWithSftp(string localFilePath, string remoteFilePath)
        {
            string KeyFilePath = GetKeyFilePath();

            try
            {
                using var client = new SftpClient(Host, Port, Username, new PrivateKeyFile(KeyFilePath));
                client.Connect();
                
                if (!client.IsConnected)
                {
                    Console.WriteLine("Failed to connect to SFTP server.");
                    return false;
                }
                
                // Make sure the directory exists
                EnsureDirectoryExists(client, Path.GetDirectoryName(remoteFilePath).Replace("\\", "/"));
                
                // Upload the file
                using var fileStream = new FileStream(localFilePath, FileMode.Open);
                client.UploadFile(fileStream, remoteFilePath, true); // Overwrite if exists
                
                client.Disconnect();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
                return false;
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
