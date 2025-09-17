using SASRip.Data;
using SASRip.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace SASRip.Services;

public class MediaCacheCleanupWatchdog
{
    private int lifetime;
    private string fileLocation;

    private IMediaCache mediaCache;
    private Timer scanTimer;

    public MediaCacheCleanupWatchdog(int scanRepeatTime, int lifetime, string fileLocation, IMediaCache mediaCache, bool cleanOnStartup = true)
    {
        this.mediaCache = mediaCache;
        this.lifetime = lifetime;
        this.fileLocation = fileLocation;

        if (cleanOnStartup)
        {
            ClearAllMedia();
        }

        scanTimer = new Timer(1000 * scanRepeatTime);
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
        string relativePath = fileLocation + "/";
        string path = Path.GetFullPath(relativePath);
        if (Directory.Exists(path))
        {
            foreach (string folder in Directory.GetDirectories(path))
            {
                RecursiveDelete(folder);
            }
        }
    }

    private void Cleanup(Object source, ElapsedEventArgs e)
    {
        List<string> keys = mediaCache.MediaCacheStatus.Keys.ToList();
        foreach (string key in keys)
        {
            string path = mediaCache.MediaCacheStatus[key].AbsolutePath;

            bool lifetimeExceeded = DateTime.Now.Subtract(mediaCache.MediaCacheStatus[key].TimeOfCreation).TotalSeconds > lifetime;
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
                continue;
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
            foreach (string folder in Directory.GetDirectories(path))
            {
                RecursiveDelete(folder);
            }

            foreach (string file in Directory.GetFiles(path))
            {
                File.Delete(file);
            }

            Directory.Delete(path);
        }
    }
}
