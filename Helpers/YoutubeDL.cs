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
    public class YoutubeDL
    {
        static Dictionary<string, CacheInfo> ProcessingQueue { get; set; }
        static YoutubeDL()
        {
            ProcessingQueue = new Dictionary<string, CacheInfo>();
        }

        public IConfiguration Configuration { get; }

        string youtubedl_path;
        string output_path;
        string wwwroot;

        string video_arguments;
        string video_name;

        string audio_arguments;
        string audio_name;


        // Possible Status Messages
        string file_ready;
        string file_processing;
        string file_not_found;


        public YoutubeDL(IConfiguration configuration)
        {
            Configuration = configuration;
            youtubedl_path = configuration["YoutubeDLPath"];
            wwwroot = "." + configuration["wwwroot"];
            output_path = wwwroot + configuration["StoredFilesPath"];

            video_arguments = configuration["VideoArguments"];
            video_name = configuration["VideoName"];

            audio_arguments = configuration["AudioArguments"];
            audio_name = configuration["AudioName"];

            file_ready = configuration["StatusFileReady"];
            file_processing = configuration["StatusFileProcessing"];
            file_not_found = configuration["StatusFileNotFound"];
        }

        public String sha256_hash(string value)
        {
            StringBuilder Sb = new StringBuilder();

            using (var hash = SHA256.Create())
            {
                Encoding enc = Encoding.UTF8;
                Byte[] result = hash.ComputeHash(enc.GetBytes(value));

                foreach (Byte b in result)
                    Sb.Append(b.ToString("x2"));
            }

            return Sb.ToString();
        }

        public bool Download(bool isVideo, string download_url, out string path_on_disk, out string status)
        {

            // Calculate the URL Hash and
            // determine the cli arguments for youtube-dl.
            string hash = sha256_hash(download_url);

            string save_path_debug = $"{output_path}/{hash}/";

            string save_path;
            string youtubedl_args;

            if (isVideo)
            {
                save_path = $"{output_path}/{hash}/MP4";
                youtubedl_args = $"{video_arguments} --output \"{save_path}/{video_name}\" -- {download_url}";
            }
            else
            {
                save_path = $"{output_path}/{hash}/MP3";
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
            LogURL(save_path_debug, download_url);



            // If the file is not queued or already downloaded,
            // run YoutubeDL and update/add it to the cache.
            if (!IsDone(save_path) && !IsInQueue(save_path))
            {
                MarkAsQueued(save_path);

                int exit_code = YoutoubeDL(youtubedl_args);

                if (exit_code == 0)
                {
                    MarkAsDone(save_path);
                }
                else
                {
                    MarkAsFailed(save_path);
                }
            }


            // Return Depending on the status.
            if (IsDone(save_path))
            {
                string path = Directory.GetFiles(save_path)[0].Replace(wwwroot, "");
                path = path.Replace("\\", "/");
                path = path.Replace("//", "/");
                path_on_disk = path;
                status = file_ready;
                return true;
            }
            else if (IsInQueue(save_path))
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

        private int YoutoubeDL(string args)
        {
            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo.FileName = youtubedl_path;
                process.StartInfo.Arguments = args;

                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.OutputDataReceived += (sender, data) => Console.WriteLine(data.Data);
                process.ErrorDataReceived += (sender, data) => Console.WriteLine(data.Data);

                Console.WriteLine("[YOUTUBE-DL] STARTING...");

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();


                Console.WriteLine($"[YOUTUBE-DL] DONE!");
                return process.ExitCode;
            }
        }

        private void LogURL(string path, string url)
        {
            path += "debug_urls.txt";
            using (TextWriter tw = new StreamWriter(path))
            {
                if (!File.Exists(path))
                {
                    File.Create(path);
                    tw.WriteLine(url);
                }
                else if (File.Exists(path))
                {
                    tw.WriteLine(url);
                }
            }
        }

        // Chaching Helper Methods
        private void MarkAsQueued(string uniquePath)
        {
            if (!ProcessingQueue.ContainsKey(uniquePath))
            {
                ProcessingQueue.Add(uniquePath, new CacheInfo(file_processing, DateTime.Now));
            }
            else
            {
                ProcessingQueue[uniquePath] = new CacheInfo(file_processing, DateTime.Now);
            }
        }
        private void MarkAsDone(string uniquePath)
        {
            if (ProcessingQueue.ContainsKey(uniquePath))
            {
                ProcessingQueue[uniquePath].Status = file_ready;
            }
        }
        private void MarkAsFailed(string uniquePath)
        {
            if (!ProcessingQueue.ContainsKey(uniquePath))
            {
                ProcessingQueue.Add(uniquePath, new CacheInfo(file_not_found, DateTime.Now));
            }
            else
            {
                ProcessingQueue[uniquePath].Status = file_not_found;
            }
        }
        private bool IsInQueue(string uniquePath)
        {
            if (ProcessingQueue.ContainsKey(uniquePath))
            {
                return ProcessingQueue[uniquePath].Status == file_processing;
            }
            else
            {
                return false;
            }
        }
        private bool IsDone(string uniquePath)
        {
            if (ProcessingQueue.ContainsKey(uniquePath))
            {
                return ProcessingQueue[uniquePath].Status == file_ready;
            }
            else
            {
                return false;
            }
        }
    }
}
