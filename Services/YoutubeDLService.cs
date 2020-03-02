using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SASRip.Interfaces;

namespace SASRip.Services
{
    public class YoutubeDLService : IMediaDownloader
    {
        string youtubedl_path;
        string output_path = "./wwwroot/files";

        string videoArguments = "--no-playlist --format bestvideo[ext=webm]+bestaudio[ext=webm]/bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=webm]/best[ext=mp4]/bestvideo+bestaudio/best";
        string audioArguments = "--no-playlist --extract-audio --audio-format mp3";

        string videoPath = "Video";
        string audioPath = "Audio";

        string videoName = "%(title)s - (%(resolution)s).%(ext)s";
        string audioName = "%(title)s.%(ext)s";

        public YoutubeDLService()
        {
            youtubedl_path = Data.AppConfig.Configuration["YoutubeDLPath"];
        }

        public string DownloadVideo(string url)
        {
            int exit_code = 1;

            string hash = Helpers.SHA256Encoder.EncodeString(url);
            string savePath = $"{output_path}/{hash}/{videoPath}";
            string fullPath = Path.GetFullPath(savePath);

            string args = $"{videoArguments} --output \"{savePath}/{videoName}\" -- {url}";

            if (!Directory.Exists(fullPath))
            {
                Console.WriteLine("[YOUTUBE-DL] Creating Directory...");
                Directory.CreateDirectory(fullPath);
            }

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
                exit_code = process.ExitCode;
            }

            if (exit_code == 0)
            {
                try
                {
                    return SanitizedFilePath(fullPath);
                }
                catch (Exception)
                {
                    throw new FileNotFoundException("File Name Sanitization Failed!");
                }
            }
            else
            {
                throw new FileNotFoundException($"YouTube-DL exited with code ({exit_code})!");
            }
        }

        public string DownloadAudio(string url)
        {
            int exit_code = 1;

            string hash = Helpers.SHA256Encoder.EncodeString(url);
            string savePath = $"{output_path}/{hash}/{audioPath}";
            string fullPath = Path.GetFullPath(savePath);

            string args = $"{audioArguments} --output \"{savePath}/{audioName}\" -- {url}";

            if (!Directory.Exists(fullPath))
            {
                Console.WriteLine("[YOUTUBE-DL] Creating Directory...");
                Directory.CreateDirectory(fullPath);
            }

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
                exit_code = process.ExitCode;
            }

            if (exit_code == 0)
            {
                try
                {
                    return SanitizedFilePath(fullPath);
                }
                catch (Exception)
                {
                    throw new FileNotFoundException("File Name Sanitization Failed!");
                }
            }
            else
            {
                throw new FileNotFoundException($"YouTube-DL exited with code ({exit_code})!");
            }

        }

        private string SanitizedFilePath(string full_path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(full_path);
            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                File.Move(file.FullName, $"{file.Directory}/{Regex.Replace(file.Name, "[#|?|:|;|@|=|&|%]", "_")}");
            }

            var firstFile = directoryInfo.GetFiles().First().FullName;

            // These two steps are not needed, but it made my job debugging slightly easier.
            firstFile = firstFile.Replace("\\", "/");
            firstFile = firstFile.Replace("//", "/");

            return firstFile;
        }

    }
}
