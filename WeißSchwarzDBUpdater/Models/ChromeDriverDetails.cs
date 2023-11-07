using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WeißSchwarzDBUpdater.Models
{
    internal class ChromeDriverDetails
    {
        [JsonIgnore]
        public Platform Platform { get; set; }

        [JsonPropertyName("platform")]
        public string PlatformString
        {
            set
            {
                switch (value)
                {
                    case "linux64":
                        {
                            Platform = Platform.Linux64;
                            break;
                        }
                    case "mac-arm64":
                        {
                            Platform = Platform.MacArm64;
                            break;
                        }
                    case "mac-x64":
                        {
                            Platform = Platform.MacX64;
                            break;
                        }
                    case "win32":
                        {
                            Platform = Platform.Win32;
                            break;
                        }
                    case "win64":
                        {
                            Platform = Platform.Win64;
                            break;
                        }
                    default:
                        {
                            Platform = Platform.Undefined;
                            break;
                        }
                }
            }
        }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    internal enum Platform
    {
        Linux64,
        MacArm64,
        MacX64,
        Win32,
        Win64,
        Undefined
    }
}
