using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SASRip.Interfaces;

namespace SASRip.Services
{
    public class DownloadHandlerService : IDownloadHandler
    {
        // Possible Status Messages
        static string file_ready;
        static string file_processing;
        static string file_not_found;

        private readonly IMediaDownloader downloader;
        private readonly IMediaCache cache;

        static DownloadHandlerService()
        {
            file_ready = Data.AppConfig.Configuration["StatusFileReady"];
            file_processing = Data.AppConfig.Configuration["StatusFileProcessing"];
            file_not_found = Data.AppConfig.Configuration["StatusFileNotFound"];
        }

        public DownloadHandlerService(IMediaDownloader mediaDownloader, IMediaCache mediaCache)
        {
            downloader = mediaDownloader;
            cache = mediaCache;
        }
        public bool Download(bool isVideo, string downloadURL, string callSource, out string pathOnDisk, out string status)
        {
            string path = "";
            string key = $"{isVideo}{downloadURL}";
            string hash = Helpers.SHA256Encoder.EncodeString(downloadURL);
            downloadURL = YouTubeChacheHack(downloadURL);

            if (!cache.IsDone(key) && !cache.IsInQueue(key))
            {
                cache.MarkAsQueued(key, "");
                LogDownloadOperation(downloadURL, hash, callSource, ">>> START "); // OpLog.
                try
                {
                    if (isVideo)
                    {
                        path = downloader.DownloadVideo(downloadURL);
                    }
                    else
                    {
                        path = downloader.DownloadAudio(downloadURL);
                    }

                    cache.MarkAsDone(key, path);
                }
                catch (FileNotFoundException)
                {
                    cache.MarkAsFailed(key, "");
                }
            }

            // Return Depending on the status.
            if (cache.IsDone(key))
            {
                cache.ExtendCacheTime(key);
                string filePath = cache.MediaCacheStatus[key].AbsolutePath;
                pathOnDisk = AbsoluteToRelativePath(filePath);
                status = file_ready;
                LogDownloadOperation(downloadURL, hash, callSource, "+++ DONE  "); // OpLog.
                return true;
            }
            else if (cache.IsInQueue(key))
            {
                pathOnDisk = "";
                status = file_processing;
                LogDownloadOperation(downloadURL, hash, callSource, "... QUEUED"); // OpLog.
                return false;
            }
            else
            {
                pathOnDisk = "";
                status = file_not_found;
                LogDownloadOperation(downloadURL, hash, callSource, "--- FAILED"); // OpLog.
                return false;
            }
        }

        // This dirty little hack actually improves chaching, bit ashamed.
        // But this is better than having potentially thousands of copies
        // of the same video for each time-stamp and YouTube domain, like
        // (m.youtube.com), (youtu.be), (youtube.com) each with different
        // possible time-stamps for the video.
        private string YouTubeChacheHack(string url)
        {
            Uri uri;
            bool isValidURL = Uri.TryCreate(url, UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            string finalURL;

            // Youtube Check for better caching
            if (uri.Authority == "www.youtube.com" || uri.Authority == "youtu.be" || uri.Authority == "m.youtube.com")
            {
                var query = HttpUtility.ParseQueryString(uri.Query);

                var videoId = string.Empty;

                if (query.AllKeys.Contains("v"))
                {
                    videoId = query["v"];
                }
                else
                {
                    videoId = uri.Segments.Last();
                }

                finalURL = videoId;
            }
            else
            {
                finalURL = uri.AbsoluteUri;
            }

            return finalURL;
        }
        private string AbsoluteToRelativePath(string absolute)
        {
            string relative = absolute;
            relative = relative.Replace("\\", "/");
            relative = relative.Replace("//", "/");
            int start = relative.LastIndexOf("/wwwroot");
            int count = relative.Length - start;

            relative = relative.Substring(start, count);

            relative = relative.Replace("/wwwroot", "");

            return relative;
        }
        private void LogDownloadOperation(string url, string hash, string callSource, string status)
        {
            DateTime now = DateTime.Now;
            string year = now.Year.ToString();
            string month = now.Month.ToString().PadLeft(2, '0');
            string day = now.Day.ToString().PadLeft(2, '0');

            string hour = now.Hour.ToString().PadLeft(2, '0');
            string minute = now.Minute.ToString().PadLeft(2, '0');
            string second = now.Second.ToString().PadLeft(2, '0');
            string millisecond = now.Millisecond.ToString().PadLeft(4, '0');


            string date = $"{year}-{month}-{day}";
            string time = $"{hour}:{minute}:{second}:{millisecond}";
            string folderpath = "_DebugLogs";
            string path = $"_DebugLogs/debug_urls_for_{date}.txt";

            callSource = callSource + "          ";
            callSource = callSource.Substring(0, 10);


            if (!Directory.Exists(folderpath))
            {
                Directory.CreateDirectory(folderpath);
            }

            File.AppendAllText(path, $"{date} ~ {time} - {callSource} - {status} - {hash} - {url}{Environment.NewLine}");
        }
    }
}
