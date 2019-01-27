using System;
using System.IO;

namespace BeatSaberOnline.Data
{
    static class Logger
    {
        private static string loggerName = Plugin.instance.Name;
        private static FileInfo FileLocation { get; } = new FileInfo($"UserData/Logs/{loggerName.ToLower()}.txt");
        private static StreamWriter logWriter;

        public static void Init()
        {
            FileLocation?.Directory?.Create();
            logWriter = new StreamWriter(FileLocation.FullName) { AutoFlush = true };
        }

        public static void Debug(object message)
        {
            if (Config.Instance.DebugMode) {
                Console.ForegroundColor = ConsoleColor.Gray;
                Write("Debug", message);
            }
        }
        public static void Info(object message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Write("Info", message);
        }

        public static void Warning(object message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Write("Warning", message);
        }

        public static void Error(object message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Write("Error", message);
        }

        private static void Write(string type, object message)
        {
            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [{loggerName}] [{type}] ${message}");
            logWriter.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [{type}] ${message}");
        }

    }
}
