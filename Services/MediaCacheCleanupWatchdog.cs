using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using SASRip.Interfaces;

namespace SASRip.Services
{
    public class MediaCacheCleanupWatchdog
    {
        static int scanRepeatTime, lifetime;

        static MediaCacheCleanupWatchdog()
        {
            scanRepeatTime = int.Parse(Data.AppConfig.Configuration["CachedMediaCheckupTime"]);
            lifetime = int.Parse(Data.AppConfig.Configuration["CachedMediaLifeTime"]);
        }

        IMediaCache mediaCache;

        public MediaCacheCleanupWatchdog(IMediaCache mc)
        {
            mediaCache = mc;
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
            string relativePath = Data.AppConfig.Configuration["RootOutputPath"] + "/";
            string path = Path.GetFullPath(relativePath);
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
            List<string> keys = mediaCache.MediaCacheStatus.Keys.ToList();
            foreach (var key in keys)
            {
                if (DateTime.Now.Subtract(mediaCache.MediaCacheStatus[key].TimeOfCreation).TotalMinutes > lifetime)
                {
                    if (Directory.Exists(mediaCache.MediaCacheStatus[key].AbsolutePath))
                    {
                        string path = mediaCache.MediaCacheStatus[key].AbsolutePath;
                        string parent = new DirectoryInfo(path).Parent.FullName;

                        Directory.Delete(path, true);

                        if (Directory.GetDirectories(parent).Count() == 0)
                        {
                            Directory.Delete(parent, true);
                        }

                        mediaCache.MediaCacheStatus.Remove(key);
                    }
                }
            }
        }
    }
}
