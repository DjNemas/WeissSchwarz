using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeißSchwarzCardResolutionFixer
{
    internal class ImageDetails
    {
        public string filePath { get; set; }
        public string? downloadLink { get; set; }

        public ImageDetails(string path, string? downloadLink = null)
        {
            this.filePath = path;
            this.downloadLink = downloadLink;
        }
    }
}
