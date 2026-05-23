using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CleanerControlApp.Vision.Shared
{
    public static class InfoAskPopup
    {
        /// <summary>
        /// Show a modal confirmation popup with Confirm/Cancel buttons.
        /// Returns true when user clicks 確認, false otherwise.
        /// </summary>
        public static bool Ask(string message, Window? owner = null)
        {
            try
            {
                var wOwner = owner ?? Application.Current?.MainWindow;
                var w = new Window()
                {
                    Title = "訊息確認",
                    Owner = wOwner,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    ShowInTaskbar = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var border = new Border()
                {
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xEE, 0xEE, 0xFF, 0xEE)),
                    BorderBrush = System.Windows.Media.Brushes.DarkBlue,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10)
                };

                var panel = new StackPanel() { Orientation = Orientation.Vertical };
                var txt = new TextBlock() { Text = message, FontSize = 18, TextWrapping = TextWrapping.Wrap, Foreground = System.Windows.Media.Brushes.Black, MaxWidth = 400 };
                panel.Children.Add(txt);

                var btnPanel = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 0) };

                var btnConfirm = new Button() { Content = "確認", FontSize = 16, Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(6, 0, 6, 0), MinWidth = 80 };
                var btnCancel = new Button() { Content = "取消", FontSize = 16, Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(6, 0, 6, 0), MinWidth = 80 };

                btnConfirm.Click += (s, e) =>
                {
                    try { w.DialogResult = true; w.Close(); } catch { try { w.Close(); } catch { } }
                };
                btnCancel.Click += (s, e) =>
                {
                    try { w.DialogResult = false; w.Close(); } catch { try { w.Close(); } catch { } }
                };

                btnPanel.Children.Add(btnConfirm);
                btnPanel.Children.Add(btnCancel);
                panel.Children.Add(btnPanel);

                border.Child = panel;
                w.Content = border;

                bool? res = null;
                try { res = w.ShowDialog(); } catch { }
                return res == true;
            }
            catch
            {
                return false;
            }
        }
    }
}
