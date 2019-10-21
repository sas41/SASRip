using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SASRip.Helpers
{
    public interface IMediaCache
    {
        Dictionary<string, CacheInfo> MediaCacheStatus { get; set; }
        void MarkAsQueued(string key, string path);
        void MarkAsDone(string key, string path);
        void MarkAsFailed(string key, string path);

        bool IsInQueue(string key);
        bool IsDone(string key);
        bool IsFailed(string key);
    }
}
