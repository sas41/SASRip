using System;
using System.Collections.Generic;
using System.Linq;
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

        public static int DownloadMedia(string args)
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
    }
}
