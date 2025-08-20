// YouTubeMusicWidgetObject.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using WigiDashWidgetFramework;
using WigiDashWidgetFramework.WidgetUtility;

namespace YouTubeMusicWidget
{
    public class YouTubeMusicWidgetObject : IWidgetObject
    {
        public Guid Guid => new Guid("{A5A40218-3349-4848-9473-483F5195A44F}");
        public string Name => "YouTube Music Player";
        public string Author => "headpiece747";
        public string Website => "https://eclipticsight.com";
        public string Description => "Control the YouTube Music Desktop app.";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public SdkVersion TargetSdk => WidgetUtility.CurrentSdkVersion;
        public List<WidgetSize> SupportedSizes => new List<WidgetSize> { new WidgetSize(5, 4) };
        public IWidgetManager WidgetManager { get; set; }
        public string LastErrorMessage { get; set; }

        private Bitmap _previewImage;

        public Bitmap PreviewImage
        {
            get
            {
                // Generate the preview image programmatically if it hasn't been created yet
                if (_previewImage == null)
                {
                    _previewImage = CreatePlaceholderBitmap("YTM");
                }
                return _previewImage;
            }
        }

        public Bitmap WidgetThumbnail => PreviewImage;

        public Bitmap GetWidgetPreview(WidgetSize widgetSize) => PreviewImage;

        public IWidgetInstance CreateWidgetInstance(WidgetSize widgetSize, Guid instanceGuid)
        {
            return new YouTubeMusicWidgetInstance(this, widgetSize, instanceGuid);
        }

        public bool RemoveWidgetInstance(Guid instanceGuid)
        {
            return true;
        }

        public WidgetError Load(string resourcePath)
        {
            // No resources to load from file anymore
            return WidgetError.NO_ERROR;
        }

        public WidgetError Unload()
        {
            _previewImage?.Dispose();
            _previewImage = null;
            return WidgetError.NO_ERROR;
        }

        private Bitmap CreatePlaceholderBitmap(string text)
        {
            var placeholder = new Bitmap(200, 160); // 5x4 ratio
            using (var g = Graphics.FromImage(placeholder))
            {
                g.Clear(Color.FromArgb(35, 35, 35)); // Dark gray background
                using (var font = new Font("Arial", 48, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                {
                    var stringFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(text, font, brush, new RectangleF(0, 0, 200, 160), stringFormat);
                }
            }
            return placeholder;
        }
    }
}
