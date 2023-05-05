using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml;
using WeißSchwarzSharedClasses;

namespace WeißSchwarzDBUpdater
{
    public class ChromeDriverUpdater
    {
        private readonly static string _chromeDriverInfoTxtFilePath = Path.Combine(Environment.CurrentDirectory, "chromedriver", "version.txt");
        

        private readonly string _chromePath;
        private readonly string _chromeDriverPath;
        private string _chromeVersion;
        private string _chromeDriverVersion;
        private bool foundNewVersion = false;
        

        public ChromeDriverUpdater(string chromeExePath, string chromeDriverFullPath)
        {
            _chromePath = chromeExePath;
            _chromeDriverPath = chromeDriverFullPath;
            FileInfo fileInfo = new FileInfo(chromeDriverFullPath);
            if(!Directory.Exists(fileInfo.DirectoryName))
                Directory.CreateDirectory(fileInfo.DirectoryName);
            if (!File.Exists(_chromeDriverInfoTxtFilePath))
            {
                File.WriteAllText(_chromeDriverInfoTxtFilePath, "0.0.0.0");
            }
        }
        public void CheckUpdate()
        {
            GetChromeExeVersion();
            GetChromeDriverVersion();
            if (_chromeVersion != _chromeDriverVersion)
            {
                Log.Info("New ChromeDriver Version Available.", true);
                DownloadNewVersion();
            }
                
        }

        private void GetChromeExeVersion()
        {
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(_chromePath);
            _chromeVersion = versionInfo.FileVersion;
        }
        private void GetChromeDriverVersion()
        {
            _chromeDriverVersion = File.ReadAllText(_chromeDriverInfoTxtFilePath);
        }

        private async void DownloadNewVersion()
        {
            Log.Info("Start Download", true);
            if (File.Exists(_chromeDriverPath))
                File.Delete(_chromeDriverPath);

            using (HttpClient client = new())
            {
                //string versionCut = _chromeVersion.Substring(0, _chromeVersion.LastIndexOf("."));
                StringContent content = new("application/xml");
                string xml = await client.GetStringAsync($"https://chromedriver.storage.googleapis.com/?delimiter=/&prefix={_chromeVersion}/");

                string windowsVersionKey = GetWindowsVersionKey(xml);

                byte[] dataZip = await client.GetByteArrayAsync($"https://chromedriver.storage.googleapis.com/{windowsVersionKey}");

                FileInfo dir = new FileInfo(_chromeDriverPath);
                string zipPath = Path.Combine(dir.DirectoryName, "chromedriver.zip");
                File.WriteAllBytes(zipPath, dataZip);

                ZipFile.ExtractToDirectory(zipPath, dir.DirectoryName, true);
                File.Delete(zipPath);

                await Console.Out.WriteLineAsync();
                
            }
            File.WriteAllText(_chromeDriverInfoTxtFilePath, _chromeVersion);
            Log.Info("Download Done", true);
        }

        private string GetWindowsVersionKey(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNodeList keys = doc.GetElementsByTagName("Key");
            foreach (XmlNode keyNode in keys)
            {
                if(keyNode.InnerText.Contains("win32.zip"))
                    return keyNode.InnerText;
            }
            return string.Empty;
        }
    }
}
