using System.Collections.Generic;

namespace SASRip.Helpers
{
    public enum RequestStatus
    {
        Started,
        Processing,
        Ready,
        CacheHit,
        Failed
    }

    static class RequestStatusJSONResponse
    {
        public static readonly Dictionary<RequestStatus, string> Response = new Dictionary<RequestStatus, string>
        {
            { RequestStatus.Started, "download_started" },
            { RequestStatus.Processing, "file_processing" },
            { RequestStatus.Ready, "file_ready" },
            { RequestStatus.CacheHit, "file_cached" },
            { RequestStatus.Failed, "file_not_found" }
        };
    }
}
