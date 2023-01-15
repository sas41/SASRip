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
        string youtubeDLPath;
        string outputPath;

        string videoArguments;
        string audioArguments;

        string videoName;
        string audioName;

        public YoutubeDLService()
        {
            youtubeDLPath = Data.AppConfig.Configuration["YoutubeDLPath"];
            outputPath = Path.GetFullPath(Data.AppConfig.Configuration["RootOutputPath"]);

            videoArguments = Data.AppConfig.Configuration["VideoArguments"];
            audioArguments = Data.AppConfig.Configuration["AudioArguments"];

            videoName = Data.AppConfig.Configuration["VideoName"];
            audioName = Data.AppConfig.Configuration["AudioName"];
        }

        public string DownloadVideo(string url, string hash)
        {
            int exitCode = 1;

            string savePath = $"{outputPath}/{hash}";
            string fullPath = Path.GetFullPath(savePath);

            string args = $"{videoArguments} --output \"{savePath}/{videoName}\" -- {url}";

            if (!Directory.Exists(fullPath))
            {
                Console.WriteLine("[YOUTUBE-DL] Creating Directory...");
                Directory.CreateDirectory(fullPath);
            }

            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo.FileName = youtubeDLPath;
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
                exitCode = process.ExitCode;
            }

            if (exitCode == 0)
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
                throw new FileNotFoundException($"YouTube-DL exited with code ({exitCode})!");
            }
        }

        public string DownloadAudio(string url, string hash)
        {
            int exitCode = 1;

            string savePath = $"{outputPath}/{hash}";
            string fullPath = Path.GetFullPath(savePath);

            string args = $"{audioArguments} --output \"{savePath}/{audioName}\" -- {url}";

            if (!Directory.Exists(fullPath))
            {
                Console.WriteLine("[YOUTUBE-DL] Creating Directory...");
                Directory.CreateDirectory(fullPath);
            }

            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo.FileName = youtubeDLPath;
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
                exitCode = process.ExitCode;
            }

            if (exitCode == 0)
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
                throw new FileNotFoundException($"YouTube-DL exited with code ({exitCode})!");
            }
        }

        private string SanitizedFilePath(string fullPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(fullPath);
            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                if (file.Name.Replace(file.Extension, "").Trim().Length < 1)
                {
                    File.Move(file.FullName, $"file{file.Extension}");
                }
                else
                {
                    File.Move(file.FullName, $"{file.Directory}/{Regex.Replace(file.Name, "[#|?|:|;|@|=|&|%]", "_")}");
                }
            }

            var firstFile = directoryInfo.GetFiles().First().FullName;

            // These two steps are not needed, but it made my job debugging slightly easier.
            firstFile = firstFile.Replace("\\", "/");
            firstFile = firstFile.Replace("//", "/");

            return firstFile;
        }

    }
}
