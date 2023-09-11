using System;
using Eto.Forms;

namespace TsMap.Canvas
{
    public partial class ItemVisibilityForm : Form
    {
        public ItemVisibilityForm(RenderFlags renderFlags)
        {
            InitializeComponent();
            PrefabsCheckBox.Checked = renderFlags.IsActive(RenderFlags.Prefabs);
            RoadsCheckBox.Checked = renderFlags.IsActive(RenderFlags.Roads);
            SecretRoadsCheckBox.Checked = renderFlags.IsActive(RenderFlags.SecretRoads);
            MapAreasCheckBox.Checked = renderFlags.IsActive(RenderFlags.MapAreas);
            MapOverlaysCheckBox.Checked = renderFlags.IsActive(RenderFlags.MapOverlays);
            FerryConnectionsCheckBox.Checked = renderFlags.IsActive(RenderFlags.FerryConnections);
            CityNamesCheckBox.Checked = renderFlags.IsActive(RenderFlags.CityNames);
            BusStopOverlayCheckBox.Checked = renderFlags.IsActive(RenderFlags.BusStopOverlay);
        }

        public delegate void UpdateItemVisibilityEvent(RenderFlags renderFlags);

        public UpdateItemVisibilityEvent UpdateItemVisibility;

        private void CheckChanged(object sender, EventArgs e) // Gets called if any checkbox is changed
        {
            RenderFlags renderFlags = 0;
            if (PrefabsCheckBox.Checked == true) renderFlags |= RenderFlags.Prefabs;
            if (RoadsCheckBox.Checked == true) renderFlags |= RenderFlags.Roads;
            if (SecretRoadsCheckBox.Checked == true) renderFlags |= RenderFlags.SecretRoads;
            if (MapAreasCheckBox.Checked == true) renderFlags |= RenderFlags.MapAreas;
            if (MapOverlaysCheckBox.Checked == true)
            {
                renderFlags |= RenderFlags.MapOverlays;
                renderFlags |= RenderFlags.TextOverlay;
            }
            else BusStopOverlayCheckBox.Checked = false;
            if (FerryConnectionsCheckBox.Checked == true) renderFlags |= RenderFlags.FerryConnections;
            if (CityNamesCheckBox.Checked == true) renderFlags |= RenderFlags.CityNames;
            if (BusStopOverlayCheckBox.Checked == true) renderFlags |= RenderFlags.BusStopOverlay;
            UpdateItemVisibility?.Invoke(renderFlags);
        }
    }
}
