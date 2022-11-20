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
using System.Windows.Media;
using static System.Net.WebRequestMethods;

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

        private static List<ImageDetails> imageList = new();

        private readonly string chromeDriverFolder = "./driver/";
        private readonly string chromeDriverFile = "chromedriver.exe";
        private string? chromePath;

        private ChromeDriver driver;
        private readonly bool headless = true;
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
                
                if(System.IO.File.Exists(zipPath))
                    System.IO.File.Delete(zipPath);
                using (FileStream fs = new(zipPath, FileMode.CreateNew))
                {
                    data.CopyTo(fs);
                }
            }
            ZipArchive za = ZipFile.Open(zipPath, ZipArchiveMode.Read);
            za.ExtractToDirectory(chromeDriverFolder, true);
            za.Dispose();

            System.IO.File.Delete(zipPath);
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

        private void btnStartFix_Click(object sender, RoutedEventArgs e)
        {
            btnStartFix.IsEnabled = false;
            btnSelectDir.IsEnabled = false;
            Task.Factory.StartNew(WorkAsync);
            Console.WriteLine("Done");
        }

        private async Task WorkAsync()
        {
            Application.Current.Dispatcher.Invoke(() => {
                pbProgress.Value = 0;
                lblStatus.Foreground = Brushes.Aqua;
                lblStatus.Content = "Process Started...";
            });

            driver.Navigate().GoToUrl(apiURL);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
            await UploadImages();
            await FixCards();

            Application.Current.Dispatcher.Invoke(() =>
            {
                lblCardsFound.Foreground = Brushes.Black;
                lblCardsFound.Content = "Cards selected: 0";
                btnStartFix.IsEnabled = false;
                btnSelectDir.IsEnabled = true;
                lblStatus.Foreground = Brushes.LightGreen;
                lblStatus.Content = "Done";
            });
        }
        private Task FixCards()
        {
            IWebElement divFiles = driver.FindElement(By.Id("files"));
            Console.WriteLine();

            int cardsToFix = divFiles.FindElements(By.XPath("./div")).Count;
            int indexCounter = 0;

            foreach(var div in divFiles.FindElements(By.XPath("./div")))
            {
                Application.Current.Dispatcher.Invoke(() => {
                    pbProgress.Value = (int)(100f / (float)cardsToFix * ((float)indexCounter + 1f));
                    lblStatus.Foreground = Brushes.Aqua;
                    lblStatus.Content = $"Fix Card {(indexCounter + 1)}...";
                });

                div.FindElement(By.XPath("./div[2]/button[1]")).Click();
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(200);
                driver.FindElement(By.Id("big_ok")).Click();
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(200);

                WaitForDownloadLink(indexCounter++, div);
            }
            return Task.CompletedTask;
        }

        private void WaitForDownloadLink(int index, IWebElement element)
        {
            while (true)
            {
                string downloadLink = element.FindElement(By.XPath("./div[2]/a[1]")).GetAttribute("href");
                if (string.IsNullOrEmpty(downloadLink))
                {
                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                    continue;
                }
                imageList[index].downloadLink = downloadLink;
                break;
            }

            HttpClient client = new HttpClient();
            using (Stream data = client.GetStreamAsync(imageList[index].downloadLink).Result)
            {
                FileInfo img = new FileInfo(imageList[index].filePath);

                string fixDirectory = Path.Combine(img.DirectoryName, "FixedCards");
                if(!Directory.Exists(fixDirectory))
                    Directory.CreateDirectory(fixDirectory);

                if (System.IO.File.Exists(Path.Combine(fixDirectory, img.Name)))
                    System.IO.File.Delete(Path.Combine(fixDirectory, img.Name));

                using (FileStream fs = new(Path.Combine(fixDirectory, img.Name), FileMode.CreateNew))
                {
                    data.CopyTo(fs);
                }
            }
        }

        private Task UploadImages()
        {
            float counter = 1;
            Application.Current.Dispatcher.Invoke(() =>
            {
                lblStatus.Foreground = Brushes.Aqua;
                lblStatus.Content = "Uploading Images...";
            });
            
            foreach (var image in imageList)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    pbProgress.Value = (int)(100f / imageList.Count * counter++)
                    );

                driver.FindElement(By.Id("fileupload")).SendKeys(image.filePath);
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(500);
                
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                lblStatus.Foreground = Brushes.LightGreen;
                lblStatus.Content = "Done";
            });
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

                imageList.Clear();

                int countImageFiles = 0;
                foreach (var file in listFiles)
                {
                    if (file.Extension == ".jpg")
                    {
                        countImageFiles++;
                        imageList.Add(new(file.FullName));
                    }
                }
                if(countImageFiles < 0)
                {
                    MessageBox.Show("No Images found.");
                    return;
                }
                lblCardsFound.Content = $"Cards selected: " + countImageFiles;
                lblCardsFound.Foreground = Brushes.Aqua;
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
