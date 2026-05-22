using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CleanerControlApp.Vision.Shared
{
 public static class StatusPopup
 {
 /// <summary>
 /// Show a modal status popup with auto-close countdown.
 /// owner may be null, in which case Application.Current.MainWindow is used if available.
 /// </summary>
 public static void Show(string status, Window? owner = null, int autoCloseSeconds =10)
 {
 try
 {
 var wOwner = owner ?? Application.Current?.MainWindow;
 var w = new Window()
 {
 Title = "無法操作原因",
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
 Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xEE,0xFF,0xCC,0xCC)),
 BorderBrush = System.Windows.Media.Brushes.DarkRed,
 BorderThickness = new Thickness(1),
 CornerRadius = new CornerRadius(6),
 Padding = new Thickness(10)
 };

 var panel = new StackPanel() { Orientation = Orientation.Vertical };
 var txt = new TextBlock() { Text = status, FontSize =14, TextWrapping = TextWrapping.Wrap, Foreground = System.Windows.Media.Brushes.Black, MaxWidth =300 };
 panel.Children.Add(txt);

 var countdown = new TextBlock() { Text = $"將在 {autoCloseSeconds} 秒後關閉", FontSize =14, Margin = new Thickness(0,8,0,0), Foreground = System.Windows.Media.Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center };
 panel.Children.Add(countdown);

 var dt = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(1) };
 int remaining = autoCloseSeconds;
 dt.Tick += (s, e) =>
 {
 try
 {
 remaining -=1;
 if (remaining <=0)
 {
 dt.Stop();
 try { if (w.IsVisible) w.Close(); } catch { }
 }
 else
 {
 try { countdown.Text = $"將在 {remaining} 秒後關閉"; } catch { }
 }
 }
 catch { }
 };

 var btn = new Button() { Content = "關閉", FontSize =18, Padding = new Thickness(8,6,8,6), Margin = new Thickness(0,10,0,0), HorizontalAlignment = HorizontalAlignment.Center };
 btn.Click += (s, e) =>
 {
 try
 {
 if (dt.IsEnabled) dt.Stop();
 try { w.Close(); } catch { }
 }
 catch { }
 };
 panel.Children.Add(btn);

 border.Child = panel;
 w.Content = border;

 w.Closed += (s, e) => { try { if (dt.IsEnabled) dt.Stop(); } catch { } };
 w.Loaded += (s, e) => { try { dt.Start(); } catch { } };

 try { w.ShowDialog(); } catch { }
 }
 catch
 {
 // ignore
 }
 }
 }
}
