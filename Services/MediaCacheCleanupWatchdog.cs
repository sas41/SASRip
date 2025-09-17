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
        scanTimer.Start();
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

            // Handle entries with empty/null paths (often from failed downloads or queued items)
            if (string.IsNullOrEmpty(path))
            {
                bool isEmptyPathFailed = mediaCache.MediaCacheStatus[key].Status == CacheInfo.Statuses.Failed;
                bool isEmptyPathExpired = DateTime.Now.Subtract(mediaCache.MediaCacheStatus[key].TimeOfCreation).TotalSeconds > lifetime;

                // Remove failed entries or old processing entries with no path
                if (isEmptyPathFailed || isEmptyPathExpired)
                {
                    mediaCache.MediaCacheStatus.Remove(key);
                }
                continue;
            }

            bool lifetimeExceeded = DateTime.Now.Subtract(mediaCache.MediaCacheStatus[key].TimeOfCreation).TotalSeconds > lifetime;
            bool isFailed = mediaCache.MediaCacheStatus[key].Status == CacheInfo.Statuses.Failed;
            string parent;

            try
            {
                parent = new DirectoryInfo(path).Parent.FullName;
            }
            catch (Exception)
            {
                // Path exists but is invalid (malformed, access denied, etc.)
                // Remove the invalid entry and continue with other entries
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
