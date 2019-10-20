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
        static Dictionary<string, string> ProcessingQueue { get; set; }
        static YoutubeDL()
        {
            ProcessingQueue = new Dictionary<string, string>();
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
            // Get a hash of the URL, for caching.
            Console.WriteLine("[YOUTUBE-DL] Getting URL Hash...");
            string hash = sha256_hash(download_url);



            string save_path_debug = $"{output_path}/{hash}/";
            Console.WriteLine("[YOUTUBE-DL] Debug Path: ");
            Console.WriteLine(save_path_debug);

            string save_path;
            string youtubedl_args;
            if (isVideo)
            {
                // Set Youtube-DL output path to default_path/url_hash/MP4.
                save_path = $"{output_path}/{hash}/MP4";
                youtubedl_args = $"{video_arguments} --output \"{save_path}/{video_name}\" -- {download_url}";
            }
            else
            {
                save_path = $"{output_path}/{hash}/MP3";
                youtubedl_args = $"{audio_arguments} --output \"{save_path}/{audio_name}\" -- {download_url}";

            }

            Console.WriteLine("[YOUTUBE-DL] Save Path: ");
            Console.WriteLine(save_path);
            Console.WriteLine("[YOUTUBE-DL] ARGUMENTS:");
            Console.WriteLine(youtubedl_args);

            // If directory exists, we have attempted to download this before.
            if (!Directory.Exists(save_path))
            {
                MarkAsQueued(save_path);
                Console.WriteLine("[YOUTUBE-DL] Creating Directory...");
                Directory.CreateDirectory(save_path);

                using (var process = new System.Diagnostics.Process())
                {
                    //string save_arg = $"\"{save_path}/{video_name}\" ";
                    //string youtubedl_args = video_arguments + save_arg + $"-- {download_url}";

                    process.StartInfo.FileName = youtubedl_path;
                    process.StartInfo.Arguments = youtubedl_args;

                    //Console.WriteLine("[YOUTUBE-DL] ARGUMENTS:");
                    //Console.WriteLine(process.StartInfo.Arguments);

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
                }

                MarkAsDone(save_path);
            }

            LogURL(save_path_debug, download_url);

            // If there is a file after the process above, then we send it.
            // If not, then we failed.
            if (Directory.GetFiles(save_path).Count() == 1 && !IsInQueue(save_path))
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

        private void MarkAsQueued(string uniquePath)
        {
            if (!ProcessingQueue.ContainsKey(uniquePath))
            {
                ProcessingQueue.Add(uniquePath, file_processing);
            }
            else
            {
                ProcessingQueue[uniquePath] = file_processing;
            }
        }
        private void MarkAsDone(string uniquePath)
        {
            if (ProcessingQueue.ContainsKey(uniquePath))
            {
                ProcessingQueue.Remove(uniquePath);
            }
        }

        private bool IsInQueue(string uniquePath)
        {
            return ProcessingQueue.ContainsKey(uniquePath);
        }
    }
}
