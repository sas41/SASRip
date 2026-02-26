using System;

namespace SASRip.Data;

[Serializable]
public class DownloadResponse
{
    public bool Success { get; set; }
    public string Status { get; set; }
    public string DownloadPath { get; set; }

    public DownloadResponse(bool success, string downloadPath, string status)
    {
        Success = success;
        DownloadPath = downloadPath;
        Status = status;
    }
}
