using SASRip.Helpers;

namespace SASRip.Interfaces;

public interface ILogger
{
    public void Log(string hash, string url, string requestSource, bool isVideo, RequestStatus status);
    public void LogError(string hash, string url, string requestSource, bool isVideo, string error);
}
