using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SASRip.Data
{
    public static class AppConfig
    {
        public static IConfiguration Configuration { get; set; }
    }
}
