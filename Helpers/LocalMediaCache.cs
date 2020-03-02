using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SASRip.Interfaces;

namespace SASRip.Services
{
    public class LocalMediaCache
    {
        public static IMediaCache MediaCache { get; private set; }

        public LocalMediaCache(IMediaCache mc)
        {
            MediaCache = mc;
        }
    }
}
