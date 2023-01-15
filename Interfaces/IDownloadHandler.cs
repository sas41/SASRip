using SASRip.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SASRip.Interfaces
{
    public interface IDownloadHandler
    {
        public bool Download(bool isVideo, string downloadURL, string callSource, out string pathOnDisk, out RequestStatus status);
    }
}
