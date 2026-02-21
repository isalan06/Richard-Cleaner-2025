using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CleanerControlApp.Vision.Developer
{
 public partial class PlcTest_MotorStatusView : UserControl
 {
 public PlcTest_MotorStatusView()
 {
 InitializeComponent();
 DataContext = new MotorStatusViewModel();
 }

 private void OnJogPlusClick(object sender, RoutedEventArgs e)
 {
 MessageBox.Show("Jog+ pressed");
 }

 private void OnJogMinusClick(object sender, RoutedEventArgs e)
 {
 MessageBox.Show("Jog- pressed");
 }

 private void OnJSpeedHighClick(object sender, RoutedEventArgs e)
 {
 MessageBox.Show("JSpeed High pressed");
 }

 private void OnJSpeedMedClick(object sender, RoutedEventArgs e)
 {
 MessageBox.Show("JSpeed Medium pressed");
 }

 private void OnHomeClick(object sender, RoutedEventArgs e)
 {
 MessageBox.Show("Home pressed");
 }

 private void OnStopClick(object sender, RoutedEventArgs e)
 {
 MessageBox.Show("Stop pressed");
 }

 private void OnCommandClick(object sender, RoutedEventArgs e)
 {
 MessageBox.Show("Command pressed");
 }

 private void OnServoOnClick(object sender, RoutedEventArgs e)
 {
 MessageBox.Show("Servo On pressed");
 }

 private void OnAlarmResetClick(object sender, RoutedEventArgs e)
 {
 MessageBox.Show("Alarm Reset pressed");
 }

 // Numeric-only input handlers
 private static readonly Regex _numericRegex = new("[^0-9]+");
 private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
 {
 e.Handled = _numericRegex.IsMatch(e.Text);
 }

 private void NumberOnly_Pasting(object sender, DataObjectPastingEventArgs e)
 {
 if (e.DataObject.GetDataPresent(DataFormats.Text))
 {
 var text = e.DataObject.GetData(DataFormats.Text) as string;
 if (_numericRegex.IsMatch(text))
 {
 e.CancelCommand();
 }
 }
 else
 {
 e.CancelCommand();
 }
 }
 }

 // Simple view models for demo purposes
 public class IndicatorItem
 {
 public string Description { get; set; }
 public bool IsOn { get; set; }
 }

 public class AxisViewModel
 {
 public string Header { get; set; }
 public ObservableCollection<IndicatorItem> AlarmItems { get; set; } = new();
 public ObservableCollection<IndicatorItem> StatusItems { get; set; } = new();
 public int CurrentPosition { get; set; }
 
 // Text properties bound to TextBoxes (string to allow empty -> treated as0)
 private string _targetPositionText = "0";
 public string TargetPositionText
 {
 get => _targetPositionText;
 set
 {
 _targetPositionText = string.IsNullOrWhiteSpace(value) ? "0" : value;
 // parse and write to PLC or model here
 int.TryParse(_targetPositionText, out var v);
 // TODO: write v to PLC
 }
 }

 private string _speedText = "0";
 public string SpeedText
 {
 get => _speedText;
 set
 {
 _speedText = string.IsNullOrWhiteSpace(value) ? "0" : value;
 int.TryParse(_speedText, out var v);
 // TODO: write v to PLC
 }
 }
 }

 public class MotorStatusViewModel
 {
 public ObservableCollection<AxisViewModel> Axes { get; set; } = new();

 public MotorStatusViewModel()
 {
 string[] headers = new[]
 {
 "軸1 - 移載組X軸",
 "軸2 - 移載組Z軸",
 "軸3 - 沖洗槽Z軸",
 "軸4 - 浸泡槽Z軸"
 };

 for (int i =0; i <4; i++)
 {
 var axis = new AxisViewModel
 {
 Header = headers[i],
 CurrentPosition =1234,
 TargetPositionText = "1234",
 SpeedText = "1234"
 };
 // Alarm items
 axis.AlarmItems.Add(new IndicatorItem { Description = "軸異常", IsOn = false });
 axis.AlarmItems.Add(new IndicatorItem { Description = "負極限", IsOn = false });
 axis.AlarmItems.Add(new IndicatorItem { Description = "正極限", IsOn = false });
 axis.AlarmItems.Add(new IndicatorItem { Description = "Home Timeout", IsOn = false });
 axis.AlarmItems.Add(new IndicatorItem { Description = "Command Timeout", IsOn = false });

 // Status items
 axis.StatusItems.Add(new IndicatorItem { Description = "原點復歸完成", IsOn = true });
 axis.StatusItems.Add(new IndicatorItem { Description = "Home 流程", IsOn = false });
 axis.StatusItems.Add(new IndicatorItem { Description = "Command 流程", IsOn = false });
 axis.StatusItems.Add(new IndicatorItem { Description = "定位指令驅動中", IsOn = false });
 axis.StatusItems.Add(new IndicatorItem { Description = "脈衝輸出即時停止", IsOn = false });

 Axes.Add(axis);
 }
 }
 }
}
