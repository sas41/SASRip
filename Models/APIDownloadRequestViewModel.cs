using System.ComponentModel.DataAnnotations;

namespace SASRip.Models;

// API Request Model
public class APIDownloadRequestViewModel
{
    [Url]
    [Required]
    public string DownloadURL { get; set; }
    public string Type { get; set; }
    public string CallSource { get; set; }
}
