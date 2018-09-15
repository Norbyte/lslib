using System;
using System.Windows.Forms;
using LSLib.LS;
using LSLib.LS.Enums;

namespace ConverterApp
{
    public sealed partial class MainForm : Form
    {
        PackagePane packagePane;

        public MainForm()
        {
            InitializeComponent();

            var gr2Pane = new GR2Pane(this)
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Size = gr2Tab.ClientSize
            };
            gr2Tab.Controls.Add(gr2Pane);

            packagePane = new PackagePane
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

            var osirisPane = new OsirisPane
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Size = osirisTab.ClientSize
            };
            osirisTab.Controls.Add(osirisPane);

            Text += $" (LSLib v{Common.LibraryVersion()})";
            gr2Game.SelectedIndex = 2;
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
                pane.use16bitIndex.Checked = game == Game.DivinityOriginalSinEE || game == Game.DivinityOriginalSin2;
            }

            packagePane.SetGame(game);
        }
    }
}
