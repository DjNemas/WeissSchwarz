using System;
using System.IO;

namespace WeißSchwarzSharedClasses
{
    public class Log
    {
        private static readonly string formatDate = "dd.MM.yyyy HH:mm";
        private static readonly string logDir = "./log";
        private static readonly string logFile = "/log.txt";

        private static object logLock = new object();

        private static readonly bool debugMode = true; // Set true if you want to see Debug Console Output 
        public static void Debug(string msg, bool toFile = false)
        {
            lock (logLock)
            {
                if (!debugMode)
                    return;
                string msgString = DateTime.Now.ToString(formatDate) + " [DEBUG] " + msg;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(msgString);
                Console.ResetColor();
                LogToFile(msgString, toFile);
            }
        }

        public static void Error(string msg, bool toFile = false)
        {
            lock (logLock)
            {
                string msgString = DateTime.Now.ToString(formatDate) + " [ERROR] " + msg;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msgString);
                Console.ResetColor();
                LogToFile(msgString, toFile);
            }
            
        }

        public static void Info(string msg, bool toFile = false)
        {
            lock (logLock)
            {
                string msgString = DateTime.Now.ToString(formatDate) + " [INFO] " + msg;
                Console.WriteLine(msgString);
                LogToFile(msgString, toFile);
            }
        }

        private static void LogToFile(string msg, bool toFile)
        {
            lock (logLock)
            {
                if (toFile)
                {
                    if (!Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);
                    File.AppendAllText(logDir + logFile, msg + "\n");
                }
            }
        }
    }
}
