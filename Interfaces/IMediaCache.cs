using SASRip.Data;
using System;
using System.Collections.Generic;

namespace SASRip.Interfaces;

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

    TimeSpan GetAge(string key);
}
