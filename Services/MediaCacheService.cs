using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SASRip.Interfaces;
using SASRip.Data;

namespace SASRip.Services
{
    public class MediaCacheService : IMediaCache
    {
        public Dictionary<string, CacheInfo> MediaCacheStatus { get; set; }

        public MediaCacheService()
        {
            MediaCacheStatus = new Dictionary<string, CacheInfo>();
            // Start Background Cleanup Service.
            var clearingService = new MediaCacheCleanupWatchdog(this);
        }

        // Chaching Helper Methods
        public void MarkAsQueued(string key, string path)
        {
            if (!MediaCacheStatus.ContainsKey(key))
            {
                MediaCacheStatus.Add(key, new CacheInfo(CacheInfo.Statuses.Processing, DateTime.Now, path));
            }
            else
            {
                MediaCacheStatus[key] = new CacheInfo(CacheInfo.Statuses.Processing, DateTime.Now, path);
            }
        }

        public void MarkAsDone(string key, string path)
        {
            if (MediaCacheStatus.ContainsKey(key))
            {
                MediaCacheStatus[key].Status = CacheInfo.Statuses.Ready;
                MediaCacheStatus[key].AbsolutePath = path;
            }
            else
            {
                MediaCacheStatus.Add(key, new CacheInfo(CacheInfo.Statuses.Ready, DateTime.Now, path));
            }
        }

        public void MarkAsFailed(string key, string path)
        {
            if (!MediaCacheStatus.ContainsKey(key))
            {
                MediaCacheStatus.Add(key, new CacheInfo(CacheInfo.Statuses.Failed, DateTime.Now, path));
            }
            else
            {
                MediaCacheStatus[key].Status = CacheInfo.Statuses.Failed;
            }
        }

        public void ExtendCacheTime(string key)
        {
            if (MediaCacheStatus.ContainsKey(key) && MediaCacheStatus[key].Status == CacheInfo.Statuses.Ready)
            {
                MediaCacheStatus[key].TimeOfCreation = DateTime.Now;
            }
        }

        public bool IsInQueue(string key)
        {
            if (MediaCacheStatus.ContainsKey(key))
            {
                return MediaCacheStatus[key].Status == CacheInfo.Statuses.Processing;
            }
            else
            {
                return false;
            }
        }

        public bool IsDone(string key)
        {
            if (MediaCacheStatus.ContainsKey(key))
            {
                return MediaCacheStatus[key].Status == CacheInfo.Statuses.Ready;
            }
            else
            {
                return false;
            }
        }

        public bool IsFailed(string key)
        {
            return false;
        }

        TimeSpan IMediaCache.GetAge(string key)
        {
            if (MediaCacheStatus.ContainsKey(key))
            {
                return DateTime.Now.Subtract(MediaCacheStatus[key].TimeOfCreation);
            }
            return TimeSpan.Zero;
        }

    }
}
