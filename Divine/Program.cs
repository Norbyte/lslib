using System;
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
                ShowUsageHeader = "Divine - by Norbyte & fireundubh <https://github.com/Norbyte/lslib>"
            };

            argv = new CommandLineArguments();

            parser.ExtractArgumentAttributes(argv);

            string[] helpArgs =
            {
                @"-h",
                @"--help",
                @"-?",
                @"/?"
            };

            if (args.Length == 0 || helpArgs.Any(args.Contains))
            {
                parser.PrintUsage(Console.Out);
                return;
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
