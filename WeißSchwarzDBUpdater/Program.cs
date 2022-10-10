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
        private static readonly string chromeDriverPath = "G:\\binarys\\chromedriver";
        static void Main(string[] args)
        {
            // Init DB
            if (ConnectDB())
            {
                Log.Debug("DB Connected");
                // Start Collect
                collector = new(chromeDriverPath, true);
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
