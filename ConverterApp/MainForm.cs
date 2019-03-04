using System;
using Alphaleonis.Win32.Filesystem;
using System.Windows.Forms;
using LSLib.LS;
using LSLib.LS.Enums;
using Newtonsoft.Json;
using System.ComponentModel;

namespace ConverterApp
{
    public sealed partial class MainForm : Form, ISettingsDataSource
    {
        PackagePane packagePane;

        public ConverterAppSettings Settings { get; set; }

        public MainForm()
        {
            InitializeComponent();

            Settings = new ConverterAppSettings();

            try
            {
                if (File.Exists("settings.json"))
                {
                    using (System.IO.StreamReader file = File.OpenText("settings.json"))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        Settings = (ConverterAppSettings)serializer.Deserialize(file, typeof(ConverterAppSettings));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading settings: {ex.ToString()}");
            }

            var gr2Pane = new GR2Pane(this)
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Size = gr2Tab.ClientSize
            };
            gr2Tab.Controls.Add(gr2Pane);

            packagePane = new PackagePane(this)
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Size = packageTab.ClientSize
            };
            packageTab.Controls.Add(packagePane);

            var resourcePane = new ResourcePane(this)
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Size = resourceTab.ClientSize
            };
            resourceTab.Controls.Add(resourcePane);

            var osirisPane = new OsirisPane(this)
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Size = osirisTab.ClientSize
            };
            osirisTab.Controls.Add(osirisPane);

            Text += $" (LSLib v{Common.LibraryVersion()})";

            gr2Game.SelectedIndex = gr2Game.Items.Count - 1;
            gr2Game.DataBindings.Add("SelectedIndex", Settings, "SelectedGame", true, DataSourceUpdateMode.OnPropertyChanged);

            Settings.UpdateVersion(Common.LibraryVersion());
            Settings.SetPropertyChangedEvent(SaveSettings);
        }

        private void SaveSettings(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                File.WriteAllText("settings.json", JsonConvert.SerializeObject(Settings, Formatting.Indented));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.ToString()}");
            }
        }

        public Game GetGame()
        {
            switch (gr2Game.SelectedIndex)
            {
                case 0:
                {
                    return Game.DivinityOriginalSin;
                }
                case 1:
                {
                    return Game.DivinityOriginalSinEE;
                }
                case 2:
                {
                    return Game.DivinityOriginalSin2;
                }
                case 3:
                {
                    return Game.DivinityOriginalSin2DE;
                }
                default:
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private void gr2Game_SelectedIndexChanged(object sender, EventArgs e)
        {
            Game game = GetGame();
            if (gr2Tab.Controls[0] is GR2Pane pane)
            {
                pane.use16bitIndex.Checked = game == Game.DivinityOriginalSinEE || game.IsDOS2();
                pane.flipMeshes.Checked = game.IsDOS2();
            }

            packagePane.SetGame(game);
        }
    }
}
