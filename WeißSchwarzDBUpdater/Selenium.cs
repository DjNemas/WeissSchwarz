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
        private readonly string binaryPath = @"C:\Program Files\Google\Chrome Beta\Application\chrome.exe";
        private readonly string chromePath;
        private bool headless;

        public ChromeDriver driver { get; }
        private ChromeOptions options;

        public Selenium(string chromePath, bool headless)
        {
            this.chromePath = chromePath;
            this.headless = headless;

            this.options = new ChromeOptions();

            if (this.headless)
            {
                this.options.BinaryLocation = binaryPath;
                this.options.AddArguments(new List<string>() { "headless", "disable-gpu" });
                //this.options.AddArguments(new List<string>() { "headless" });
            }

            this.driver = new ChromeDriver(this.chromePath, options);
        }

        public Selenium(ChromeDriverService service, bool headless)
        {
            this.options = new ChromeOptions();
            this.headless = headless;

            if (this.headless)
            {
                this.options.BinaryLocation = binaryPath;
                this.options.AddArguments(new List<string>() { "headless", "disable-gpu" });
                //this.options.AddArguments(new List<string>() { "headless" });
            }
            this.driver = new ChromeDriver(service, options);
        }
    }
}
