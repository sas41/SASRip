using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace SASRip.Services
{
    public class ClearMediaCacheService
    {
        static int scanRepeatTime, lifetime;

        static ClearMediaCacheService()
        {
            scanRepeatTime = int.Parse(Data.AppConfig.Configuration["CachedMediaCheckupTime"]);
            lifetime = int.Parse(Data.AppConfig.Configuration["CachedMediaLifeTime"]);
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
            string wwwroot = "." + Data.AppConfig.Configuration["wwwroot"];
            string output_path = wwwroot + Data.AppConfig.Configuration["StoredFilesPath"] + "/";
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
            List<string> keys = Services.LocalMediaCacheService.MediaCache.MediaCacheStatus.Keys.ToList();
            foreach (var key in keys)
            {
                if (DateTime.Now.Subtract(Services.LocalMediaCacheService.MediaCache.MediaCacheStatus[key].TimeOfCreation).TotalMinutes > lifetime)
                {
                    if (Directory.Exists(Services.LocalMediaCacheService.MediaCache.MediaCacheStatus[key].AbsolutePath))
                    {
                        string path = Services.LocalMediaCacheService.MediaCache.MediaCacheStatus[key].AbsolutePath;
                        string parent = new DirectoryInfo(path).Parent.FullName;

                        Directory.Delete(path, true);

                        if (Directory.GetDirectories(parent).Count() == 0)
                        {
                            Directory.Delete(parent, true);
                        }

                        Services.LocalMediaCacheService.MediaCache.MediaCacheStatus.Remove(key);
                    }
                }
            }
        }
    }
}
