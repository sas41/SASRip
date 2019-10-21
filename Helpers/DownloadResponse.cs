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
        public bool Success { get; set; }
        public string Status { get; set; }
        public string DownloadPath { get; set; }

        public DownloadResponse(bool success, string downloadPath, string status)
        {
            Success = success;
            DownloadPath = downloadPath;
            Status = status;
        }

        public string GetJSON()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
