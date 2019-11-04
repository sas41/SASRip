using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SASRip.Data
{
    public class MediaCache : IMediaCache
    {
        public Dictionary<string, CacheInfo> MediaCacheStatus { get; set; }

        public MediaCache()
        {
            MediaCacheStatus = new Dictionary<string, CacheInfo>();
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
                Console.WriteLine(MediaCacheStatus[key].TimeOfCreation);
                return MediaCacheStatus[key].Status == CacheInfo.Statuses.Ready;
            }
            else
            {
                return false;
            }
        }

        // This is currently, unused.
        public bool IsFailed(string key)
        {
            return false;
        }
    }
}
