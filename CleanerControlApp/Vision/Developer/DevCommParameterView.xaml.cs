using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Controls;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows;
using CleanerControlApp.Utilities;

namespace CleanerControlApp.Vision.Developer
{
 /// <summary>
 /// Interaction logic for DevCommParameterView.xaml
 /// </summary>
 public partial class DevCommParameterView : UserControl
 {
 private CommunicationSettings _commSettings;

 // Default constructor uses ConfigLoader to obtain settings (fallback for DI)
 public DevCommParameterView() : this(ConfigLoader.GetCommunicationSettings())
 {
 }

 // DI-friendly constructor
 public DevCommParameterView(CommunicationSettings commSettings)
 {
 InitializeComponent();
 _commSettings = commSettings ?? new CommunicationSettings();

 // populate UI from settings
 LoadModbusTcpParametersFromSettings();
 LoadModbusRtuParameters();
 }

 private void BtnLoadParams_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 // Refresh UI from current DI-provided CommunicationSettings (do not read from file)
 LoadModbusTcpParametersFromSettings();
 LoadModbusRtuParameters();
 }

 private void BtnSaveParams_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try
 {
 // Update _commSettings from UI only (do not write to appsettings.json here)
 if (_commSettings == null) _commSettings = new CommunicationSettings();

 if (_commSettings.ModbusTCPParameter == null) _commSettings.ModbusTCPParameter = new ModbusTCPParameter();
 _commSettings.ModbusTCPParameter.IP = TxtModbusIP?.Text ?? "";
 if (int.TryParse(TxtModbusPort?.Text, out var tcpPort)) _commSettings.ModbusTCPParameter.Port = tcpPort;

 // ModbusRTUParameter (main)
 if (_commSettings.ModbusRTUParameter == null) _commSettings.ModbusRTUParameter = new ModbusRTUParameter();
 _commSettings.ModbusRTUParameter.PortName = cmbRTUPortName?.SelectedItem?.ToString() ?? "";
 if (int.TryParse(cmbRTUBaudRate?.SelectedItem?.ToString(), out var baud)) _commSettings.ModbusRTUParameter.BaudRate = baud;
 _commSettings.ModbusRTUParameter.Parity = cmbRTUParity?.SelectedItem?.ToString() ?? "";
 if (int.TryParse(cmbRTUDataBits?.SelectedItem?.ToString(), out var db)) _commSettings.ModbusRTUParameter.DataBits = db;
 if (double.TryParse(cmbRTUStopBit?.SelectedItem?.ToString(), out var sb))
 {
 // store StopBits as int if integral
 _commSettings.ModbusRTUParameter.StopBits = (int)sb;
 }

 // ModbusRTUPoolParameter
 var poolList = new List<ModbusRTUParameter>();
 for (int i =0; i <4; i++)
 {
 ComboBox portCb = i ==0 ? cmbPool1PortName : i ==1 ? cmbPool2PortName : i ==2 ? cmbPool3PortName : cmbPool4PortName;
 ComboBox baudCb = i ==0 ? cmbPool1BaudRate : i ==1 ? cmbPool2BaudRate : i ==2 ? cmbPool3BaudRate : cmbPool4BaudRate;
 ComboBox parityCb = i ==0 ? cmbPool1Parity : i ==1 ? cmbPool2Parity : i ==2 ? cmbPool3Parity : cmbPool4Parity;
 ComboBox dataCb = i ==0 ? cmbPool1DataBits : i ==1 ? cmbPool2DataBits : i ==2 ? cmbPool3DataBits : cmbPool4DataBits;
 ComboBox stopCb = i ==0 ? cmbPool1StopBit : i ==1 ? cmbPool2StopBit : i ==2 ? cmbPool3StopBit : cmbPool4StopBit;

 var param = new ModbusRTUParameter();
 param.PortName = portCb?.SelectedItem?.ToString() ?? "";
 if (int.TryParse(baudCb?.SelectedItem?.ToString(), out var pbaud)) param.BaudRate = pbaud;
 param.Parity = parityCb?.SelectedItem?.ToString() ?? "";
 if (int.TryParse(dataCb?.SelectedItem?.ToString(), out var pdata)) param.DataBits = pdata;
 if (double.TryParse(stopCb?.SelectedItem?.ToString(), out var pstop)) { param.StopBits = (int)pstop; }

 poolList.Add(param);
 }

 _commSettings.ModbusRTUPoolParameter = poolList;

 // Do not persist to file here. Caller/host can choose to persist using ConfigLoader.Save()/SetCommunicationSettings
 MessageBox.Show("把计穝 CommunicationSettingsン叫パ糷∕﹚纗砞﹚郎", "糶把计", MessageBoxButton.OK, MessageBoxImage.Information);
 }
 catch (System.Exception ex)
 {
 MessageBox.Show("糶把计ア毖: " + ex.Message, "岿粇", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }

 private void LoadModbusTcpParametersFromSettings()
 {
 try
 {
 if (_commSettings?.ModbusTCPParameter != null)
 {
 TxtModbusIP.Text = _commSettings.ModbusTCPParameter.IP ?? "";
 TxtModbusPort.Text = _commSettings.ModbusTCPParameter.Port.ToString();
 }
 }
 catch
 {
 // ignore and keep defaults
 }
 }

 private void LoadModbusRtuParameters()
 {
 // Default option lists
 var defaultBaudRates = new List<string> { "110", "300", "600", "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200", "230400" };
 var defaultParityOptions = new List<string> { "None", "Odd", "Even", "Mark", "Space" };
 var defaultDataBitsOptions = new List<string> { "5", "6", "7", "8" };
 var defaultStopBitsOptions = new List<string> { "1", "1.5", "2" };

 try
 {
 // Port names come only from the system serial ports
 var systemPorts = new List<string>(SerialPort.GetPortNames());

 var baudSet = new HashSet<string>(defaultBaudRates);
 var paritySet = new HashSet<string>(defaultParityOptions);
 var dataBitsSet = new HashSet<string>(defaultDataBitsOptions);
 var stopBitsSet = new HashSet<string>(defaultStopBitsOptions);

 // Prepare per-pool selected value holders (up to4)
 var poolPortNameVals = new string[4];
 var poolBaudVals = new string[4];
 var poolParityVals = new string[4];
 var poolDataBitsVals = new string[4];
 var poolStopBitVals = new string[4];

 // If we have settings loaded, incorporate them
 var comm = _commSettings;
 if (comm != null)
 {
 if (comm.ModbusRTUPoolParameter != null)
 {
 for (int idx =0; idx < comm.ModbusRTUPoolParameter.Count && idx <4; idx++)
 {
 var item = comm.ModbusRTUPoolParameter[idx];
 if (item != null)
 {
 poolPortNameVals[idx] = item.PortName ?? "";
 poolBaudVals[idx] = item.BaudRate.ToString();
 poolParityVals[idx] = item.Parity ?? "";
 poolDataBitsVals[idx] = item.DataBits.ToString();
 poolStopBitVals[idx] = item.StopBits.ToString();

 baudSet.Add(poolBaudVals[idx]);
 if (!string.IsNullOrEmpty(poolParityVals[idx])) paritySet.Add(poolParityVals[idx]);
 if (!string.IsNullOrEmpty(poolDataBitsVals[idx])) dataBitsSet.Add(poolDataBitsVals[idx]);
 if (!string.IsNullOrEmpty(poolStopBitVals[idx])) stopBitsSet.Add(poolStopBitVals[idx]);
 }
 }
 }

 // main RTU parameter values
 string portNameVal = null, baudVal = null, parityVal = null, dataBitsVal = null, stopBitVal = null;
 if (comm.ModbusRTUParameter != null)
 {
 portNameVal = comm.ModbusRTUParameter.PortName;
 baudVal = comm.ModbusRTUParameter.BaudRate.ToString();
 parityVal = comm.ModbusRTUParameter.Parity;
 dataBitsVal = comm.ModbusRTUParameter.DataBits.ToString();
 stopBitVal = comm.ModbusRTUParameter.StopBits.ToString();

 if (!string.IsNullOrEmpty(baudVal)) baudSet.Add(baudVal);
 if (!string.IsNullOrEmpty(parityVal)) paritySet.Add(parityVal);
 if (!string.IsNullOrEmpty(dataBitsVal)) dataBitsSet.Add(dataBitsVal);
 if (!string.IsNullOrEmpty(stopBitVal)) stopBitsSet.Add(stopBitVal);
 }

 // Also include system ports into pool port lists
 }

 // Build lists for itemsources
 var portNameListForMain = new List<string>(systemPorts);
 // ensure configured main RTU PortName is included so it can be selected even if not present in system ports
 if (comm?.ModbusRTUParameter != null && !string.IsNullOrEmpty(comm.ModbusRTUParameter.PortName) && !portNameListForMain.Contains(comm.ModbusRTUParameter.PortName))
 {
 portNameListForMain.Add(comm.ModbusRTUParameter.PortName);
 }
 var baudList = new List<string>(baudSet);
 var parityList = new List<string>(paritySet);
 var dataBitsList = new List<string>(dataBitsSet);
 var stopBitsList = new List<string>(stopBitsSet);

 // Sort lists
 portNameListForMain.Sort();
 baudList.Sort((a, b) =>
 {
 if (long.TryParse(a, out var ai) && long.TryParse(b, out var bi)) return ai.CompareTo(bi);
 return string.Compare(a, b);
 });
 parityList.Sort();
 dataBitsList.Sort((a, b) => int.Parse(a).CompareTo(int.Parse(b)));
 stopBitsList.Sort();

 // Assign items to main combo boxes
 cmbRTUPortName.ItemsSource = portNameListForMain;
 cmbRTUBaudRate.ItemsSource = baudList;
 cmbRTUParity.ItemsSource = parityList;
 cmbRTUDataBits.ItemsSource = dataBitsList;
 cmbRTUStopBit.ItemsSource = stopBitsList;

 // Assign items and selected values for each pool control (1..4)
 for (int i =0; i <4; i++)
 {
 // prepare port list for this pool: system ports plus the pool's configured portName (if any and not present)
 var poolPortList = new List<string>(systemPorts);
 if (!string.IsNullOrEmpty(poolPortNameVals[i]) && !poolPortList.Contains(poolPortNameVals[i])) poolPortList.Add(poolPortNameVals[i]);
 poolPortList.Sort();

 // pick corresponding controls
 ComboBox poolPortCb = i ==0 ? cmbPool1PortName : i ==1 ? cmbPool2PortName : i ==2 ? cmbPool3PortName : cmbPool4PortName;
 ComboBox poolBaudCb = i ==0 ? cmbPool1BaudRate : i ==1 ? cmbPool2BaudRate : i ==2 ? cmbPool3BaudRate : cmbPool4BaudRate;
 ComboBox poolParityCb = i ==0 ? cmbPool1Parity : i ==1 ? cmbPool2Parity : i ==2 ? cmbPool3Parity : cmbPool4Parity;
 ComboBox poolDataCb = i ==0 ? cmbPool1DataBits : i ==1 ? cmbPool2DataBits : i ==2 ? cmbPool3DataBits : cmbPool4DataBits;
 ComboBox poolStopCb = i ==0 ? cmbPool1StopBit : i ==1 ? cmbPool2StopBit : i ==2 ? cmbPool3StopBit : cmbPool4StopBit;

 if (poolPortCb != null) poolPortCb.ItemsSource = poolPortList;
 if (poolBaudCb != null) poolBaudCb.ItemsSource = baudList;
 if (poolParityCb != null) poolParityCb.ItemsSource = parityList;
 if (poolDataCb != null) poolDataCb.ItemsSource = dataBitsList;
 if (poolStopCb != null) poolStopCb.ItemsSource = stopBitsList;

 // set selected values if present, otherwise choose first
 if (poolPortCb != null)
 {
 if (!string.IsNullOrEmpty(poolPortNameVals[i]) && poolPortCb.Items.Contains(poolPortNameVals[i])) poolPortCb.SelectedItem = poolPortNameVals[i];
 else if (poolPortCb.Items.Count >0) poolPortCb.SelectedIndex =0;
 }
 if (poolBaudCb != null)
 {
 if (!string.IsNullOrEmpty(poolBaudVals[i]) && poolBaudCb.Items.Contains(poolBaudVals[i])) poolBaudCb.SelectedItem = poolBaudVals[i];
 else if (poolBaudCb.Items.Count >0) poolBaudCb.SelectedIndex =0;
 }
 if (poolParityCb != null)
 {
 if (!string.IsNullOrEmpty(poolParityVals[i]) && poolParityCb.Items.Contains(poolParityVals[i])) poolParityCb.SelectedItem = poolParityVals[i];
 else if (poolParityCb.Items.Count >0) poolParityCb.SelectedIndex =0;
 }
 if (poolDataCb != null)
 {
 if (!string.IsNullOrEmpty(poolDataBitsVals[i]) && poolDataCb.Items.Contains(poolDataBitsVals[i])) poolDataCb.SelectedItem = poolDataBitsVals[i];
 else if (poolDataCb.Items.Count >0) poolDataCb.SelectedIndex =0;
 }
 if (poolStopCb != null)
 {
 if (!string.IsNullOrEmpty(poolStopBitVals[i]) && poolStopCb.Items.Contains(poolStopBitVals[i])) poolStopCb.SelectedItem = poolStopBitVals[i];
 else if (poolStopCb.Items.Count >0) poolStopCb.SelectedIndex =0;
 }
 }

 // Now set main RTU selected values (defaults if missing)
 if (_commSettings?.ModbusRTUParameter != null)
 {
 var rtu = _commSettings.ModbusRTUParameter;
 if (!string.IsNullOrEmpty(rtu.PortName) && cmbRTUPortName.Items.Contains(rtu.PortName)) cmbRTUPortName.SelectedItem = rtu.PortName;
 else if (cmbRTUPortName.Items.Count >0) cmbRTUPortName.SelectedIndex =0;

 var baudVal = rtu.BaudRate.ToString();
 if (!string.IsNullOrEmpty(baudVal) && cmbRTUBaudRate.Items.Contains(baudVal)) cmbRTUBaudRate.SelectedItem = baudVal;
 else if (cmbRTUBaudRate.Items.Count >0) cmbRTUBaudRate.SelectedIndex =0;

 if (!string.IsNullOrEmpty(rtu.Parity) && cmbRTUParity.Items.Contains(rtu.Parity)) cmbRTUParity.SelectedItem = rtu.Parity;
 else if (cmbRTUParity.Items.Count >0) cmbRTUParity.SelectedIndex =0;

 var dataBitsVal = rtu.DataBits.ToString();
 if (!string.IsNullOrEmpty(dataBitsVal) && cmbRTUDataBits.Items.Contains(dataBitsVal)) cmbRTUDataBits.SelectedItem = dataBitsVal;
 else if (cmbRTUDataBits.Items.Count >0) cmbRTUDataBits.SelectedIndex =0;

 var stopBitVal = rtu.StopBits.ToString();
 if (!string.IsNullOrEmpty(stopBitVal) && cmbRTUStopBit.Items.Contains(stopBitVal)) cmbRTUStopBit.SelectedItem = stopBitVal;
 else if (cmbRTUStopBit.Items.Count >0) cmbRTUStopBit.SelectedIndex =0;
 }
 else
 {
 // No ModbusRTUParameter: set first items for main and pools
 if (cmbRTUPortName.Items.Count >0) cmbRTUPortName.SelectedIndex =0;
 if (cmbRTUBaudRate.Items.Count >0) cmbRTUBaudRate.SelectedIndex =0;
 if (cmbRTUParity.Items.Count >0) cmbRTUParity.SelectedIndex =0;
 if (cmbRTUDataBits.Items.Count >0) cmbRTUDataBits.SelectedIndex =0;
 if (cmbRTUStopBit.Items.Count >0) cmbRTUStopBit.SelectedIndex =0;

 // pools
 if (cmbPool1PortName.Items.Count >0) cmbPool1PortName.SelectedIndex =0;
 if (cmbPool1BaudRate.Items.Count >0) cmbPool1BaudRate.SelectedIndex =0;
 if (cmbPool1Parity.Items.Count >0) cmbPool1Parity.SelectedIndex =0;
 if (cmbPool1DataBits.Items.Count >0) cmbPool1DataBits.SelectedIndex =0;
 if (cmbPool1StopBit.Items.Count >0) cmbPool1StopBit.SelectedIndex =0;
 if (cmbPool2PortName.Items.Count >0) cmbPool2PortName.SelectedIndex =0;
 if (cmbPool2BaudRate.Items.Count >0) cmbPool2BaudRate.SelectedIndex =0;
 if (cmbPool2Parity.Items.Count >0) cmbPool2Parity.SelectedIndex =0;
 if (cmbPool2DataBits.Items.Count >0) cmbPool2DataBits.SelectedIndex =0;
 if (cmbPool2StopBit.Items.Count >0) cmbPool2StopBit.SelectedIndex =0;
 if (cmbPool3PortName.Items.Count >0) cmbPool3PortName.SelectedIndex =0;
 if (cmbPool3BaudRate.Items.Count >0) cmbPool3BaudRate.SelectedIndex =0;
 if (cmbPool3Parity.Items.Count >0) cmbPool3Parity.SelectedIndex =0;
 if (cmbPool3DataBits.Items.Count >0) cmbPool3DataBits.SelectedIndex =0;
 if (cmbPool3StopBit.Items.Count >0) cmbPool3StopBit.SelectedIndex =0;
 if (cmbPool4PortName.Items.Count >0) cmbPool4PortName.SelectedIndex =0;
 if (cmbPool4BaudRate.Items.Count >0) cmbPool4BaudRate.SelectedIndex =0;
 if (cmbPool4Parity.Items.Count >0) cmbPool4Parity.SelectedIndex =0;
 if (cmbPool4DataBits.Items.Count >0) cmbPool4DataBits.SelectedIndex =0;
 if (cmbPool4StopBit.Items.Count >0) cmbPool4StopBit.SelectedIndex =0;
 }
 }
 catch
 {
 // on error fall back to defaults so UI has entries
 cmbRTUPortName.ItemsSource = SerialPort.GetPortNames();
 cmbRTUBaudRate.ItemsSource = defaultBaudRates;
 cmbRTUParity.ItemsSource = defaultParityOptions;
 cmbRTUDataBits.ItemsSource = defaultDataBitsOptions;
 cmbRTUStopBit.ItemsSource = defaultStopBitsOptions;
 // pools fallback
 cmbPool1PortName.ItemsSource = SerialPort.GetPortNames();
 cmbPool2PortName.ItemsSource = SerialPort.GetPortNames();
 cmbPool3PortName.ItemsSource = SerialPort.GetPortNames();
 cmbPool4PortName.ItemsSource = SerialPort.GetPortNames();
 cmbPool1BaudRate.ItemsSource = defaultBaudRates;
 cmbPool2BaudRate.ItemsSource = defaultBaudRates;
 cmbPool3BaudRate.ItemsSource = defaultBaudRates;
 cmbPool4BaudRate.ItemsSource = defaultBaudRates;
 cmbPool1Parity.ItemsSource = defaultParityOptions;
 cmbPool2Parity.ItemsSource = defaultParityOptions;
 cmbPool3Parity.ItemsSource = defaultParityOptions;
 cmbPool4Parity.ItemsSource = defaultParityOptions;
 cmbPool1DataBits.ItemsSource = defaultDataBitsOptions;
 cmbPool2DataBits.ItemsSource = defaultDataBitsOptions;
 cmbPool3DataBits.ItemsSource = defaultDataBitsOptions;
 cmbPool4DataBits.ItemsSource = defaultDataBitsOptions;
 cmbPool1StopBit.ItemsSource = defaultStopBitsOptions;
 cmbPool2StopBit.ItemsSource = defaultStopBitsOptions;
 cmbPool3StopBit.ItemsSource = defaultStopBitsOptions;
 cmbPool4StopBit.ItemsSource = defaultStopBitsOptions;
 }
 }
 }
}
