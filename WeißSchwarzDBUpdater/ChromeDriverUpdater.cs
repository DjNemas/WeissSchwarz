using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using WeißSchwarzDBUpdater.Models;
using WeißSchwarzSharedClasses;

namespace WeißSchwarzDBUpdater
{
    public class ChromeDriverUpdater
    {
        private readonly static string _chromeDriverInfoTxtFilePath = Path.Combine(Environment.CurrentDirectory, "chromedriver", "version.txt");

        private readonly FileInfo _chromeExePath;
        private readonly DirectoryInfo _chromeDriverDir;
        private readonly FileInfo _chromeDriverFile;
        private string _chromeVersion;
        private string _chromeDriverVersion;        

        public ChromeDriverUpdater(FileInfo chromeExePath, DirectoryInfo chromeDriverDir)
        {
            _chromeExePath = chromeExePath;
            _chromeDriverDir = chromeDriverDir;
            _chromeDriverFile = new FileInfo(Path.Combine(chromeDriverDir.FullName, "chromedriver.exe"));

            if (!chromeDriverDir.Exists)
                Directory.CreateDirectory(chromeDriverDir.FullName);
            if (!File.Exists(_chromeDriverInfoTxtFilePath))
            {
                File.WriteAllText(_chromeDriverInfoTxtFilePath, "0.0.0.0");
            }
        }
        public async Task CheckUpdate()
        {
            GetChromeExeVersion();
            GetChromeDriverVersion();
            if (_chromeVersion != _chromeDriverVersion)
            {
                Log.Info("New ChromeDriver Version Available.", true);
                await DownloadNewVersion();
            }
                
        }

        private void GetChromeExeVersion()
        {
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(_chromeExePath.FullName);
            _chromeVersion = versionInfo.FileVersion;
        }
        private void GetChromeDriverVersion()
        {
            _chromeDriverVersion = File.ReadAllText(_chromeDriverInfoTxtFilePath);
        }

        private async Task DownloadNewVersion()
        {
            Log.Info("Start Download", true);
            if (_chromeDriverFile.Exists)
                _chromeDriverFile.Delete();

            using (HttpClient client = new())
            {
                string jsonString = await client.GetStringAsync($"https://googlechromelabs.github.io/chrome-for-testing/known-good-versions-with-downloads.json");
                ChromeDriverResponse response = JsonSerializer.Deserialize<ChromeDriverResponse>(jsonString);

                string downloadURL = response.Versions
                    .FirstOrDefault(x => x.Version == _chromeVersion && x.Downloads.Chromedriver is not null).Downloads.Chromedriver
                    .FirstOrDefault(x => x.Platform == Platform.Win64).Url;

                byte[] dataZip = await client.GetByteArrayAsync(downloadURL);

                string zipPath = Path.Combine(_chromeDriverDir.FullName, "chromedriver.zip");
                File.WriteAllBytes(zipPath, dataZip);

                ZipFile.ExtractToDirectory(zipPath, _chromeDriverDir.FullName, true);
                FileInfo chromeDriver = _chromeDriverDir.GetFiles("chromedriver.exe", SearchOption.AllDirectories).FirstOrDefault();

                File.Copy(chromeDriver.FullName, _chromeDriverFile.FullName);

                Directory.Delete(chromeDriver.Directory.FullName, true);
                File.Delete(zipPath);

                await Console.Out.WriteLineAsync();

            }
            File.WriteAllText(_chromeDriverInfoTxtFilePath, _chromeVersion);
            Log.Info("Download Done", true);
        }
    }
}
