using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using SASRip.Helpers;

namespace SASRip.Services
{
    public class ClearMediaCacheService
    {
        static int scanRepeatTime, lifetime;

        static ClearMediaCacheService()
        {
            scanRepeatTime = int.Parse(AppConfig.Configuration["CachedMediaCheckupTime"]);
            lifetime = int.Parse(AppConfig.Configuration["CachedMediaLifeTime"]);
        }

        public async void Run()
        {
            ClearCachedMedia();

            while (true)
            {
                Cleanup();
                Thread.Sleep(1000 * scanRepeatTime);
            }
        }

        private void ClearCachedMedia()
        {
            string wwwroot = "." + AppConfig.Configuration["wwwroot"];
            string output_path = wwwroot + AppConfig.Configuration["StoredFilesPath"] + "/";
            string path = Path.GetFullPath(output_path);
            if (Directory.Exists(path))
            {
                foreach (var folder in Directory.GetDirectories(path))
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        private void Cleanup()
        {
            List<string> keys = DownloadHandler.MediaCache.MediaCacheStatus.Keys.ToList();
            foreach (var key in keys)
            {
                if (DateTime.Now.Subtract(DownloadHandler.MediaCache.MediaCacheStatus[key].TimeOfCreation).TotalMinutes > lifetime)
                {
                    if (Directory.Exists(DownloadHandler.MediaCache.MediaCacheStatus[key].AbsolutePath))
                    {
                        string path = DownloadHandler.MediaCache.MediaCacheStatus[key].AbsolutePath;
                        string parent = new DirectoryInfo(path).Parent.FullName;

                        Directory.Delete(path, true);

                        if (Directory.GetDirectories(parent).Count() == 0)
                        {
                            Directory.Delete(parent, true);
                        }

                        DownloadHandler.MediaCache.MediaCacheStatus.Remove(key);
                    }
                }
            }
        }
    }
}
