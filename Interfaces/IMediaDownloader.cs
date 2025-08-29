namespace SASRip.Interfaces;

public interface IMediaDownloader
{
    string DownloadVideo(string url, string hash);
    string DownloadAudio(string url, string hash);
}
