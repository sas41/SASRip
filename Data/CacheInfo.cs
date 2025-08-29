using System;

namespace SASRip.Data;

public class CacheInfo
{
    public enum Statuses
    {
        Ready = 0,
        Processing = 1,
        Failed = 2
    }

    public DateTime TimeOfCreation { get; set; }
    public Statuses Status { get; set; }

    public string AbsolutePath { get; set; }

    public CacheInfo(Statuses status, DateTime toc, string path)
    {
        Status = status;
        TimeOfCreation = toc;
        AbsolutePath = path;
    }
}
