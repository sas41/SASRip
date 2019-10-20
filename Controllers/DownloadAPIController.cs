using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SASRip.Helpers;

namespace SASRip.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DownloadAPIController : ControllerBase
    {   
        // Need the configuration file to read paths for youtubedl and such.
        public IConfiguration Configuration { get; }

        public DownloadAPIController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // API only needs to know the type, we pass the URL in the header.
        [HttpGet("{type}")]
        public ActionResult<string> Get(string type)
        {
            // Instance a Youtube-DL process.
            YoutubeDL youtubeDL = new YoutubeDL(Configuration);
            
            bool isVideo = type.ToLower() == "mp4";

            Console.WriteLine();
            Console.WriteLine("[DownloadAPIController] Headers:");
            Console.WriteLine(Request.Headers.Count());
            foreach (var kvp in Request.Headers)
            {
                Console.WriteLine(kvp.Key + " - " + kvp.Value);
            }

            // Get the requested URL from the header.
            string download_url = Request.Headers["download_url"];

            Console.WriteLine($"[DownloadAPIController] Incoming Request...");
            Console.WriteLine($"[DownloadAPIController] TYPE: {type}");
            Console.WriteLine($"[DownloadAPIController] URL: {download_url}");

            // Put everything in a try catch block, if something fails, return fail.
            try
            {
                // Start by validating the URL
                Uri initial_url;
                bool is_valid_url = Uri.TryCreate(download_url, UriKind.Absolute, out initial_url) && (initial_url.Scheme == Uri.UriSchemeHttp || initial_url.Scheme == Uri.UriSchemeHttps);

                // See if the URL redirects to somewhere else, for caching purposes.
                HttpWebRequest post_redirect = (HttpWebRequest)WebRequest.Create(initial_url.AbsoluteUri);
                post_redirect.AllowAutoRedirect = true;

                Uri post_redirect_url;
                bool is_valid_redirect_url = Uri.TryCreate(post_redirect.Address.AbsoluteUri, UriKind.Absolute, out post_redirect_url) && (post_redirect_url.Scheme == Uri.UriSchemeHttp || post_redirect_url.Scheme == Uri.UriSchemeHttps);

                // If the URLs are OK, proceed.
                if (is_valid_url && is_valid_redirect_url)
                {
                    // Take the final, post redirect URL to download from.
                    string final_url = post_redirect.GetResponse().ResponseUri.AbsoluteUri; // This might bite me in the ass later, as it's insecure, but stupid JS redirects are undetectable otherwise.

                    bool success;
                    string file_path;
                    string status;

                    Console.WriteLine($"[DownloadAPIController] Starting Youtube-DL...");
                    success = youtubeDL.Download(isVideo, final_url, out file_path, out status);
                    Console.WriteLine($"[DownloadAPIController] Youtube-DL Done!");

                    // Add the current domain in front of the relative path of the file.
                    string response_url = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}" + file_path;


                    // Craft and send a JSON response
                    return new DownloadResponse(success, response_url, isVideo, status).GetJSON();
                }

                Console.WriteLine();

            }
            catch (Exception apiProcessingException)
            {
                Console.WriteLine(apiProcessingException.Message);
                Console.WriteLine();

                return new DownloadResponse(false, "", isVideo, $"Internal Server Error: {apiProcessingException.Message}").GetJSON();
                //throw apiProcessingException;
            }

            return new DownloadResponse(false, "", isVideo, "Internal Server Error: Generic").GetJSON();
        }
    }
}