using LSLib.Granny.Model;
using LSLib.LS.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ConverterApp
{
    public interface ISettingsDataSource
    {
        ConverterAppSettings Settings { get; set; }
    }

    public class SettingsBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ConverterAppSettings : SettingsBase
    {
        private GR2PaneSettings gr2;

        public GR2PaneSettings GR2
        {
            get { return gr2; }
            set { gr2 = value; }
        }

        private PackagePaneSettings pakSettings;

        public PackagePaneSettings PAK
        {
            get { return pakSettings; }
            set { pakSettings = value; }
        }

        private ResourcePaneSettings resourceSettings;

        public ResourcePaneSettings Resources
        {
            get { return resourceSettings; }
            set { resourceSettings = value; }
        }

        private OsirisPaneSettings storySettings;

        public OsirisPaneSettings Story
        {
            get { return storySettings; }
            set { storySettings = value; }
        }

        private DebugPaneSettings debugSettings;

        public DebugPaneSettings Debugging
        {
            get { return debugSettings; }
            set { debugSettings = value; }
        }

        private Game selectedGame = Game.DivinityOriginalSin2DE;

        public int SelectedGame
        {
            get { return (int)selectedGame; }
            set { selectedGame = (Game)value; OnPropertyChanged(); }
        }

        private string version = "";

        public string Version
        {
            get { return version; }
            set { version = value; }
        }

        public void SetPropertyChangedEvent(PropertyChangedEventHandler eventHandler)
        {
            this.PropertyChanged += eventHandler;
            GR2.PropertyChanged += eventHandler;
            PAK.PropertyChanged += eventHandler;
            Resources.PropertyChanged += eventHandler;
            Story.PropertyChanged += eventHandler;
        }

        public ConverterAppSettings()
        {
            GR2 = new GR2PaneSettings();
            PAK = new PackagePaneSettings();
            Resources = new ResourcePaneSettings();
            Story = new OsirisPaneSettings();
            Debugging = new DebugPaneSettings();
        }
    }

    public class GR2PaneSettings : SettingsBase
    {
        private string inputPath = "";

        public string InputPath
        {
            get { return inputPath; }
            set { inputPath = value; OnPropertyChanged(); }
        }

        private string outputPath = "";

        public string OutputPath
        {
            get { return outputPath; }
            set { outputPath = value; OnPropertyChanged(); }
        }

        private string batchInputPath = "";

        public string BatchInputPath
        {
            get { return batchInputPath; }
            set { batchInputPath = value; OnPropertyChanged(); }
        }

        private string batchOutputPath = "";

        public string BatchOutputPath
        {
            get { return batchOutputPath; }
            set { batchOutputPath = value; OnPropertyChanged(); }
        }

        private ExportFormat batchInputFormat = ExportFormat.GR2;

        public int BatchInputFormat
        {
            get { return (int)batchInputFormat; }
            set { batchInputFormat = (ExportFormat)value; OnPropertyChanged(); }
        }

        private ExportFormat batchOutputFormat = ExportFormat.DAE;

        public int BatchOutputFormat
        {
            get { return (int)batchOutputFormat; }
            set { batchOutputFormat = (ExportFormat)value; OnPropertyChanged(); }
        }

        private string conformPath;

        public string ConformPath
        {
            get { return conformPath; }
            set { conformPath = value; OnPropertyChanged(); }
        }

    }

    public class PackagePaneSettings : SettingsBase
    {
        private string extractInputPath = "";

        public string ExtractInputPath
        {
            get { return extractInputPath; }
            set { extractInputPath = value; OnPropertyChanged(); }
        }

        private string extractOutputPath = "";

        public string ExtractOutputPath
        {
            get { return extractOutputPath; }
            set { extractOutputPath = value; OnPropertyChanged(); }
        }

        private string createInputPath = "";

        public string CreateInputPath
        {
            get { return createInputPath; }
            set { createInputPath = value; OnPropertyChanged(); }
        }

        private string createOutputPath = "";

        public string CreateOutputPath
        {
            get { return createOutputPath; }
            set { createOutputPath = value; OnPropertyChanged(); }
        }

        private int createPackageVersion = 0;

        public int CreatePackageVersion
        {
            get { return createPackageVersion; }
            set { createPackageVersion = value; OnPropertyChanged(); }
        }

        private int createPackageCompression = 3;

        public int CreatePackageCompression
        {
            get { return createPackageCompression; }
            set { createPackageCompression = value; OnPropertyChanged(); }
        }

        //public string BatchInputPath { get; set; } = "";
        //public string BatchOutputPath { get; set; } = "";
    }

    public class ResourcePaneSettings : SettingsBase
    {
        private string inputPath = "";

        public string InputPath
        {
            get { return inputPath; }
            set { inputPath = value; OnPropertyChanged(); }
        }

        private string outputPath = "";

        public string OutputPath
        {
            get { return outputPath; }
            set { outputPath = value; OnPropertyChanged(); }
        }

        private string batchInputPath = "";

        public string BatchInputPath
        {
            get { return batchInputPath; }
            set { batchInputPath = value; OnPropertyChanged(); }
        }

        private string batchOutputPath = "";

        public string BatchOutputPath
        {
            get { return batchOutputPath; }
            set { batchOutputPath = value; OnPropertyChanged(); }
        }

        private int batchInputFormat;

        public int BatchInputFormat
        {
            get { return batchInputFormat; }
            set { batchInputFormat = value; OnPropertyChanged(); }
        }

        private int batchOutputFormat;

        public int BatchOutputFormat
        {
            get { return batchOutputFormat; }
            set { batchOutputFormat = value; OnPropertyChanged(); }
        }
    }

    public class OsirisPaneSettings : SettingsBase
    {
        private string inputPath = "";

        public string InputPath
        {
            get { return inputPath; }
            set { inputPath = value; OnPropertyChanged(); }
        }

        private string outputPath = "";

        public string OutputPath
        {
            get { return outputPath; }
            set { outputPath = value; OnPropertyChanged(); }
        }
    }

    public class DebugPaneSettings : SettingsBase
    {
        private string savePath = "";

        public string SavePath
        {
            get { return savePath; }
            set { savePath = value; OnPropertyChanged(); }
        }
    }

    sealed class PackageVersionConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return true;
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if(value is PackageVersion version)
            {
                switch (version)
                {
                    case PackageVersion.V10:
                        {
                            return 2;
                        }
                    case PackageVersion.V9:
                        {
                            return 3;
                        }
                    case PackageVersion.V7:
                        {
                            return 4;
                        }
                    case PackageVersion.V13:
                    default:
                        {
                            return 0;
                        }
                }
            }
            return 0;
        }
    }

    sealed class CompressionConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return true;
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (value is CompressionMethod compression)
            {
                switch (compression)
                {
                    case CompressionMethod.Zlib:
                        {
                            return 1;
                        }
                    case CompressionMethod.None:
                        {
                            return 0;
                        }
                    case CompressionMethod.LZ4:
                    default:
                        {
                            return 3;
                        }
                }
            }
            return 0;
        }
    }
}
