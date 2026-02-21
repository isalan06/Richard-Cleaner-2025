using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using CleanerControlApp;

namespace CleanerControlApp.Vision.Developer
{
	public partial class PlcTest_MotorStatusView : UserControl
	{
		// track which command buttons are currently pressed so we only execute once per press
		private readonly System.Collections.Generic.HashSet<Button> _pressedCommandButtons = new();

		public PlcTest_MotorStatusView()
		{
			InitializeComponent();
			DataContext = new MotorStatusViewModel();
		}

		private void OnJogPlusClick(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement fe && fe.DataContext is AxisViewModel axis)
			{
				axis.Command_JogPlus();
				return;
			}
			MessageBox.Show("Jog+ pressed");
		}

		private void OnJogMinusClick(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement fe && fe.DataContext is AxisViewModel axis)
			{
				axis.Command_JogMinus();
				return;
			}
			MessageBox.Show("Jog- pressed");
		}

		private void OnJSpeedHighClick(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement fe && fe.DataContext is AxisViewModel axis)
			{
				axis.JSpeedH_On = !axis.JSpeedH_On;
				return;
			}
			MessageBox.Show("JSpeed High pressed");
		}

		private void OnJSpeedMedClick(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement fe && fe.DataContext is AxisViewModel axis)
			{
				axis.JSpeedM_On = !axis.JSpeedM_On;
				return;
			}
			MessageBox.Show("JSpeed Medium pressed");
		}

		private void OnHomeClick(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement fe && fe.DataContext is AxisViewModel axis)
			{
				axis.Command_Home();
				return;
			}
			MessageBox.Show("Home pressed");
		}

		private void OnStopClick(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement fe && fe.DataContext is AxisViewModel axis)
			{
				axis.Command_Stop();
				return;
			}
			MessageBox.Show("Stop pressed");
		}

		private void OnCommandClick(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement fe && fe.DataContext is AxisViewModel axis)
			{
				axis.Command_Command();
				return;
			}
			MessageBox.Show("Command pressed");
		}

		private void OnServoOnClick(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement fe && fe.DataContext is AxisViewModel axis)
			{
				axis.ServoOn_On = !axis.ServoOn_On;
				return;
			}
			MessageBox.Show("Servo On pressed");
		}

		private void OnAlarmResetClick(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement fe && fe.DataContext is AxisViewModel axis)
			{
				axis.Command_AlarmReset();
				return;
			}
			MessageBox.Show("Alarm Reset pressed");
		}

		// Numeric-only input handlers
		private static readonly Regex _numericRegex = new("^[0-9]+$");
		private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !_numericRegex.IsMatch(e.Text);
		}

		private void NumberOnly_Pasting(object sender, DataObjectPastingEventArgs e)
		{
			if (e.DataObject.GetDataPresent(DataFormats.Text))
			{
				var text = e.DataObject.GetData(DataFormats.Text) as string;
				if (string.IsNullOrEmpty(text) || !_numericRegex.IsMatch(text))
				{
					e.CancelCommand();
				}
			}
			else
			{
				e.CancelCommand();
			}
		}

		// Signed number handlers: allow optional leading '-' only at position0
		private static readonly Regex _signedNumericFragment = new("^-?[0-9]*$");
		private void SignedNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			if (sender is TextBox tb)
			{
				var full = tb.Text;
				var selStart = tb.SelectionStart;
				var selLength = tb.SelectionLength;
				var newText = full.Remove(selStart, selLength).Insert(selStart, e.Text);
				// allow only digits and a single leading '-'
				e.Handled = !_signedNumericFragment.IsMatch(newText);
			}
			else
			{
				e.Handled = true;
			}
		}

		private void SignedNumber_Pasting(object sender, DataObjectPastingEventArgs e)
		{
			if (e.DataObject.GetDataPresent(DataFormats.Text))
			{
				var pasteText = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
				if (sender is TextBox tb)
				{
					var full = tb.Text;
					var selStart = tb.SelectionStart;
					var selLength = tb.SelectionLength;
					var newText = full.Remove(selStart, selLength).Insert(selStart, pasteText);
					if (!_signedNumericFragment.IsMatch(newText))
					{
						e.CancelCommand();
					}
				}
				else
				{
					e.CancelCommand();
				}
			}
			else
			{
				e.CancelCommand();
			}
		}

		private void OnCommandButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (sender is Button btn && !_pressedCommandButtons.Contains(btn))
			{
				_pressedCommandButtons.Add(btn);
				if (btn.DataContext is AxisViewModel axis)
				{
					var tag = (btn.Tag as string) ?? string.Empty;
					switch (tag)
					{
						case "JSpeedH":
							axis.JSpeedH_On = !axis.JSpeedH_On;
							break;
						case "JSpeedM":
							axis.JSpeedM_On = !axis.JSpeedM_On;
							break;
						case "ServoOn":
							axis.ServoOn_On = !axis.ServoOn_On;
							break;
					}
				}
				// mark handled so other mouse events don't also fire
				e.Handled = true;
			}
		}

		private void OnCommandButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (sender is Button btn)
			{
				// allow next press
				_pressedCommandButtons.Remove(btn);
				// mark handled
				e.Handled = true;
			}
		}

		private void OnActionButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (sender is Button btn && !_pressedCommandButtons.Contains(btn))
			{
				_pressedCommandButtons.Add(btn);
				if (btn.DataContext is AxisViewModel axis)
				{
					var tag = (btn.Tag as string) ?? string.Empty;
					switch (tag)
					{
						case "JogP": axis.SetMomentaryCommand(MomentaryCommand.JogP, true); break;
						case "JogN": axis.SetMomentaryCommand(MomentaryCommand.JogN, true); break;
						case "Home": axis.SetMomentaryCommand(MomentaryCommand.Home, true); break;
						case "Stop": axis.SetMomentaryCommand(MomentaryCommand.Stop, true); break;
						case "Command": axis.SetMomentaryCommand(MomentaryCommand.Command, true); break;
						case "AlarmReset": axis.SetMomentaryCommand(MomentaryCommand.AlarmReset, true); break;
					}
				}
				e.Handled = true;
			}
		}

		private void OnActionButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (sender is Button btn)
			{
				_pressedCommandButtons.Remove(btn);
				if (btn.DataContext is AxisViewModel axis)
				{
					var tag = (btn.Tag as string) ?? string.Empty;
					switch (tag)
					{
						case "JogP": axis.SetMomentaryCommand(MomentaryCommand.JogP, false); break;
						case "JogN": axis.SetMomentaryCommand(MomentaryCommand.JogN, false); break;
						case "Home": axis.SetMomentaryCommand(MomentaryCommand.Home, false); break;
						case "Stop": axis.SetMomentaryCommand(MomentaryCommand.Stop, false); break;
						case "Command": axis.SetMomentaryCommand(MomentaryCommand.Command, false); break;
						case "AlarmReset": axis.SetMomentaryCommand(MomentaryCommand.AlarmReset, false); break;
					}
				}
				e.Handled = true;
			}
		}
	}

	public enum MomentaryCommand { JogP, JogN, Home, Stop, Command, AlarmReset }

	public class IndicatorItem : INotifyPropertyChanged
	{
		public string? Description { get; set; }
		private bool _isOn;
		public bool IsOn
		{
			get => _isOn;
			set => SetField(ref _isOn, value, nameof(IsOn));
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		protected bool SetField<T>(ref T field, T value, string propertyName)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}
	}

	public class AxisViewModel : INotifyPropertyChanged
	{
		public string? Header { get; set; }
		public ObservableCollection<IndicatorItem> AlarmItems { get; set; } = new();
		public ObservableCollection<IndicatorItem> StatusItems { get; set; } = new();

		private int _currentPosition;
		public int CurrentPosition
		{
			get => _currentPosition;
			set => SetField(ref _currentPosition, value, nameof(CurrentPosition));
		}

		private string _targetPositionText = "0";
		public string TargetPositionText
		{
			get => _targetPositionText;
			set
			{
				// ensure empty maps to "0"
				var normalized = string.IsNullOrWhiteSpace(value) ? "0" : value;
				if (SetField(ref _targetPositionText, normalized, nameof(TargetPositionText)))
				{
					if (_suspendWrite) return;
					if (int.TryParse(_targetPositionText, out var v))
					{
						// write to PLC operator move info
						if (_op != null && _axisIndex >=0)
						{
							switch (_axisIndex)
							{
								case 0: _op.Command_Axis1Pos = v; break;
								case 1: _op.Command_Axis2Pos = v; break;
								case 2: _op.Command_Axis3Pos = v; break;
								case 3: _op.Command_Axis4Pos = v; break;
							}
						}
					}
				}
			}
		}

		private string _speedText = "0";
		public string SpeedText
		{
			get => _speedText;
			set
			{
				var normalized = string.IsNullOrWhiteSpace(value) ? "0" : value;
				if (SetField(ref _speedText, normalized, nameof(SpeedText)))
				{
					if (_suspendWrite) return;
					if (int.TryParse(_speedText, out var v))
					{
						if (_op != null && _axisIndex >=0)
						{
							switch (_axisIndex)
							{
								case 0: _op.Command_Axis1Speed = v; break;
								case 1: _op.Command_Axis2Speed = v; break;
								case 2: _op.Command_Axis3Speed = v; break;
								case 3: _op.Command_Axis4Speed = v; break;
							}
						}
					}
				}
			}
		}

		// Command-related properties
		private bool _jSpeedH_on;
		public bool JSpeedH_On
		{
			get => _jSpeedH_on;
			set
			{
				// write-and-verify/pulse behavior for more reliable operator writes
				if (SetField(ref _jSpeedH_on, value, nameof(JSpeedH_On)))
				{
					_ = WriteAndVerifyCommandAsync(CommandType.JSpeedH, value, latched: true);
				}
			}
		}

		private bool _jSpeedM_on;
		public bool JSpeedM_On
		{
			get => _jSpeedM_on;
			set
			{
				if (SetField(ref _jSpeedM_on, value, nameof(JSpeedM_On)))
				{
					_ = WriteAndVerifyCommandAsync(CommandType.JSpeedM, value, latched: true);
				}
			}
		}

		private bool _servoOn_on;
		public bool ServoOn_On
		{
			get => _servoOn_on;
			set
			{
				if (SetField(ref _servoOn_on, value, nameof(ServoOn_On)))
				{
					_ = WriteAndVerifyCommandAsync(CommandType.ServoOn, value, latched: true);
				}
			}
		}

		private bool _suspendWrite = false;
		private IPLCOperator? _op;
		private int _axisIndex = -1;

		public void SetOperator(IPLCOperator op, int axisIndex)
		{
			_op = op;
			_axisIndex = axisIndex;
			RefreshCommandPropertiesFromOperator();

			// Initialize target and speed text from operator values while suspending writes
			_suspendWrite = true;
			switch (_axisIndex)
			{
				case 0:
					TargetPositionText = _op.Command_Axis1Pos.ToString();
					SpeedText = _op.Command_Axis1Speed.ToString();
					break;
				case 1:
					TargetPositionText = _op.Command_Axis2Pos.ToString();
					SpeedText = _op.Command_Axis2Speed.ToString();
					break;
				case 2:
					TargetPositionText = _op.Command_Axis3Pos.ToString();
					SpeedText = _op.Command_Axis3Speed.ToString();
					break;
				case 3:
					TargetPositionText = _op.Command_Axis4Pos.ToString();
					SpeedText = _op.Command_Axis4Speed.ToString();
					break;
			}
			_suspendWrite = false;
		}

		private enum CommandType { JSpeedH, JSpeedM, ServoOn }

		private void WriteRaw(CommandType type, bool value)
		{
			if (_op == null) return;
			switch (_axisIndex)
			{
				case 0:
					switch (type)
					{
						case CommandType.JSpeedH: _op.Command_Axis1JogSpeedH = value; break;
						case CommandType.JSpeedM: _op.Command_Axis1JogSpeedM = value; break;
						case CommandType.ServoOn: _op.Command_Axis1ServoOn = value; break;
					}
					break;
				case 1:
					switch (type)
					{
						case CommandType.JSpeedH: _op.Command_Axis2JogSpeedH = value; break;
						case CommandType.JSpeedM: _op.Command_Axis2JogSpeedM = value; break;
						case CommandType.ServoOn: _op.Command_Axis2ServoOn = value; break;
					}
					break;
				case 2:
					switch (type)
					{
						case CommandType.JSpeedH: _op.Command_Axis3JogSpeedH = value; break;
						case CommandType.JSpeedM: _op.Command_Axis3JogSpeedM = value; break;
						case CommandType.ServoOn: _op.Command_Axis3ServoOn = value; break;
					}
					break;
				case 3:
					switch (type)
					{
						case CommandType.JSpeedH: _op.Command_Axis4JogSpeedH = value; break;
						case CommandType.JSpeedM: _op.Command_Axis4JogSpeedM = value; break;
						case CommandType.ServoOn: _op.Command_Axis4ServoOn = value; break;
					}
					break;
			}
		}

		private bool ReadRaw(CommandType type)
		{
			if (_op == null) return false;
			switch (_axisIndex)
			{
				case 0:
					switch (type)
					{
						case CommandType.JSpeedH: return _op.Command_Axis1JogSpeedH;
						case CommandType.JSpeedM: return _op.Command_Axis1JogSpeedM;
						case CommandType.ServoOn: return _op.Command_Axis1ServoOn;
					}
					break;
				case 1:
					switch (type)
					{
						case CommandType.JSpeedH: return _op.Command_Axis2JogSpeedH;
						case CommandType.JSpeedM: return _op.Command_Axis2JogSpeedM;
						case CommandType.ServoOn: return _op.Command_Axis2ServoOn;
					}
					break;
				case 2:
					switch (type)
					{
						case CommandType.JSpeedH: return _op.Command_Axis3JogSpeedH;
						case CommandType.JSpeedM: return _op.Command_Axis3JogSpeedM;
						case CommandType.ServoOn: return _op.Command_Axis3ServoOn;
					}
					break;
				case 3:
					switch (type)
					{
						case CommandType.JSpeedH: return _op.Command_Axis4JogSpeedH;
						case CommandType.JSpeedM: return _op.Command_Axis4JogSpeedM;
						case CommandType.ServoOn: return _op.Command_Axis4ServoOn;
					}
					break;
			}
			return false;
		}

		// command-tracking to avoid transient refresh overwrites
		private CommandType? _lastCommandInProgress = null;

		private async Task WriteAndVerifyCommandAsync(CommandType type, bool value, bool latched)
		{
			if (_op == null) return;

			_lastCommandInProgress = type;
			try
			{
				// write requested value
				WriteRaw(type, value);

				if (!latched)
				{
					// Pulse commands: keep true briefly then clear
					await Task.Delay(150);
					WriteRaw(type, false);

					// ensure UI shows the pulse: raise properties on UI thread
					Application.Current.Dispatcher.Invoke(() =>
					{
						// set local property to false if the operator did not latch it
						var opVal = ReadRaw(type);
						if (!opVal)
						{
							switch (type)
							{
								case CommandType.JSpeedH: _jSpeedH_on = false; OnPropertyChanged(nameof(JSpeedH_On)); break;
								case CommandType.JSpeedM: _jSpeedM_on = false; OnPropertyChanged(nameof(JSpeedM_On)); break;
							}
						}
					});
				}
				else
				{
					// Latched command (e.g. Servo On): retry and verify it became the expected value
					const int retries =5;
					const int delayMs =200;
					for (int i =0; i < retries; i++)
					{
						await Task.Delay(delayMs);
						var opVal = ReadRaw(type);
						if (opVal == value) break; // success
						// retry write
						WriteRaw(type, value);
					}
					// final read and sync local property
					var final = ReadRaw(type);
					Application.Current.Dispatcher.Invoke(() =>
					{
						switch (type)
						{
							case CommandType.JSpeedH:
								_jSpeedH_on = final; OnPropertyChanged(nameof(JSpeedH_On)); break;
							case CommandType.JSpeedM:
								_jSpeedM_on = final; OnPropertyChanged(nameof(JSpeedM_On)); break;
							case CommandType.ServoOn:
								_servoOn_on = final; OnPropertyChanged(nameof(ServoOn_On)); break;
						}
					});
				}
			}
			finally
			{
				_lastCommandInProgress = null;
			}
		}

		private void RefreshCommandPropertiesFromOperator()
		{
			if (_op == null) return;
			_suspendWrite = true;
			switch (_axisIndex)
			{
				case 0:
					_jSpeedH_on = _op.Command_Axis1JogSpeedH;
					_jSpeedM_on = _op.Command_Axis1JogSpeedM;
					// if a JSpeed command is in progress, skip updating ServoOn to avoid transient overwrite
					if (_lastCommandInProgress != CommandType.JSpeedH && _lastCommandInProgress != CommandType.JSpeedM)
						_servoOn_on = _op.Command_Axis1ServoOn;
					break;
				case 1:
					_jSpeedH_on = _op.Command_Axis2JogSpeedH;
					_jSpeedM_on = _op.Command_Axis2JogSpeedM;
					if (_lastCommandInProgress != CommandType.JSpeedH && _lastCommandInProgress != CommandType.JSpeedM)
						_servoOn_on = _op.Command_Axis2ServoOn;
					break;
				case 2:
					_jSpeedH_on = _op.Command_Axis3JogSpeedH;
					_jSpeedM_on = _op.Command_Axis3JogSpeedM;
					if (_lastCommandInProgress != CommandType.JSpeedH && _lastCommandInProgress != CommandType.JSpeedM)
						_servoOn_on = _op.Command_Axis3ServoOn;
					break;
				case 3:
					_jSpeedH_on = _op.Command_Axis4JogSpeedH;
					_jSpeedM_on = _op.Command_Axis4JogSpeedM;
					if (_lastCommandInProgress != CommandType.JSpeedH && _lastCommandInProgress != CommandType.JSpeedM)
						_servoOn_on = _op.Command_Axis4ServoOn;
					break;
			}
			// raise change notifications
			OnPropertyChanged(nameof(JSpeedH_On));
			OnPropertyChanged(nameof(JSpeedM_On));
			OnPropertyChanged(nameof(ServoOn_On));
			_suspendWrite = false;
		}

		// expose a public refresh method used by viewmodel timer
		public void RefreshCommands()
		{
			RefreshCommandPropertiesFromOperator();
		}

		// Update from operator (positions and indicators)
		public void UpdateFromOperator()
		{
			if (_op == null) return;
			switch (_axisIndex)
			{
				case 0:
					CurrentPosition = _op.Axis1Pos;
					if (AlarmItems.Count >=5)
					{
						AlarmItems[0].IsOn = _op.Axis1ErrorAlarm;
						AlarmItems[1].IsOn = _op.Axis1ErrorLimitN;
						AlarmItems[2].IsOn = _op.Axis1ErrorLimitP;
						AlarmItems[3].IsOn = _op.Axis1ErrorHomeTimeout;
						AlarmItems[4].IsOn = _op.Axis1ErrorCommandTimeout;
					}
					if (StatusItems.Count >=5)
					{
						StatusItems[0].IsOn = _op.Axis1HomeComplete;
						StatusItems[1].IsOn = _op.Axis1HomeProcedure;
						StatusItems[2].IsOn = _op.Axis1CommandProcedure;
						StatusItems[3].IsOn = _op.Axis1CommandDriving;
						StatusItems[4].IsOn = _op.Axis1OutputPulseStop;
					}
					break;
				case 1:
					CurrentPosition = _op.Axis2Pos;
					if (AlarmItems.Count >=5)
					{
						AlarmItems[0].IsOn = _op.Axis2ErrorAlarm;
						AlarmItems[1].IsOn = _op.Axis2ErrorLimitN;
						AlarmItems[2].IsOn = _op.Axis2ErrorLimitP;
						AlarmItems[3].IsOn = _op.Axis2ErrorHomeTimeout;
						AlarmItems[4].IsOn = _op.Axis2ErrorCommandTimeout;
					}
					if (StatusItems.Count >=5)
					{
						StatusItems[0].IsOn = _op.Axis2HomeComplete;
						StatusItems[1].IsOn = _op.Axis2HomeProcedure;
						StatusItems[2].IsOn = _op.Axis2CommandProcedure;
						StatusItems[3].IsOn = _op.Axis2CommandDriving;
						StatusItems[4].IsOn = _op.Axis2OutputPulseStop;
					}
					break;
				case 2:
					CurrentPosition = _op.Axis3Pos;
					if (AlarmItems.Count >=5)
					{
						AlarmItems[0].IsOn = _op.Axis3ErrorAlarm;
						AlarmItems[1].IsOn = _op.Axis3ErrorLimitN;
						AlarmItems[2].IsOn = _op.Axis3ErrorLimitP;
						AlarmItems[3].IsOn = _op.Axis3ErrorHomeTimeout;
						AlarmItems[4].IsOn = _op.Axis3ErrorCommandTimeout;
					}
					if (StatusItems.Count >=5)
					{
						StatusItems[0].IsOn = _op.Axis3HomeComplete;
						StatusItems[1].IsOn = _op.Axis3HomeProcedure;
						StatusItems[2].IsOn = _op.Axis3CommandProcedure;
						StatusItems[3].IsOn = _op.Axis3CommandDriving;
					 StatusItems[4].IsOn = _op.Axis3OutputPulseStop;
					}
					break;
				case 3:
					CurrentPosition = _op.Axis4Pos;
					if (AlarmItems.Count >=5)
					{
						AlarmItems[0].IsOn = _op.Axis4ErrorAlarm;
						AlarmItems[1].IsOn = _op.Axis4ErrorLimitN;
						AlarmItems[2].IsOn = _op.Axis4ErrorLimitP;
						AlarmItems[3].IsOn = _op.Axis4ErrorHomeTimeout;
						AlarmItems[4].IsOn = _op.Axis4ErrorCommandTimeout;
					}
					if (StatusItems.Count >=5)
					{
						StatusItems[0].IsOn = _op.Axis4HomeComplete;
						StatusItems[1].IsOn = _op.Axis4HomeProcedure;
						StatusItems[2].IsOn = _op.Axis4CommandProcedure;
						StatusItems[3].IsOn = _op.Axis4CommandDriving;
						StatusItems[4].IsOn = _op.Axis4OutputPulseStop;
					}
					break;
			}
		}

		// Placeholder command actions
		public void Command_JogPlus()
		{
			if (_op == null) return;
			switch (_axisIndex)
			{
				case 0: _op.Command_Axis1JogP = true; break;
				case 1: _op.Command_Axis2JogP = true; break;
				case 2: _op.Command_Axis3JogP = true; break;
				case 3: _op.Command_Axis4JogP = true; break;
			}
		}

		public void Command_JogMinus()
		{
			if (_op == null) return;
			switch (_axisIndex)
			{
				case 0: _op.Command_Axis1JogN = true; break;
				case 1: _op.Command_Axis2JogN = true; break;
				case 2: _op.Command_Axis3JogN = true; break;
				case 3: _op.Command_Axis4JogN = true; break;
			}
		}

		public void Command_Home()
		{
			if (_op == null) return;
			switch (_axisIndex)
			{
				case 0: _op.Command_Axis1Home = true; break;
				case 1: _op.Command_Axis2Home = true; break;
				case 2: _op.Command_Axis3Home = true; break;
				case 3: _op.Command_Axis4Home = true; break;
			}
		}

		public void Command_Stop()
		{
			if (_op == null) return;
			switch (_axisIndex)
			{
				case 0: _op.Command_Axis1Stop = true; break;
				case 1: _op.Command_Axis2Stop = true; break;
				case 2: _op.Command_Axis3Stop = true; break;
				case 3: _op.Command_Axis4Stop = true; break;
			}
		}

		public void Command_Command()
		{
			if (_op == null) return;
			switch (_axisIndex)
			{
				case 0: _op.Command_Axis1Command = true; break;
				case 1: _op.Command_Axis2Command = true; break;
				case 2: _op.Command_Axis3Command = true; break;
				case 3: _op.Command_Axis4Command = true; break;
			}
		}

		public void Command_AlarmReset()
		{
			if (_op == null) return;
			switch (_axisIndex)
			{
				case 0: _op.Command_Axis1AlarmReset = true; break;
				case 1: _op.Command_Axis2AlarmReset = true; break;
				case 2: _op.Command_Axis3AlarmReset = true; break;
				case 3: _op.Command_Axis4AlarmReset = true; break;
			}
		}

		public void SetMomentaryCommand(MomentaryCommand cmd, bool value)
		{
			if (_op == null) return;
			switch (_axisIndex)
			{
				case 0:
					switch (cmd)
					{
						case MomentaryCommand.JogP: _op.Command_Axis1JogP = value; break;
						case MomentaryCommand.JogN: _op.Command_Axis1JogN = value; break;
						case MomentaryCommand.Home: _op.Command_Axis1Home = value; break;
						case MomentaryCommand.Stop: _op.Command_Axis1Stop = value; break;
						case MomentaryCommand.Command: _op.Command_Axis1Command = value; break;
						case MomentaryCommand.AlarmReset: _op.Command_Axis1AlarmReset = value; break;
					}
					break;
				case 1:
					switch (cmd)
					{
						case MomentaryCommand.JogP: _op.Command_Axis2JogP = value; break;
						case MomentaryCommand.JogN: _op.Command_Axis2JogN = value; break;
						case MomentaryCommand.Home: _op.Command_Axis2Home = value; break;
						case MomentaryCommand.Stop: _op.Command_Axis2Stop = value; break;
						case MomentaryCommand.Command: _op.Command_Axis2Command = value; break;
						case MomentaryCommand.AlarmReset: _op.Command_Axis2AlarmReset = value; break;
					}
					break;
				case 2:
					switch (cmd)
					{
						case MomentaryCommand.JogP: _op.Command_Axis3JogP = value; break;
						case MomentaryCommand.JogN: _op.Command_Axis3JogN = value; break;
						case MomentaryCommand.Home: _op.Command_Axis3Home = value; break;
						case MomentaryCommand.Stop: _op.Command_Axis3Stop = value; break;
						case MomentaryCommand.Command: _op.Command_Axis3Command = value; break;
						case MomentaryCommand.AlarmReset: _op.Command_Axis3AlarmReset = value; break;
					}
					break;
				case 3:
					switch (cmd)
					{
						case MomentaryCommand.JogP: _op.Command_Axis4JogP = value; break;
						case MomentaryCommand.JogN: _op.Command_Axis4JogN = value; break;
						case MomentaryCommand.Home: _op.Command_Axis4Home = value; break;
						case MomentaryCommand.Stop: _op.Command_Axis4Stop = value; break;
						case MomentaryCommand.Command: _op.Command_Axis4Command = value; break;
						case MomentaryCommand.AlarmReset: _op.Command_Axis4AlarmReset = value; break;
					}
					break;
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		protected bool SetField<T>(ref T field, T value, string propertyName)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}
	}

	public class MotorStatusViewModel
	{
		public ObservableCollection<AxisViewModel> Axes { get; set; } = new();

		private readonly IPLCOperator? _op;
		private DispatcherTimer? _timer;

		public MotorStatusViewModel()
		{
			// try to resolve PLC operator if available
			try
			{
				if (App.AppHost != null)
				{
					_op = App.AppHost.Services.GetService(typeof(IPLCOperator)) as IPLCOperator;
				}
			}
			catch { /* ignore resolution errors and fall back to defaults */ }

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
					Header = headers[i]
				};

				for (int j =0; j <5; j++) axis.AlarmItems.Add(new IndicatorItem { Description = string.Empty, IsOn = false });
				for (int j =0; j <5; j++) axis.StatusItems.Add(new IndicatorItem { Description = string.Empty, IsOn = false });

				axis.AlarmItems[0].Description = "軸異常";
				axis.AlarmItems[1].Description = "負極限";
				axis.AlarmItems[2].Description = "正極限";
				axis.AlarmItems[3].Description = "Home Timeout";
				axis.AlarmItems[4].Description = "Command Timeout";

				axis.StatusItems[0].Description = "原點復歸完成";
				axis.StatusItems[1].Description = "Home 流程";
				axis.StatusItems[2].Description = "Command 流程";
				axis.StatusItems[3].Description = "定位指令驅動中";
				axis.StatusItems[4].Description = "脈衝輸出即時停止";

				if (_op != null)
				{
					axis.SetOperator(_op, i);
					axis.UpdateFromOperator();
					axis.RefreshCommands();
				}
				else
				{
					axis.CurrentPosition =1234;
					axis.AlarmItems[0].IsOn = false;
					axis.AlarmItems[1].IsOn = false;
					axis.AlarmItems[2].IsOn = false;
					axis.AlarmItems[3].IsOn = false;
					axis.AlarmItems[4].IsOn = false;

					axis.StatusItems[0].IsOn = true;
					axis.StatusItems[1].IsOn = false;
					axis.StatusItems[2].IsOn = false;
					axis.StatusItems[3].IsOn = false;
					axis.StatusItems[4].IsOn = false;
				}

				Axes.Add(axis);
			}

			if (_op != null)
			{
				_timer = new DispatcherTimer(DispatcherPriority.Normal)
				{
					Interval = TimeSpan.FromMilliseconds(200)
				};
				_timer.Tick += (s, e) =>
				{
					for (int i =0; i < Axes.Count; i++)
					{
						Axes[i].UpdateFromOperator();
						Axes[i].RefreshCommands();
					}
				};
				_timer.Start();
			}
		}
	}
}
