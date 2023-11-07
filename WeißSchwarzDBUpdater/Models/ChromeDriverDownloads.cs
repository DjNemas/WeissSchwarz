using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WeißSchwarzDBUpdater.Models
{
    internal class ChromeDriverDownloads
    {
        [JsonPropertyName("chrome")]
        public ChromeDriverDetails[] Chrome { get; set; }

        [JsonPropertyName("chromedriver")]
        public ChromeDriverDetails[] Chromedriver { get; set; }

        [JsonPropertyName("chrome-headless-shell")]
        public ChromeDriverDetails[] ChromeHeadlessShell { get; set; }
    }
}
