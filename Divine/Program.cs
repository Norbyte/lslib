using System;
using System.IO;
using System.Linq;
using Divine.CLI;

namespace Divine
{
    internal class Program
    {
        // ReSharper disable once InconsistentNaming
        public static CommandLineArguments argv;

        private static void Main(string[] args)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            DivineCommandLineParser parser = new DivineCommandLineParser
            {
                IgnoreCase = true,
                ShowUsageHeader = "Divine <https://github.com/Norbyte/lslib>"
            };

            argv = new CommandLineArguments();

            parser.ExtractArgumentAttributes(argv);
            
            string[] helpArgs =
            {
                @"-h",
                @"--help",
                @"help",
                @"-?",
                @"/?"
            };

            if (args.Length == 0 || helpArgs.Any(args.Contains))
            {
                parser.PrintUsage(Console.Out);
                return;
            }

            if (args.Length == 1)
            {
                string path = args[0];
                
                if (PathUtils.IsDir(path))
                {
                    args = new[]
                    {
#if DEBUG
                        "-l", "all",
#endif
                        "-a", Constants.EXTRACT_PACKAGES,
                        "-s", $"{path}",
                        "-d", $"{path}",
                        "--use-package-name"
                    };
                }
                else if (PathUtils.IsFile(path))
                {
                    args = new[]
                    {
#if DEBUG
                        "-l", "all",
#endif
                        "-a", Constants.EXTRACT_PACKAGE,
                        "-s", $"{path}",
                        "-d", $"{Path.GetDirectoryName(path)}",
                        "--use-package-name"
                    };
                }
            }

#if !DEBUG
            try
            {
#endif
                parser.ParseCommandLine(args);
#if !DEBUG
            }
            catch (Exception e)
            {
                Console.WriteLine($"[FATAL] {e.Message}");
            }
#endif

            if (parser.ParsingSucceeded)
            {
                CommandLineActions.Run(argv);
            }
        }
    }
}
