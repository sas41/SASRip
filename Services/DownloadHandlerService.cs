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
using Microsoft.Extensions.Logging;
using SASRip.Helpers;

namespace SASRip.Services
{
    public class DownloadHandlerService : IDownloadHandler
    {
        private readonly IMediaDownloader downloader;
        private readonly IMediaCache cache;

        Interfaces.ILogger logger;

        public DownloadHandlerService(IMediaDownloader mediaDownloaderService, IMediaCache mediaCacheService, Interfaces.ILogger loggerService)
        {
            logger = loggerService;
            downloader = mediaDownloaderService;
            cache = mediaCacheService;
        }

        public bool Download(bool isVideo, string downloadURL, string callSource, out string pathOnDisk, out RequestStatus status)
        {
            string path = "";
            string hash = Helpers.SHA256Encoder.EncodeString($"{isVideo}{downloadURL}");
            downloadURL = YouTubeCacheHack(downloadURL);
            bool success = false;

            if (!cache.IsDone(hash) && !cache.IsInQueue(hash))
            {
                cache.MarkAsQueued(hash, "");
                logger.Log(hash, downloadURL, callSource, isVideo, RequestStatus.Started);
                try
                {
                    if (isVideo)
                    {
                        path = downloader.DownloadVideo(downloadURL, hash);
                    }
                    else
                    {
                        path = downloader.DownloadAudio(downloadURL, hash);
                    }

                    cache.MarkAsDone(hash, path);
                }
                catch (Exception e)
                {
                    logger.LogError(hash, downloadURL, callSource, isVideo, e.Message);
                    cache.MarkAsFailed(hash, "");
                }
            }

            // Return Depending on the status.
            if (cache.IsDone(hash))
            {
                cache.ExtendCacheTime(hash);
                string filePath = cache.MediaCacheStatus[hash].AbsolutePath;
                pathOnDisk = AbsoluteToRelativePath(filePath);
                status = RequestStatus.Ready;
                success = true;
            }
            else if (cache.IsInQueue(hash))
            {
                pathOnDisk = "";
                status = RequestStatus.Processing;
            }
            else
            {
                pathOnDisk = "";
                status = RequestStatus.Failed;
            }

            logger.Log(hash, downloadURL, callSource, isVideo, status);
            return success;
        }

        // This dirty little hack actually improves chaching, bit ashamed.
        // But this is better than having potentially thousands of copies
        // of the same video for each time-stamp and YouTube domain, like
        // (m.youtube.com), (youtu.be), (youtube.com) each with different
        // possible time-stamps for the video.
        private string YouTubeCacheHack(string url)
        {
            Uri uri;
            bool isValidURL = Uri.TryCreate(url, UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            string finalURL;

            // Youtube Check for better caching
            if (uri.Authority == "www.youtube.com" || uri.Authority == "youtu.be" || uri.Authority == "m.youtube.com" || uri.Authority == "music.youtube.com")
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
            int start = relative.IndexOf("/wwwroot");
            int count = relative.Length - start;

            relative = relative.Substring(start, count);

            relative = relative.Replace("/wwwroot", "");

            return relative;
        }
    }
}
