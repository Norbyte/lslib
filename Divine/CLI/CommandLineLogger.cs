using System;
using Divine.Enums;

namespace Divine.CLI
{
    internal class CommandLineLogger
    {
        private static readonly LogLevel OptionLogLevel = CommandLineActions.LogLevel;

        public static void LogFatal(string message, int errorCode) => Log(LogLevel.FATAL, message, errorCode);
        public static void LogError(string message) => Log(LogLevel.ERROR, message);
        public static void LogWarn(string message) => Log(LogLevel.WARN, message);
        public static void LogInfo(string message) => Log(LogLevel.INFO, message);
        public static void LogDebug(string message) => Log(LogLevel.DEBUG, message);
        public static void LogTrace(string message) => Log(LogLevel.TRACE, message);
        public static void LogAll(string message) => Log(LogLevel.ALL, message);

        private static void Log(LogLevel loglevel, string message, int errorCode = -1)
        {
            if (OptionLogLevel == LogLevel.OFF)
            {
                return;
            }

            switch (loglevel)
            {
                case LogLevel.FATAL:
                    if (OptionLogLevel > LogLevel.OFF)
                    {
                        Console.WriteLine($"[FATAL] {message}");
                    }

                    if (errorCode == -1)
                    {
                        Environment.Exit((int) LogLevel.FATAL);
                    }
                    else
                    {
                        Environment.Exit((int) LogLevel.FATAL + errorCode);
                    }
                    break;

                case LogLevel.ERROR:
                    if (OptionLogLevel < LogLevel.ERROR)
                    {
                        break;
                    }
                    Console.WriteLine($"[ERROR] {message}");
                    break;

                case LogLevel.WARN:
                    if (OptionLogLevel < LogLevel.WARN)
                    {
                        break;
                    }
                    Console.WriteLine($"[WARN] {message}");
                    break;

                case LogLevel.INFO:
                    if (OptionLogLevel < LogLevel.INFO)
                    {
                        break;
                    }
                    Console.WriteLine($"[INFO] {message}");
                    break;

                case LogLevel.DEBUG:
                    if (OptionLogLevel < LogLevel.DEBUG)
                    {
                        break;
                    }
                    Console.WriteLine($"[DEBUG] {message}");
                    break;

                case LogLevel.TRACE:
                    if (OptionLogLevel < LogLevel.TRACE)
                    {
                        break;
                    }
                    Console.WriteLine($"[TRACE] {message}");
                    break;
            }
        }
    }
}
