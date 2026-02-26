using SASRip.Interfaces;

namespace SASRip.Helpers;

public class LocalMediaCache
{
    public static IMediaCache MediaCache { get; private set; }

    public LocalMediaCache(IMediaCache mc)
    {
        MediaCache = mc;
    }
}
