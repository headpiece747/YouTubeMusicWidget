// YouTubeMusicWidgetSettings.xaml.cs
using System.Windows;
using System.Windows.Controls;
using WigiDashWidgetFramework;

namespace YouTubeMusicWidget
{
    public partial class YouTubeMusicWidgetSettings : UserControl
    {
        private YouTubeMusicWidgetInstance _widgetInstance;

        public YouTubeMusicWidgetSettings(YouTubeMusicWidgetInstance widgetInstance)
        {
            InitializeComponent();
            _widgetInstance = widgetInstance;
            UpdateAuthStatus();
        }

        private void UpdateAuthStatus()
        {
            if (_widgetInstance.IsAuthenticated())
            {
                AuthStatusLabel.Content = "Authenticated";
                AuthenticateButton.IsEnabled = false;
            }
            else
            {
                AuthStatusLabel.Content = "Not Authenticated";
                AuthenticateButton.IsEnabled = true;
            }
        }

        private async void AuthenticateButton_Click(object sender, RoutedEventArgs e)
        {
            AuthenticateButton.IsEnabled = false;
            AuthStatusLabel.Content = "Authenticating... Please approve in the YouTube Music app.";
            await _widgetInstance.Authenticate();
            UpdateAuthStatus();
        }
    }
}
