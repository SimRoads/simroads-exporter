using Eto.Forms;
using System;

namespace TsMap.Canvas
{
    partial class ItemVisibilityForm : Form
    {

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.CityNamesCheckBox = new CheckBox();
            this.MapAreasCheckBox = new CheckBox();
            this.FerryConnectionsCheckBox = new CheckBox();
            this.RoadsCheckBox = new CheckBox();
            this.MapOverlaysCheckBox = new CheckBox();
            this.PrefabsCheckBox = new CheckBox();
            this.SecretRoadsCheckBox = new CheckBox();
            this.BusStopOverlayCheckBox = new CheckBox();

            // 
            // CityNamesCheckBox
            // 
            this.CityNamesCheckBox.TabIndex = 5;
            this.CityNamesCheckBox.Text = "CityNames";
            this.CityNamesCheckBox.CheckedChanged += new EventHandler<EventArgs>(this.CheckChanged);
            // 
            // MapAreasCheckBox
            // 
            this.MapAreasCheckBox.TabIndex = 2;
            this.MapAreasCheckBox.Text = "MapAreas";
            this.MapAreasCheckBox.CheckedChanged += new EventHandler<EventArgs>(this.CheckChanged);
            // 
            // FerryConnectionsCheckBox
            // 
            this.MapAreasCheckBox.TabIndex = 4;
            this.FerryConnectionsCheckBox.Text = "FerryConnections";
            this.FerryConnectionsCheckBox.CheckedChanged += new EventHandler<EventArgs>(this.CheckChanged);
            // 
            // RoadsCheckBox
            // 
            this.RoadsCheckBox.TabIndex = 1;
            this.RoadsCheckBox.Text = "Roads";
            this.RoadsCheckBox.CheckedChanged += new EventHandler<EventArgs>(this.CheckChanged);
            // 
            // MapOverlaysCheckBox
            // 
            this.MapOverlaysCheckBox.TabIndex = 3;
            this.MapOverlaysCheckBox.Text = "MapOverlays";
            this.MapOverlaysCheckBox.CheckedChanged += new EventHandler<EventArgs>(this.CheckChanged);
            // 
            // PrefabsCheckBox
            // 
            this.PrefabsCheckBox.TabIndex = 0;
            this.PrefabsCheckBox.Text = "Prefabs";
            this.PrefabsCheckBox.CheckedChanged += new EventHandler<EventArgs>(this.CheckChanged);
            // 
            // SecretRoadsCheckBox
            // 
            this.SecretRoadsCheckBox.TabIndex = 1;
            this.SecretRoadsCheckBox.Text = "Secret Roads";
            this.SecretRoadsCheckBox.CheckedChanged += new EventHandler<EventArgs>(this.CheckChanged);
            // 
            // BusStopOverlayCheckBox
            // 
            this.BusStopOverlayCheckBox.TabIndex = 5;
            this.BusStopOverlayCheckBox.Text = "Bus Stop Overlay";
            this.BusStopOverlayCheckBox.CheckedChanged += new EventHandler<EventArgs>(this.CheckChanged);


            Content = new StackLayout
            {
                Spacing = 5,
                Padding = 5,
                Items =
                {
                    this.BusStopOverlayCheckBox,
                    this.CityNamesCheckBox,
                    this.MapAreasCheckBox,
                    this.FerryConnectionsCheckBox,
                    this.SecretRoadsCheckBox,
                    this.RoadsCheckBox,
                    this.MapOverlaysCheckBox,
                    this.PrefabsCheckBox
                }
            };
            this.Title = "Item Visibility";
        }

        private CheckBox CityNamesCheckBox;
        private CheckBox MapAreasCheckBox;
        private CheckBox FerryConnectionsCheckBox;
        private CheckBox RoadsCheckBox;
        private CheckBox MapOverlaysCheckBox;
        private CheckBox PrefabsCheckBox;
        private CheckBox SecretRoadsCheckBox;
        private CheckBox BusStopOverlayCheckBox;
    }
}