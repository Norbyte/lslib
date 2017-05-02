using LSLib.LS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ConverterApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            // ResourceUtils.LoadResource(@"C:\Dev\DOS\extracttest\Public\Game\Assets\Materials\Clouds\RS3_Cloud_Blood.lsb");
            // ResourceUtils.LoadResource(@"C:\Dev\DOS\extracttest\Public\Shared\Assets\Materials\Terrain_PBR\TR_BloodForest_PBR.lsb");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
