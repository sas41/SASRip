using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SASRip.Helpers
{
    [Serializable]
    public class DownloadResponse
    {
        const string mimeMP3 = "audio/mpeg";
        const string mimeMP4 = "video/mp4";
        public bool Success { get; set; }
        public string Status { get; set; }
        public string MimeType { get; set; }
        public string DownloadPath { get; set; }

        public DownloadResponse(bool success, string downloadPath, bool isVideo, string status)
        {
            Success = success;
            DownloadPath = downloadPath;
            Status = status;

            if (isVideo)
            {
                MimeType = mimeMP4;
            }
            else
            {
                MimeType = mimeMP3;
            }
        }

        public string GetJSON()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
