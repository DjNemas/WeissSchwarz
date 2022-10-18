using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Path = System.IO.Path;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager;
using System.Net.Http;
using System.Net;
using System.Diagnostics;
using OpenQA.Selenium.Chrome;
using System.IO.Compression;
using OpenQA.Selenium;
using System.Configuration;
using OpenQA.Selenium.DevTools.V104.Browser;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;

namespace WeißSchwarzCardResolutionFixer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
#if DEBUG
        [DllImport("Kernel32")]
        private static extern void AllocConsole();
#endif

        private static List<string> imageList = new();

        private readonly string chromeDriverFolder = "./driver/";
        private readonly string chromeDriverFile = "chromedriver.exe";
        private string? chromePath;

        private ChromeDriver driver;
        private readonly bool headless = false;
        private readonly Uri apiURL = new Uri("https://bigjpg.com/");


        public MainWindow()
        {
#if DEBUG
            AllocConsole();
#endif
            InitializeComponent();

            InitChromeDriver();
        }

        private void InitChromeDriver()
        {
            CheckChromeInstalled();
            UpdateChromeDriver();
            CreateChromeDriver();
        }

        private void CheckChromeInstalled()
        {
            chromePath = (string?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe", "", null);

            if (chromePath == null)
            {
                MessageBox.Show("Chrome has to be installed on this Maschine.\n" +
                    "Please Visit https://www.google.de/intl/de/chrome/ for more information and download.");
                Application.Current.Shutdown();
            }

#pragma warning disable CS8604 // Mögliches Nullverweisargument.
            chromePath += Path.Combine(chromePath, "chrome.exe");
#pragma warning restore CS8604 // Mögliches Nullverweisargument.
        }

        private void UpdateChromeDriver()
        {
            if (!Directory.Exists(chromeDriverFolder))
                Directory.CreateDirectory(chromeDriverFolder);

            ChromeConfig cConfig = new();
            string browserVersionBinary = cConfig.GetUrl64();
            browserVersionBinary = browserVersionBinary.Replace("<version>", cConfig.GetMatchingBrowserVersion());

            HttpClient httpClient = new();
            string zipPath = Path.Combine(chromeDriverFolder, "chromedriver.zip");
            using (Stream data = httpClient.GetStreamAsync(browserVersionBinary).Result)
            {
                
                if(File.Exists(zipPath))
                    File.Delete(zipPath);
                using (FileStream fs = new(zipPath, FileMode.CreateNew))
                {
                    data.CopyTo(fs);
                }
            }
            ZipArchive za = ZipFile.Open(zipPath, ZipArchiveMode.Read);
            za.ExtractToDirectory(chromeDriverFolder, true);
            za.Dispose();

            File.Delete(zipPath);
        }

        private void CreateChromeDriver()
        {
            ChromeDriverService driverService = ChromeDriverService.CreateDefaultService(chromeDriverFolder);
            driverService.HideCommandPromptWindow = true;

            ChromeOptions options = new();
            if (headless)
                options.AddArgument("headless");

            driver = new ChromeDriver(driverService, options);
        }

        private async void btnStartFix_Click(object sender, RoutedEventArgs e)
        {
            float counter = 1;

            pbProgress.Value = 0;

            driver.Navigate().GoToUrl(apiURL);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
            await UploadImages();

            Console.WriteLine("Done");
        }

        private Task UploadImages()
        {
            float counter = 1;
            foreach (var imagePath in imageList)
            {
                pbProgress.Value = (int)(100f / imageList.Count * counter++);

                driver.FindElement(By.Id("fileupload")).SendKeys(imagePath);
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(500);

            }
            return Task.CompletedTask;
        }

        private void btnSelectDir_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                DirectoryInfo dirInfo = new(Path.GetFullPath(dialog.FileName));
                List<FileInfo> listFiles = dirInfo.EnumerateFiles().ToList();

                int countImageFiles = 0;
                foreach (var file in listFiles)
                {
                    if (file.Extension == ".jpg")
                    {
                        countImageFiles++;
                        imageList.Add(file.FullName);
                        Console.WriteLine(file.FullName);
                    }
                }
                if(countImageFiles < 0)
                {
                    MessageBox.Show("No Images found.");
                    return;
                }
                lblCardsFound.Content = $"Cards Found: " + countImageFiles;
                lblCardsFound.Foreground = System.Windows.Media.Brushes.Aqua;
                btnStartFix.IsEnabled = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (driver != null)
            {
                driver.Close();
            }
            foreach (Process p in Process.GetProcessesByName("chromedriver"))
                p.Kill();
            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill();
        }
    }
}
