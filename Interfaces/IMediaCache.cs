using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SASRip.Data;

namespace SASRip.Interfaces
{
    public interface IMediaCache
    {
        Dictionary<string, CacheInfo> MediaCacheStatus { get; set; }

        void MarkAsQueued(string key, string path);
        void MarkAsDone(string key, string path);
        void MarkAsFailed(string key, string path);

        void ExtendCacheTime(string key);

        bool IsInQueue(string key);
        bool IsDone(string key);
        bool IsFailed(string key);
    }
}
