using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SASRip.Helpers
{
    public class CacheInfo
    {
        public DateTime TimeOfCreation { get; set; }
        public string Status { get; set; }

        public CacheInfo(string status, DateTime toc)
        {
            Status = status;
            TimeOfCreation = toc;
        }
    }
}
