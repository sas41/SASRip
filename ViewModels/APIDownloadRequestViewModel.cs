using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SASRip.ViewModels
{
    public class APIDownloadRequestViewModel
    {
        [Url]
        [Required]
        public string DownloadURL { get; set; }
        public string Type { get; set; }
        public string CallSource { get; set; }
    }
}
