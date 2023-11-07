using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WeißSchwarzDBUpdater.Models
{
    internal class ChromeDriverVersions
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("downloads")]
        public ChromeDriverDownloads Downloads { get; set; }
    }
}
