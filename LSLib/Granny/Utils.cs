namespace LSLib.Granny
{
    abstract class Utils
    {
        public static void Warn(string message)
        {
            System.Console.WriteLine("WARNING: " + message);
        }

        public static void Info(string message)
        {
            System.Console.WriteLine(message);
        }
    }
}
