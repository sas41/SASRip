using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SASRip.Services
{
    public static class YoutubeDL
    {
        static string youtubedl_path;

        static YoutubeDL()
        {
            youtubedl_path = Data.AppConfig.Configuration["YoutubeDLPath"];
        }

        public static int DownloadMedia(string args, string full_path)
        {
            int exit_code = 1;
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

            try
            {
                SanitizeFileNames(full_path);
                return exit_code;
            }
            catch (Exception)
            {
                return 1;
            }
        }

        private static void SanitizeFileNames(string full_path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(full_path);
            FileInfo[] files = directoryInfo.GetFiles();
            foreach (FileInfo file in files)
            {
                File.Move(file.FullName, $"{file.Directory}/{Regex.Replace(file.Name, "[#|?|:|;|@|=|&]", "_")}");
            }
        }
    }
}
