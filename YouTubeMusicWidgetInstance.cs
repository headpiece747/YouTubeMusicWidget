// YouTubeMusicWidgetInstance.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using WigiDashWidgetFramework;
using WigiDashWidgetFramework.WidgetUtility;

namespace YouTubeMusicWidget
{
    public class YouTubeMusicWidgetInstance : IWidgetInstance, IDisposable
    {
        public IWidgetObject WidgetObject { get; }
        public Guid Guid { get; }
        public WidgetSize WidgetSize { get; }
        public event WidgetUpdatedEventHandler WidgetUpdated;
        public IWidgetManager WidgetManager { get; set; }

        private readonly YouTubeMusicApiClient _apiClient;
        private State _currentState;
        private Bitmap _currentAlbumArt;
        private Bitmap _prevIcon, _playIcon, _pauseIcon, _nextIcon, _rewindIcon, _forwardIcon;
        private Bitmap _likeIcon, _dislikeIcon, _likedIcon, _shuffleOnIcon, _repeatOneOnIcon, _repeatAllOnIcon;
        private Color _currentWidgetBackgroundColor = Color.Black;

        private bool _disposed;

        public YouTubeMusicWidgetInstance(IWidgetObject widgetObject, WidgetSize widgetSize, Guid instanceGuid)
        {
            WidgetObject = widgetObject;
            WidgetSize = widgetSize;
            Guid = instanceGuid;

            _apiClient = new YouTubeMusicApiClient();
            _apiClient.Authenticated += OnAuthenticated;
            _apiClient.StateChanged += OnStateChanged;
            _apiClient.LogMessage += OnApiClientLogMessage;

            LoadControlIcons();
        }

        private void OnApiClientLogMessage(string message)
        {
            WidgetManager?.WriteLogMessage(this, LogLevel.INFO, $"[API Client] {message}");
        }

        public void OnActivated()
        {
            LoadTokenAndConnect();
        }

        private async void LoadTokenAndConnect()
        {
            if (LoadToken())
            {
                await _apiClient.ConnectAsync();
            }
        }

        private void OnAuthenticated()
        {
            SaveToken();
            Task.Run(() => _apiClient.ConnectAsync());
        }

        private void OnStateChanged(State state)
        {
            if (state?.Video != null)
            {
                WidgetManager?.WriteLogMessage(this, LogLevel.INFO, $"Track: {state.Video.Title} by {state.Video.Author}");
            }
            else
            {
                WidgetManager?.WriteLogMessage(this, LogLevel.WARN, "Received state or video is null. Is music playing?");
            }

            _currentState = state;
            Task.Run(() => UpdateAlbumArtAsync());
            RequestUpdate();
        }

        public void RequestUpdate()
        {
            if (_disposed) return;

            using (Bitmap widgetBitmap = DrawWidget())
            {
                if (widgetBitmap != null)
                {
                    var eventArgs = new WidgetUpdatedEventArgs
                    {
                        WidgetBitmap = (Bitmap)widgetBitmap.Clone(),
                        Offset = Point.Empty,
                        WaitMax = 1000
                    };
                    WidgetUpdated?.Invoke(this, eventArgs);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _apiClient?.Dispose();
                    _currentAlbumArt?.Dispose();
                    _prevIcon?.Dispose();
                    _playIcon?.Dispose();
                    _pauseIcon?.Dispose();
                    _nextIcon?.Dispose();
                    _rewindIcon?.Dispose();
                    _forwardIcon?.Dispose();
                    _likeIcon?.Dispose();
                    _dislikeIcon?.Dispose();
                    _likedIcon?.Dispose();
                    _shuffleOnIcon?.Dispose();
                    _repeatOneOnIcon?.Dispose();
                    _repeatAllOnIcon?.Dispose();
                }
                _disposed = true;
            }
        }

        public void EnterSleep() { }

        public void ExitSleep()
        {
            RequestUpdate();
        }

        public UserControl GetSettingsControl()
        {
            return new YouTubeMusicWidgetSettings(this);
        }

        public async Task Authenticate()
        {
            await _apiClient.AuthenticateAsync("youtubemusicwidget", "YouTube Music Widget", "1.0.0");
        }

        public bool IsAuthenticated()
        {
            return _apiClient.IsAuthenticated;
        }

        private bool LoadToken()
        {
            if (WidgetManager != null && WidgetManager.LoadSetting(this, "token", out string token) && !string.IsNullOrEmpty(token))
            {
                _apiClient.SetToken(token);
                return true;
            }
            return false;
        }

        private void SaveToken()
        {
            if (WidgetManager != null)
            {
                WidgetManager.StoreSetting(this, "token", _apiClient.GetToken());
            }
        }

        private void LoadControlIcons()
        {
            // Generate icons programmatically instead of loading from files
            _prevIcon = CreateIconBitmap("⏮");
            _rewindIcon = CreateIconBitmap("⏪");
            _playIcon = CreateIconBitmap("▶");
            _pauseIcon = CreateIconBitmap("⏸");
            _forwardIcon = CreateIconBitmap("⏩");
            _nextIcon = CreateIconBitmap("⏭");
            _likeIcon = CreateIconBitmap("👍");
            _dislikeIcon = CreateIconBitmap("👎");
            _likedIcon = CreateIconBitmap("👍", Brushes.DodgerBlue); // A different color for liked state
            _shuffleOnIcon = CreateIconBitmap("🔀", Brushes.DodgerBlue);
            _repeatOneOnIcon = CreateIconBitmap("🔁¹", Brushes.DodgerBlue);
            _repeatAllOnIcon = CreateIconBitmap("🔁", Brushes.DodgerBlue);
        }

        private Bitmap CreateIconBitmap(string text, Brush color = null)
        {
            color = color ?? Brushes.White;
            var bitmap = new Bitmap(64, 64);
            using (var g = Graphics.FromImage(bitmap))
            using (var font = new Font("Segoe UI Emoji", 24)) // Using an emoji font
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Color.Transparent);

                var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(text, font, color, new RectangleF(0, 0, 64, 64), stringFormat);
            }
            return bitmap;
        }


        private async Task UpdateAlbumArtAsync()
        {
            if (_currentState?.Video?.Thumbnails == null || _currentState.Video.Thumbnails.Count == 0)
            {
                _currentAlbumArt?.Dispose();
                _currentAlbumArt = null;
                UpdateBackgroundColorFromAlbumArt();
                return;
            }

            var thumbnailUrl = _currentState.Video.Thumbnails[0].Url;

            // Fix for URLs that are missing the protocol
            if (!thumbnailUrl.StartsWith("http"))
            {
                thumbnailUrl = "https:" + thumbnailUrl;
            }

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(thumbnailUrl);
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        _currentAlbumArt?.Dispose();
                        _currentAlbumArt = new Bitmap(stream);
                        UpdateBackgroundColorFromAlbumArt();
                    }
                }
            }
            catch (Exception ex)
            {
                WidgetManager?.WriteLogMessage(this, LogLevel.WARN, $"Failed to load album art from {thumbnailUrl}: {ex.Message}");
                _currentAlbumArt?.Dispose();
                _currentAlbumArt = null;
                UpdateBackgroundColorFromAlbumArt();
            }
        }

        private void UpdateBackgroundColorFromAlbumArt()
        {
            if (_currentAlbumArt == null)
            {
                _currentWidgetBackgroundColor = Color.Black;
                return;
            }

            try
            {
                using (Bitmap tinyArt = new Bitmap(1, 1))
                using (Graphics g = Graphics.FromImage(tinyArt))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(_currentAlbumArt, new Rectangle(0, 0, 1, 1));
                    _currentWidgetBackgroundColor = tinyArt.GetPixel(0, 0);
                }
            }
            catch { _currentWidgetBackgroundColor = Color.Black; }
        }

        public async void ClickEvent(ClickType click_type, int x, int y)
        {
            if (click_type != ClickType.Single || _disposed) return;

            var layout = CalculateLayout();
            Point click = new Point(x, y);

            if (layout.PrevButtonRect.Contains(click))
                await _apiClient.SendCommandAsync("previous");
            else if (layout.RewindButtonRect.Contains(click))
                await _apiClient.SendCommandAsync("player-rewind");
            else if (layout.PlayPauseButtonRect.Contains(click))
                await _apiClient.SendCommandAsync("playPause");
            else if (layout.ForwardButtonRect.Contains(click))
                await _apiClient.SendCommandAsync("player-forward");
            else if (layout.NextButtonRect.Contains(click))
                await _apiClient.SendCommandAsync("next");
            else if (layout.LikeButtonRect.Contains(click))
                await _apiClient.SendCommandAsync("track-like");
            else if (layout.DislikeButtonRect.Contains(click))
                await _apiClient.SendCommandAsync("track-dislike");
            else if (layout.ShuffleButtonRect.Contains(click))
                await _apiClient.SendCommandAsync("player-shuffle");
            else if (layout.RepeatButtonRect.Contains(click))
                await _apiClient.SendCommandAsync("player-repeat");
        }

        private WidgetLayout CalculateLayout()
        {
            var layout = new WidgetLayout();
            Size size = new Size(WidgetSize.Width, WidgetSize.Height);
            int padding = 10;
            int albumArtSide = size.Height - (2 * padding);
            layout.AlbumArtRect = new Rectangle(padding, padding, albumArtSide, albumArtSide);

            int textX = layout.AlbumArtRect.Right + padding;
            int textWidth = size.Width - textX - padding;

            layout.TextFormat = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

            int titleFontHeight = 28;
            int infoFontHeight = 24;
            int textBlockHeight = titleFontHeight + infoFontHeight * 2 + 10;
            int textBlockTopY = (size.Height - textBlockHeight) / 2;

            layout.TitleRect = new RectangleF(textX, textBlockTopY, textWidth, titleFontHeight);
            layout.ArtistRect = new RectangleF(textX, textBlockTopY + titleFontHeight + 5, textWidth, infoFontHeight);
            layout.AlbumRect = new RectangleF(textX, textBlockTopY + titleFontHeight + infoFontHeight + 10, textWidth, infoFontHeight);

            int mainBtnIconSize = 40; // Reduced size to fit 5 buttons
            int sideBtnIconSize = 32;
            int btnSpacing = 5;

            int mainButtonsTopY = size.Height - padding - mainBtnIconSize;
            int totalMainButtonsWidth = (5 * mainBtnIconSize) + (4 * btnSpacing);
            int mainButtonsStartX = textX + (textWidth - totalMainButtonsWidth) / 2;

            layout.PrevButtonRect = new Rectangle(mainButtonsStartX, mainButtonsTopY, mainBtnIconSize, mainBtnIconSize);
            layout.RewindButtonRect = new Rectangle(layout.PrevButtonRect.Right + btnSpacing, mainButtonsTopY, mainBtnIconSize, mainBtnIconSize);
            layout.PlayPauseButtonRect = new Rectangle(layout.RewindButtonRect.Right + btnSpacing, mainButtonsTopY, mainBtnIconSize, mainBtnIconSize);
            layout.ForwardButtonRect = new Rectangle(layout.PlayPauseButtonRect.Right + btnSpacing, mainButtonsTopY, mainBtnIconSize, mainBtnIconSize);
            layout.NextButtonRect = new Rectangle(layout.ForwardButtonRect.Right + btnSpacing, mainButtonsTopY, mainBtnIconSize, mainBtnIconSize);

            int sideButtonsTopY = mainButtonsTopY + (mainBtnIconSize - sideBtnIconSize) / 2;
            layout.DislikeButtonRect = new Rectangle(layout.PrevButtonRect.Left - sideBtnIconSize - btnSpacing, sideButtonsTopY, sideBtnIconSize, sideBtnIconSize);
            layout.LikeButtonRect = new Rectangle(layout.NextButtonRect.Right + btnSpacing, sideButtonsTopY, sideBtnIconSize, sideBtnIconSize);

            int topButtonsY = padding;
            layout.ShuffleButtonRect = new Rectangle(size.Width - padding - sideBtnIconSize, topButtonsY, sideBtnIconSize, sideBtnIconSize);
            layout.RepeatButtonRect = new Rectangle(layout.ShuffleButtonRect.Left - sideBtnIconSize - btnSpacing, topButtonsY, sideBtnIconSize, sideBtnIconSize);

            return layout;
        }

        private Bitmap DrawWidget()
        {
            Size size = new Size(WidgetSize.Width, WidgetSize.Height);
            Bitmap bitmap = new Bitmap(size.Width, size.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(_currentWidgetBackgroundColor);

                var layout = CalculateLayout();
                Brush textBrush = _currentWidgetBackgroundColor.GetBrightness() > 0.5 ? Brushes.Black : Brushes.White;

                if (_currentAlbumArt != null)
                {
                    g.DrawImage(_currentAlbumArt, layout.AlbumArtRect);
                }
                else
                {
                    using (var placeholderBrush = new SolidBrush(Color.FromArgb(100, 128, 128, 128)))
                    {
                        g.FillRectangle(placeholderBrush, layout.AlbumArtRect);
                    }
                }

                if (_currentState?.Video != null)
                {
                    using (var titleFont = new Font("Arial", 24, FontStyle.Bold))
                    using (var infoFont = new Font("Arial", 20))
                    {
                        g.DrawString(_currentState.Video.Title, titleFont, textBrush, layout.TitleRect, layout.TextFormat);
                        g.DrawString(_currentState.Video.Author, infoFont, textBrush, layout.ArtistRect, layout.TextFormat);
                        g.DrawString(_currentState.Video.Album, infoFont, textBrush, layout.AlbumRect, layout.TextFormat);
                    }
                }
                else
                {
                    using (var font = new Font("Arial", 12, FontStyle.Bold))
                    {
                        g.DrawString("Waiting for YouTube Music...", font, Brushes.White, 10, 10);
                    }
                }

                // Main Playback Controls
                Bitmap currentPlayPauseIcon = (_currentState?.Player?.TrackState == 1 ? _pauseIcon : _playIcon);
                if (_prevIcon != null) g.DrawImage(_prevIcon, layout.PrevButtonRect);
                if (_rewindIcon != null) g.DrawImage(_rewindIcon, layout.RewindButtonRect);
                if (currentPlayPauseIcon != null) g.DrawImage(currentPlayPauseIcon, layout.PlayPauseButtonRect);
                if (_forwardIcon != null) g.DrawImage(_forwardIcon, layout.ForwardButtonRect);
                if (_nextIcon != null) g.DrawImage(_nextIcon, layout.NextButtonRect);

                // Like/Dislike Controls
                Bitmap currentLikeIcon = (_currentState?.Video?.LikeStatus == 1 ? _likedIcon : _likeIcon);
                if (currentLikeIcon != null) g.DrawImage(currentLikeIcon, layout.LikeButtonRect);
                if (_dislikeIcon != null) g.DrawImage(_dislikeIcon, layout.DislikeButtonRect);

                // Shuffle/Repeat Controls
                if (_currentState?.Player?.Shuffle == true && _shuffleOnIcon != null)
                {
                    g.DrawImage(_shuffleOnIcon, layout.ShuffleButtonRect);
                }

                if (_currentState?.Player?.Queue != null && _currentState.Player.Queue.RepeatMode != 0)
                {
                    Bitmap repeatIcon = _currentState.Player.Queue.RepeatMode == 2 ? _repeatOneOnIcon : _repeatAllOnIcon;
                    if (repeatIcon != null)
                    {
                        g.DrawImage(repeatIcon, layout.RepeatButtonRect);
                    }
                }
            }

            return bitmap;
        }

        private struct WidgetLayout
        {
            public Rectangle AlbumArtRect;
            public RectangleF TitleRect;
            public RectangleF ArtistRect;
            public RectangleF AlbumRect;
            public Rectangle PrevButtonRect;
            public Rectangle RewindButtonRect;
            public Rectangle PlayPauseButtonRect;
            public Rectangle ForwardButtonRect;
            public Rectangle NextButtonRect;
            public Rectangle LikeButtonRect;
            public Rectangle DislikeButtonRect;
            public Rectangle ShuffleButtonRect;
            public Rectangle RepeatButtonRect;
            public StringFormat TextFormat;
        }
    }
}
