using System;
using LSLib.LS.Enums;

namespace Divine.CLI
{
    internal static class CommandLineLogger
    {
        private static readonly LogLevel LogLevelOption = CommandLineActions.LogLevel;

        public static void LogFatal(string message, int errorCode) => Log(LogLevel.FATAL, message, errorCode);

        public static void LogError(string message) => Log(LogLevel.ERROR, message);

        public static void LogWarn(string message) => Log(LogLevel.WARN, message);

        public static void LogInfo(string message) => Log(LogLevel.INFO, message);

        public static void LogDebug(string message) => Log(LogLevel.DEBUG, message);

        public static void LogTrace(string message) => Log(LogLevel.TRACE, message);

        public static void LogAll(string message) => Log(LogLevel.ALL, message);

        private static void Log(LogLevel logLevel, string message, int errorCode = -1)
        {
            if (LogLevelOption == LogLevel.OFF && logLevel != LogLevel.FATAL)
            {
                return;
            }

            switch (logLevel)
            {
                case LogLevel.FATAL:
                    if (LogLevelOption > LogLevel.OFF)
                    {
                        Console.WriteLine($"[FATAL] {message}");
                    }

                    switch (errorCode)
                    {
                        case -1:
                            Environment.Exit((int)LogLevel.FATAL);
                            break;
                        default:
                            Environment.Exit((int)LogLevel.FATAL + errorCode);
                            break;
                    }

                    break;

                case LogLevel.ERROR:
                    if (LogLevelOption < logLevel)
                    {
                        break;
                    }

                    Console.WriteLine($"[ERROR] {message}");
                    break;

                case LogLevel.WARN:
                    if (LogLevelOption < logLevel)
                    {
                        break;
                    }

                    Console.WriteLine($"[WARN] {message}");
                    break;

                case LogLevel.INFO:
                    if (LogLevelOption < logLevel)
                    {
                        break;
                    }

                    Console.WriteLine($"[INFO] {message}");
                    break;

                case LogLevel.DEBUG:
                    if (LogLevelOption < logLevel)
                    {
                        break;
                    }

                    Console.WriteLine($"[DEBUG] {message}");
                    break;

                case LogLevel.TRACE:
                    if (LogLevelOption < logLevel)
                    {
                        break;
                    }

                    Console.WriteLine($"[TRACE] {message}");
                    break;
            }
        }
    }
}
