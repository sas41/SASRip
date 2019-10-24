using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SASRip.Services
{
    public static class LocalMediaCacheService
    {
        public static Data.IMediaCache MediaCache { get; private set; }

        static LocalMediaCacheService()
        {
            MediaCache = new Data.MediaCache();
        }
    }
}
