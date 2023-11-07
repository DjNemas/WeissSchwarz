using System;
using System.Configuration;
using System.IO;
using WeißSchwarzSharedClasses;
using WeißSchwarzSharedClasses.DB;
using Microsoft.Extensions.Configuration;

namespace WeißSchwarzDBUpdater
{
    internal class Program
    {
        internal static WSContext db;
        private static WSDataCollector collector;
        private static readonly DirectoryInfo chromeDriverFolder = new(Path.Combine(Environment.CurrentDirectory, "chromedriver"));
        private static readonly FileInfo chromeExePath = new(@"C:\Program Files\Google\Chrome Beta\Application\chrome.exe");
        static void Main(string[] args)
        {
            ChromeDriverUpdater updater = new(chromeExePath, chromeDriverFolder);
            updater.CheckUpdate().Wait();

            // Init DB
            if (ConnectDB())
            {
                Log.Debug("DB Connected");
                // Start Collect
                collector = new(chromeDriverFolder.FullName, chromeExePath.FullName, true);
                collector.StartCollect();
            }            
            Console.ReadKey();
        }

        private static bool ConnectDB()
        {
            bool connected = false;
            db = new();
            try
            {
                db.Database.EnsureCreated();
                connected = true;
            }
            catch (Exception e)
            {
                Log.Error("Error while Connecting to DB\n" + e);
            }
            return connected;
        }
    }
}
