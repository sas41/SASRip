using SASRip.Helpers;

namespace SASRip.Interfaces;

public interface IDownloadHandler
{
    public bool Download(bool isVideo, string downloadURL, string callSource, out string pathOnDisk, out RequestStatus status);
}
