using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Hosting;
using SASRip.Data;
using SASRip.Interfaces;

namespace SASRip.Services
{
    public class MediaCacheCleanupWatchdog
    {
        private static int scanRepeatTime, lifetime;

        private IMediaCache mediaCache;
        private System.Timers.Timer scanTimer;

        public MediaCacheCleanupWatchdog(IMediaCache mc, bool cleanOnStartup = true)
        {
            scanRepeatTime = int.Parse(Data.AppConfig.Configuration["CachedMediaCheckupTime"]);
            lifetime = int.Parse(Data.AppConfig.Configuration["CachedMediaLifeTime"]);

            if (cleanOnStartup)
            {
                ClearAllMedia();
            }

            mediaCache = mc;

            scanTimer = new System.Timers.Timer(1000 * scanRepeatTime);
            scanTimer.Elapsed += Cleanup;
            scanTimer.AutoReset = true;
            scanTimer.Enabled = true;
        }

        ~MediaCacheCleanupWatchdog()
        {
            scanTimer.Stop();
            scanTimer.Dispose();
        }

        private void ClearAllMedia()
        {
            string relativePath = Data.AppConfig.Configuration["RootOutputPath"] + "/";
            string path = Path.GetFullPath(relativePath);
            if (Directory.Exists(path))
            {
                foreach (var folder in Directory.GetDirectories(path))
                {
                    RecursiveDelete(folder);
                }
            }
        }

        private void Cleanup(Object source, ElapsedEventArgs e)
        {
            List<string> keys = mediaCache.MediaCacheStatus.Keys.ToList();
            foreach (var key in keys)
            {
                string path = mediaCache.MediaCacheStatus[key].AbsolutePath;

                bool lifetimeExceeded = DateTime.Now.Subtract(mediaCache.MediaCacheStatus[key].TimeOfCreation).TotalMinutes > lifetime;
                bool isFailed = mediaCache.MediaCacheStatus[key].Status == CacheInfo.Statuses.Failed;
                string parent;

                try
                {
                    parent = new DirectoryInfo(path).Parent.FullName;
                }
                catch (Exception)
                {

                    // Most likely outcome is, the path was not valid
                    // Can't do anything but remove it from the cache.
                    mediaCache.MediaCacheStatus.Remove(key);
                    return;
                }

                if (lifetimeExceeded || isFailed)
                {
                    if (Directory.Exists(parent))
                    {
                        RecursiveDelete(parent);
                        if (Directory.Exists(parent) == false)
                        {
                            mediaCache.MediaCacheStatus.Remove(key);
                        }
                    }
                }
            }
        }


        private void RecursiveDelete(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (var folder in Directory.GetDirectories(path))
                {
                    RecursiveDelete(folder);
                }

                foreach (var file in Directory.GetFiles(path))
                {
                    File.Delete(file);
                }

                Directory.Delete(path);
            }
        }

    }
}
