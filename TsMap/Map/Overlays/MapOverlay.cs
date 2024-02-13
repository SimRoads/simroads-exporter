using System.Drawing;
using TsMap.Helpers;

namespace TsMap.Map.Overlays
{
    public class MapOverlay
    {
        public readonly OverlayImage OverlayImage;

        public readonly string OverlayName;

        public bool IsSecret { get; private set; }

        public byte ZoomLevelVisibility { get; private set; }

        public byte DlcGuard { get; private set; }

        public OverlayType OverlayType { get; }

        internal MapOverlay(OverlayImage overlayImage, OverlayType overlayType, string overlayName)
        {
            OverlayImage = overlayImage;
            OverlayType = overlayType;
            OverlayName = overlayName;
        }

        public string TypeName { get; private set; }

        public PointF Position { get; private set; }

        internal bool IsValid()
        {
            return OverlayImage.Valid;
        }

        internal void SetPosition(float x, float y)
        {
            Position = new PointF(x, y);
        }

        internal void SetSecret(bool secret)
        {
            IsSecret = secret;
        }

        internal void SetTypeName(string name)
        {
            TypeName = name;
        }

        internal void SetZoomLevelVisibility(byte flags)
        {
            ZoomLevelVisibility = flags;
        }

        internal void SetDlcGuard(byte dlcGuard)
        {
            DlcGuard = dlcGuard;
        }
    }
}