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
        private static readonly string chromeDriverPath = Path.Combine(Environment.CurrentDirectory, "chromedriver", "chromedriver.exe");
        private static readonly string chromeExePath = "C:\\Program Files\\Google\\Chrome Beta\\Application\\chrome.exe";
        static void Main(string[] args)
        {
            ChromeDriverUpdater updater = new(chromeExePath, chromeDriverPath);
            updater.CheckUpdate();

            // Init DB
            if (ConnectDB())
            {
                Log.Debug("DB Connected");
                // Start Collect
                collector = new(chromeDriverPath, chromeExePath, true);
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
