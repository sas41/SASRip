using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SASRip.Helpers
{
    public class DownloadHandler
    {

        public static IMediaCache MediaCache { get; private set; }

        // Possible Status Messages
        static string file_ready;
        static string file_processing;
        static string file_not_found;

        static DownloadHandler()
        {
            file_ready = AppConfig.Configuration["StatusFileReady"];
            file_processing = AppConfig.Configuration["StatusFileProcessing"];
            file_not_found = AppConfig.Configuration["StatusFileNotFound"];

            MediaCache = new MediaCache();
        }

        string output_path;
        string wwwroot;

        string video_path;
        string video_arguments;
        string video_name;

        string audio_path;
        string audio_arguments;
        string audio_name;

        public DownloadHandler()
        {
            wwwroot = "." + AppConfig.Configuration["wwwroot"];
            output_path = wwwroot + AppConfig.Configuration["StoredFilesPath"];

            video_path = AppConfig.Configuration["VideoSubPath"];
            video_arguments = AppConfig.Configuration["VideoArguments"];
            video_name = AppConfig.Configuration["VideoName"];

            audio_path = AppConfig.Configuration["AudioSubPath"];
            audio_arguments = AppConfig.Configuration["AudioArguments"];
            audio_name = AppConfig.Configuration["AudioName"];

        }

        public bool Download(bool isVideo, string download_url, out string path_on_disk, out string status)
        {

            // Calculate the URL Hash and
            // determine the cli arguments for youtube-dl.
            string hash = SHA256Encoder.EncodeString(download_url);

            string save_path_debug = $"{output_path}/{hash}/";

            string save_path;
            string youtubedl_args;

            if (isVideo)
            {
                save_path = $"{output_path}/{hash}/{video_path}";
                youtubedl_args = $"{video_arguments} --output \"{save_path}/{video_name}\" -- {download_url}";
            }
            else
            {
                save_path = $"{output_path}/{hash}/{audio_path}";
                youtubedl_args = $"{audio_arguments} --output \"{save_path}/{audio_name}\" -- {download_url}";
            }

            Console.WriteLine("[YOUTUBE-DL] ARGUMENTS:");
            Console.WriteLine(youtubedl_args);


            // Prepare the path for download.
            if (!Directory.Exists(save_path))
            {
                Console.WriteLine("[YOUTUBE-DL] Creating Directory...");
                Directory.CreateDirectory(save_path);
            }

            // Log the URL.
            LogDownloadOperation(download_url, hash);



            // If the file is not queued or already downloaded,
            // run YoutubeDL and update/add it to the cache.
            if (!MediaCache.IsDone(save_path) && !MediaCache.IsInQueue(save_path))
            {
                MediaCache.MarkAsQueued(save_path, Path.GetFullPath(save_path));

                int exit_code = YoutubeDL.DownloadMedia(youtubedl_args);

                if (exit_code == 0)
                {
                    MediaCache.MarkAsDone(save_path, Path.GetFullPath(save_path));
                }
                else
                {
                    MediaCache.MarkAsFailed(save_path, Path.GetFullPath(save_path));
                }
            }


            // Return Depending on the status.
            if (MediaCache.IsDone(save_path))
            {
                string path = Directory.GetFiles(save_path)[0].Replace(wwwroot, "");
                path = path.Replace("\\", "/");
                path = path.Replace("//", "/");
                path_on_disk = path;
                status = file_ready;
                return true;
            }
            else if (MediaCache.IsInQueue(save_path))
            {
                path_on_disk = "";
                status = file_processing;
                return false;
            }
            else
            {
                path_on_disk = "";
                status = file_not_found;
                return false;
            }
        }


        private void LogDownloadOperation(string url, string hash)
        {
            string path = output_path + "/debug_urls.txt";
            using (TextWriter tw = new StreamWriter(path))
            {
                if (!File.Exists(path))
                {
                    File.Create(path);
                }
                tw.WriteLine($"{DateTime.Now} - {hash} - {url}");
            }
        }

    }
}
