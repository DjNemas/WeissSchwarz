using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WeißSchwarzDBUpdater.Models
{
    internal class ChromeDriverResponse
    {
        [JsonPropertyName("versions")]
        public ChromeDriverVersions[] Versions { get; set; }
    }
}
