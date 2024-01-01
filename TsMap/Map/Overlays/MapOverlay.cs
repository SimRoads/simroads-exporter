using Eto.Drawing;

namespace TsMap.Map.Overlays
{
    public class MapOverlay
    {
        private readonly OverlayImage _overlayImage;

        public readonly string OverlayName;

        public readonly object ReferenceObj;

        public bool IsSecret { get; private set; }

        public byte ZoomLevelVisibility { get; private set; }

        public byte DlcGuard { get; private set; }

        public OverlayType OverlayType { get; }

        internal MapOverlay(OverlayImage overlayImage, OverlayType overlayType, string overlayName, object referenceObj)
        {
            _overlayImage = overlayImage;
            OverlayType = overlayType;
            OverlayName = overlayName;
            ReferenceObj = referenceObj;
        }

        public string TypeName { get; private set; }

        public PointF Position { get; private set; }

        internal bool IsValid()
        {
            return _overlayImage.Valid;
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

        public Bitmap GetBitmap()
        {
            return _overlayImage.GetBitmap();
        }
    }
}