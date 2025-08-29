using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SASRip.Helpers;
using SASRip.Interfaces;
using SASRip.Models;
using System;

namespace SASRip.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DownloadAPIController : ControllerBase
{
    private readonly IDownloadHandler dlHandler;
    public DownloadAPIController(IDownloadHandler dlh)
    {
        dlHandler = dlh;
    }

    ///////////////////////
    //   Request types   //
    ///////////////////////
    [Consumes("application/json")]
    [Produces("application/json")]
    [HttpPost("{version}/{type}")]
    public ActionResult<string> Post(string version, string type, [FromBody] APIDownloadRequestViewModel model)
    {
        if (type.ToLower() != "video" && type.ToLower() != "audio")
        {
            return InvalidType();
        }

        if (version == "v1.0")
        {
            return Version1(type, model);
        }
        else
        {
            return StatusCode(StatusCodes.Status400BadRequest, "Wrong or Deprecated API Version.");
        }
    }

    //////////////////////////////
    //   Various API Versions   //
    //////////////////////////////

    [Produces("application/json")]
    private ActionResult<string> Version1(string type, APIDownloadRequestViewModel model)
    {
        bool isVideo = type.ToLower() == "video";

        try
        {
            string filePath;
            RequestStatus status;

            bool success = dlHandler.Download(isVideo, model.DownloadURL, model.CallSource, out filePath, out status);

            string responseURL = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}{filePath}"; // current domain + relative path.
            return new JsonResult(new Data.DownloadResponse(success, responseURL, RequestStatusJSONResponse.Response[status]));
        }
        catch (Exception)
        {
            return InternalServerError();
        }
    }



    //////////////////////////////////////////////////////
    //   Canned Status Codes for various errors below   //
    //////////////////////////////////////////////////////
    private ObjectResult InvalidType()
    {
        return StatusCode(StatusCodes.Status400BadRequest, "Invalid Type. Allowed Types are: [video] and [audio].");
    }
    private ObjectResult InternalServerError()
    {
        return StatusCode(StatusCodes.Status500InternalServerError, "Generic Internal Server Error.");
    }
}