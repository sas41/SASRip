using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SASRip.Helpers
{
    public class DownloadHandler
    {
        // Possible Status Messages
        static string file_ready;
        static string file_processing;
        static string file_not_found;

        static DownloadHandler()
        {
            file_ready = Data.AppConfig.Configuration["StatusFileReady"];
            file_processing = Data.AppConfig.Configuration["StatusFileProcessing"];
            file_not_found = Data.AppConfig.Configuration["StatusFileNotFound"];
        }

        public static bool ValidateURLForYoutubeDL(string url, out string clean_url)
        {
            bool is_valid_url;

            // Start by validating the URL.
            Uri uri;
            is_valid_url = Uri.TryCreate(url, UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            // Youtube Check for better caching
            if (uri.Authority == "www.youtube.com" || uri.Authority == "youtu.be")
            {
                var query = HttpUtility.ParseQueryString(uri.Query);

                var videoId = string.Empty;

                if (query.AllKeys.Contains("v"))
                {
                    videoId = query["v"];
                }
                else
                {
                    videoId = uri.Segments.Last();
                }

                clean_url = videoId;
            }
            else
            {
                clean_url = uri.AbsoluteUri;
            }
            
            return is_valid_url;
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
            wwwroot = "." + Data.AppConfig.Configuration["wwwroot"];
            output_path = wwwroot + Data.AppConfig.Configuration["StoredFilesPath"];

            video_path = Data.AppConfig.Configuration["VideoSubPath"];
            video_arguments = Data.AppConfig.Configuration["VideoArguments"];
            video_name = Data.AppConfig.Configuration["VideoName"];

            audio_path = Data.AppConfig.Configuration["AudioSubPath"];
            audio_arguments = Data.AppConfig.Configuration["AudioArguments"];
            audio_name = Data.AppConfig.Configuration["AudioName"];

        }

        public bool Download(bool isVideo, string download_url, string call_source, out string path_on_disk, out string status)
        {

            // Calculate the URL Hash and
            // determine the cli arguments for youtube-dl.
            string hash = Helpers.SHA256Encoder.EncodeString(download_url);

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

            int exit_code = 1;
            // If the file is not queued or already downloaded,
            // run YoutubeDL and update/add it to the cache.
            if (!Services.LocalMediaCacheService.MediaCache.IsDone(save_path) && !Services.LocalMediaCacheService.MediaCache.IsInQueue(save_path))
            {
                Services.LocalMediaCacheService.MediaCache.MarkAsQueued(save_path, Path.GetFullPath(save_path));

                exit_code = Services.YoutubeDL.DownloadMedia(youtubedl_args, Path.GetFullPath(save_path));

                if (exit_code == 0)
                {
                    Services.LocalMediaCacheService.MediaCache.MarkAsDone(save_path, Path.GetFullPath(save_path));
                }
                else
                {
                    Services.LocalMediaCacheService.MediaCache.MarkAsFailed(save_path, Path.GetFullPath(save_path));
                }
            }

            // Log the URL.
            LogDownloadOperation(download_url, hash, call_source, exit_code);

            // Return Depending on the status.
            if (Services.LocalMediaCacheService.MediaCache.IsDone(save_path))
            {
                Services.LocalMediaCacheService.MediaCache.ExtendCacheTime(save_path);
                string path = Directory.GetFiles(save_path)[0].Replace(wwwroot, "");
                path = path.Replace("\\", "/");
                path = path.Replace("//", "/");
                path_on_disk = path;
                status = file_ready;
                return true;
            }
            else if (Services.LocalMediaCacheService.MediaCache.IsInQueue(save_path))
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

        private void LogDownloadOperation(string url, string hash, string call_source, int exit_code)
        {
            DateTime now = DateTime.Now;
            string year = now.Year.ToString();
            string month = now.Month.ToString().PadLeft(2, '0');
            string day = now.Day.ToString().PadLeft(2, '0');

            string hour = now.Hour.ToString().PadLeft(2, '0');
            string minute = now.Minute.ToString().PadLeft(2, '0');
            string second = now.Second.ToString().PadLeft(2, '0');
            string millisecond = now.Millisecond.ToString().PadLeft(4, '0');


            string date = $"{year}-{month}-{day}";
            string time = $"{hour}:{minute}:{second}:{millisecond}";
            string path = $"{output_path}/debug_urls_for_{date}.txt";
            call_source = call_source + "          ";
            call_source = call_source.Substring(0, 10);

            string status = "FAILED!";
            if (exit_code == 0)
            {
                status = "SUCCESS";
            }

            File.AppendAllText(path, $"{date} ~ {time} - {call_source} - {status} - {hash} - {url}{Environment.NewLine}");
        }

    }
}
