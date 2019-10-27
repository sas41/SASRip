using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SASRip.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DownloadAPIController : ControllerBase
    {
        ///////////////////////
        //   Request types   //
        ///////////////////////

        [Produces("application/json", "text/plain;charset=utf-8")]
        [HttpGet("{version}/{type}")]
        public ActionResult<string> Get(string version, string type)
        {
            if (version == "v1.0")
            {
                return Version1(type);
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest, "Wrong or Deprecated API Version.");
            }
        }



        //////////////////////////////
        //   Various API Versions   //
        //////////////////////////////
        [Produces("application/json", "text/plain;charset=utf-8")]
        private ActionResult<string> Version1(string type)
        {
            bool is_valid_url;
            bool isVideo = type.ToLower() == "video";
            string final_url = "";



            if (type.ToLower() != "video" && type.ToLower() != "audio")
            {
                return InvalidType();
            }



            // Get the requested URL from the header.
            string download_url = Request.Headers["download-url"];

            if (download_url == "" || download_url == null)
            {
                return MissingURL();
            }



            // We have a type and we have a URL, so let's check if the URL is valid.
            try
            {
                is_valid_url = Helpers.DownloadHandler.ValidateURLForYoutubeDL(download_url, out final_url);
            }
            catch (Exception urlException)
            {
                Console.WriteLine(urlException.Message);
                Console.WriteLine();

                return InvalidURL();
            }

            try
            {
                // If the URLs are OK, proceed.
                if (is_valid_url)
                {
                    // Take the final, post redirect URL to download from.
                    bool success;
                    string file_path;
                    string status;

                    // Instance a Youtube-DL process.
                    Console.WriteLine($"[DownloadAPIController] Starting Youtube-DL...");
                    Helpers.DownloadHandler youtubeDL = new Helpers.DownloadHandler();
                    success = youtubeDL.Download(isVideo, final_url, out file_path, out status);
                    Console.WriteLine($"[DownloadAPIController] Youtube-DL Done!");

                    // Add the current domain in front of the relative path of the file.
                    string response_url = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}" + file_path;


                    // Craft and send a JSON response
                    return new JsonResult(new Data.DownloadResponse(success, response_url, status));
                }
                else
                {
                    return InvalidURL();
                }
            }
            catch (Exception youtubedlException)
            {
                Console.WriteLine(youtubedlException.Message);
                Console.WriteLine();

                return InternalServerError();
            }
        }



        //////////////////////////////////////////////////////
        //   Canned Status Codes for various errors below   //
        //////////////////////////////////////////////////////

        [Produces("text/plain; charset=utf-8")]
        private ObjectResult InvalidType()
        {
            return StatusCode(StatusCodes.Status400BadRequest, "Invalid Media Type, use [video] or [audio]");
        }
        [Produces("text/plain; charset=utf-8")]
        private ObjectResult InvalidURL()
        {
            return StatusCode(StatusCodes.Status400BadRequest, "Invalid header or URL for [download-url]");
        }
        [Produces("text/plain; charset=utf-8")]
        private ObjectResult MissingURL()
        {
            return StatusCode(StatusCodes.Status400BadRequest, "Missing URL for header [download_url]");
        }
        [Produces("text/plain; charset=utf-8")]
        private ObjectResult InternalServerError()
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Generic Internal Server Error.");
        }
    }
}