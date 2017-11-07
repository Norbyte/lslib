using System;
using Divine.Enums;

namespace Divine.CLI
{
    internal class CommandLineLogger
    {
        private static readonly LogLevel optionLogLevel = CommandLineActions.LogLevel;

        public static void LogInfo(string message) => Log(LogLevel.INFO, message);
        public static void LogWarn(string message) => Log(LogLevel.WARN, message);
        public static void LogError(string message) => Log(LogLevel.ERROR, message);
        public static void LogFatal(string message) => Log(LogLevel.FATAL, message);
        public static void LogDebug(string message) => Log(LogLevel.DEBUG, message);

        private static void Log(LogLevel loglevel, string message)
        {
            if (optionLogLevel == LogLevel.SILENT) return;

            switch (loglevel)
            {
                case LogLevel.INFO:
                    if (optionLogLevel < LogLevel.INFO) break;
                    Console.WriteLine($"[INFO] {message}");
                    break;

                case LogLevel.WARN:
                    if (optionLogLevel < LogLevel.WARN) break;
                    Console.WriteLine($"[WARN] {message}");
                    break;

                case LogLevel.ERROR:
                    if (optionLogLevel < LogLevel.ERROR) break;
                    Console.WriteLine($"[ERROR] {message}");
                    break;

                case LogLevel.FATAL:
                    if (optionLogLevel > LogLevel.SILENT)
                    {
                        Console.WriteLine($"[FATAL] {message}");
                    }
                    Environment.Exit((int) LogLevel.FATAL);
                    break;

                case LogLevel.DEBUG:
                    if (optionLogLevel < LogLevel.DEBUG) break;
                    Console.WriteLine($"[DEBUG] {message}");
                    break;
            }
        }
    }
}
