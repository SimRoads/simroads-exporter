using Eto.Drawing;
using Eto.Forms;
using System;

namespace TsMap.Canvas
{
    partial class TsMapCanvas : Form
    {
        private void InitializeComponent()
        {
            this.MapPanel = new MapPanel();
            this.CitySubMenuItem = new SubMenuItem() { Text = "Cities" };
            // 
            // MapPanel
            // 
            this.MapPanel.Paint += new EventHandler<PaintEventArgs>(this.MapPanel_Paint);

            this.Title = "TsMapCanvas";
            this.Content = this.MapPanel;

            Menu = new MenuBar
            {
                Items =
                {
                    new SubMenuItem
                    {
                        Text = "Main",
                        Items =
                        {
                            new Command(this.ExportMapMenuItem_Click) {MenuText = "Export Map"},
                            new Command(this.ExitToolStripMenuItem_Click) {MenuText = "Exit"}
                        }
                    },
                    new SubMenuItem
                    {
                        Text = "View",
                        Items =
                        {
                            new SubMenuItem
                            {
                                Text = "Map",
                                Items =
                                {
                                    new Command(this.FullMapToolStripMenuItem_Click) {MenuText = "Full"},
                                    new Command(this.ResetMapToolStripMenuItem_Click) {MenuText = "Reset"},
                                    CitySubMenuItem
                                }
                            },
                             new Command(this.ItemVisibilityToolStripMenuItem_Click) {MenuText = "Item Visibility"},
                             new Command(this.localizationSettingsToolStripMenuItem_Click) {MenuText = "Localization Settings"},
                             new Command(this.paletteToolStripMenuItem_Click) {MenuText = "Palette"},
                             new Command(this.dLCGuardToolStripMenuItem_Click) {MenuText = "DLC Guards"},


                        }
                    }
                }
            };

            Size = new Size(600, 600);
        }

        private MapPanel MapPanel;
        private SubMenuItem CitySubMenuItem;
    }
}