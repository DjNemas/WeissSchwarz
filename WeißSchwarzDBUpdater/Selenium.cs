using Microsoft.EntityFrameworkCore.Metadata;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeißSchwarzDBUpdater
{
    internal class Selenium
    {
        private readonly string _chromePath;
        private readonly string _chromeDriverPath;
        private bool _headless;

        public ChromeDriver Driver { get; }
        private ChromeOptions _options;

        public Selenium(string chromeDriverPath, string chromePath, bool headless)
        {
            _chromeDriverPath = chromeDriverPath;
            _chromePath = chromePath;
            _headless = headless;

            _options = new ChromeOptions();
            _options.BinaryLocation = _chromePath;

            if (_headless)
            {
                _options.AddArguments(new List<string>() { "--headless", "--disable-gpu", "--disable-logging", "--log-level=3" });
                //this.options.AddArguments(new List<string>() { "headless" });
            }

            Driver = new ChromeDriver(_chromeDriverPath, _options);
        }
    }
}
