using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SASRip.Interfaces
{
    public interface IMediaDownloader
    {
        string DownloadVideo(string url, string hash);
        string DownloadAudio(string url, string hash);
    }
}
