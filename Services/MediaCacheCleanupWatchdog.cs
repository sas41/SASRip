using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Hosting;
using SASRip.Interfaces;

namespace SASRip.Services
{
    public class MediaCacheCleanupWatchdog
    {
        private static int scanRepeatTime, lifetime;

        static MediaCacheCleanupWatchdog()
        {
            scanRepeatTime = int.Parse(Data.AppConfig.Configuration["CachedMediaCheckupTime"]);
            lifetime = int.Parse(Data.AppConfig.Configuration["CachedMediaLifeTime"]);
        }


        private IMediaCache mediaCache;
        private System.Timers.Timer scanTimer;
        public MediaCacheCleanupWatchdog(IMediaCache mc, bool cleanOnStartup = true)
        {
            mediaCache = mc;

            scanTimer = new System.Timers.Timer(1000 * scanRepeatTime);
            scanTimer.Elapsed += Cleanup;
            scanTimer.AutoReset = true;

            if (cleanOnStartup)
            {
                ClearAllMedia();
            }
        }

        ~MediaCacheCleanupWatchdog()
        {
            scanTimer.Stop();
            scanTimer.Dispose();
        }

        public async void Run()
        {
            scanTimer.Enabled = true;
        }

        private void ClearAllMedia()
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

        private void Cleanup(Object source, ElapsedEventArgs e)
        {
            List<string> keys = mediaCache.MediaCacheStatus.Keys.ToList();
            foreach (var key in keys)
            {
                if (DateTime.Now.Subtract(mediaCache.MediaCacheStatus[key].TimeOfCreation).TotalMinutes > lifetime)
                {
                    string path = mediaCache.MediaCacheStatus[key].AbsolutePath;
                    string parent = new DirectoryInfo(path).Parent.FullName;
                    if (Directory.Exists(parent))
                    {
                        Directory.Delete(parent, true);
                        if (Directory.Exists(parent) == false)
                        {
                            mediaCache.MediaCacheStatus.Remove(key);
                        }
                    }
                }
            }
        }
    }
}
