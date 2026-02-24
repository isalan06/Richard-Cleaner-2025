using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Controls;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows;

namespace CleanerControlApp.Vision.Developer
{
 /// <summary>
 /// Interaction logic for DevCommParameterView.xaml
 /// </summary>
 public partial class DevCommParameterView : UserControl
 {
 public DevCommParameterView()
 {
 InitializeComponent();
 LoadModbusTcpParameters();
 LoadModbusRtuParameters();
 }

 private void BtnLoadParams_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 // Reload from file
 LoadModbusTcpParameters();
 LoadModbusRtuParameters();
 }

 private void BtnSaveParams_Click(object sender, System.Windows.RoutedEventArgs e)
 {
 try
 {
 var basePath = System.AppDomain.CurrentDomain.BaseDirectory;
 var configPath = Path.Combine(basePath, "appsettings.json");
 JsonNode root;

 if (File.Exists(configPath))
 {
 var text = File.ReadAllText(configPath);
 // parse with JsonDocument allowing trailing commas, then get normalized JSON
 var doc = JsonDocument.Parse(text, new JsonDocumentOptions { AllowTrailingCommas = true });
 var normalized = doc.RootElement.GetRawText();
 root = JsonNode.Parse(normalized) ?? new JsonObject();
 }
 else
 {
 root = new JsonObject();
 }

 if (root["CommunicationSettings"] == null)
 root["CommunicationSettings"] = new JsonObject();

 var comm = root["CommunicationSettings"].AsObject();

 // ModbusTCPParameter
 var tcp = new JsonObject();
 tcp["IP"] = TxtModbusIP?.Text ?? "";
 if (int.TryParse(TxtModbusPort?.Text, out var tcpPort)) tcp["Port"] = tcpPort; else tcp["Port"] = TxtModbusPort?.Text ?? "";
 comm["ModbusTCPParameter"] = tcp;

 // ModbusRTUParameter (main)
 var rtu = new JsonObject();
 rtu["PortName"] = cmbRTUPortName?.SelectedItem?.ToString() ?? "";
 if (int.TryParse(cmbRTUBaudRate?.SelectedItem?.ToString(), out var baud)) rtu["Baudrate"] = baud; else rtu["Baudrate"] = cmbRTUBaudRate?.SelectedItem?.ToString() ?? "";
 rtu["Parity"] = cmbRTUParity?.SelectedItem?.ToString() ?? "";
 if (int.TryParse(cmbRTUDataBits?.SelectedItem?.ToString(), out var db)) rtu["DataBits"] = db; else rtu["DataBits"] = cmbRTUDataBits?.SelectedItem?.ToString() ?? "";
 // StopBit may be float like1.5; try double
 if (double.TryParse(cmbRTUStopBit?.SelectedItem?.ToString(), out var sb))
 {
 if (sb == (int)sb) rtu["StopBit"] = (int)sb; else rtu["StopBit"] = sb;
 }
 else rtu["StopBit"] = cmbRTUStopBit?.SelectedItem?.ToString() ?? "";

 comm["ModbusRTUParameter"] = rtu;

 // ModbusRTUPoolParameter
 var poolArray = new JsonArray();
 for (int i =0; i <4; i++)
 {
 var poolObj = new JsonObject();
 ComboBox portCb = i ==0 ? cmbPool1PortName : i ==1 ? cmbPool2PortName : i ==2 ? cmbPool3PortName : cmbPool4PortName;
 ComboBox baudCb = i ==0 ? cmbPool1BaudRate : i ==1 ? cmbPool2BaudRate : i ==2 ? cmbPool3BaudRate : cmbPool4BaudRate;
 ComboBox parityCb = i ==0 ? cmbPool1Parity : i ==1 ? cmbPool2Parity : i ==2 ? cmbPool3Parity : cmbPool4Parity;
 ComboBox dataCb = i ==0 ? cmbPool1DataBits : i ==1 ? cmbPool2DataBits : i ==2 ? cmbPool3DataBits : cmbPool4DataBits;
 ComboBox stopCb = i ==0 ? cmbPool1StopBit : i ==1 ? cmbPool2StopBit : i ==2 ? cmbPool3StopBit : cmbPool4StopBit;

 var portVal = portCb?.SelectedItem?.ToString() ?? "";
 var baudVal = baudCb?.SelectedItem?.ToString() ?? "";
 var parityVal = parityCb?.SelectedItem?.ToString() ?? "";
 var dataVal = dataCb?.SelectedItem?.ToString() ?? "";
 var stopVal = stopCb?.SelectedItem?.ToString() ?? "";

 poolObj["PortName"] = portVal;
 if (int.TryParse(baudVal, out var pbaud)) poolObj["Baudrate"] = pbaud; else poolObj["Baudrate"] = baudVal;
 poolObj["Parity"] = parityVal;
 if (int.TryParse(dataVal, out var pdata)) poolObj["DataBits"] = pdata; else poolObj["DataBits"] = dataVal;
 if (double.TryParse(stopVal, out var pstop)) { if (pstop == (int)pstop) poolObj["StopBit"] = (int)pstop; else poolObj["StopBit"] = pstop; }
 else poolObj["StopBit"] = stopVal;

 poolArray.Add(poolObj);
 }

 comm["ModbusRTUPoolParameter"] = poolArray;

 // write back to runtime copy
 var options = new JsonSerializerOptions { WriteIndented = true };
 var outText = root.ToJsonString(options);
 File.WriteAllText(configPath, outText);

 // Also attempt to write back to project/source appsettings.json (search upward)
 try
 {
 var dir = new DirectoryInfo(basePath);
 bool wroteSource = false;
 for (int up =0; up <10 && dir != null; up++)
 {
 var candidate = Path.Combine(dir.FullName, "appsettings.json");
 if (File.Exists(candidate))
 {
 // avoid overwriting the runtime copy we just wrote
 if (!Path.GetFullPath(candidate).Equals(Path.GetFullPath(configPath), System.StringComparison.OrdinalIgnoreCase))
 {
 File.WriteAllText(candidate, outText);
 wroteSource = true;
 }
 break;
 }
 dir = dir.Parent;
 }

 MessageBox.Show(wroteSource ? "參數已寫入執行目錄與原始 appsettings.json" : "參數已寫入執行目錄 (找不到原始 appsettings.json)", "寫入參數", MessageBoxButton.OK, MessageBoxImage.Information);
 }
 catch (System.Exception ex)
 {
 MessageBox.Show("寫入設定檔時發生錯誤: " + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }
 catch (System.Exception ex)
 {
 MessageBox.Show("寫入參數失敗: " + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }

 private void LoadModbusTcpParameters()
 {
 try
 {
 var basePath = System.AppDomain.CurrentDomain.BaseDirectory;
 var configPath = Path.Combine(basePath, "appsettings.json");
 if (!File.Exists(configPath))
 return;

 using var fs = File.OpenRead(configPath);
 var parseOptions = new JsonDocumentOptions { AllowTrailingCommas = true };
 using var doc = JsonDocument.Parse(fs, parseOptions);
 if (doc.RootElement.TryGetProperty("CommunicationSettings", out var comm))
 {
 if (comm.TryGetProperty("ModbusTCPParameter", out var tcp))
 {
 if (tcp.TryGetProperty("IP", out var ip))
 {
 TxtModbusIP.Text = ip.GetString();
 }
 if (tcp.TryGetProperty("Port", out var port))
 {
 TxtModbusPort.Text = port.GetRawText().Trim('"');
 }
 }
 }
 }
 catch
 {
 // ignore errors, keep defaults
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

 var basePath = System.AppDomain.CurrentDomain.BaseDirectory;
 var configPath = Path.Combine(basePath, "appsettings.json");
 if (!File.Exists(configPath))
 {
 // assign defaults
 cmbRTUPortName.ItemsSource = systemPorts;
 cmbRTUBaudRate.ItemsSource = defaultBaudRates;
 cmbRTUParity.ItemsSource = defaultParityOptions;
 cmbRTUDataBits.ItemsSource = defaultDataBitsOptions;
 cmbRTUStopBit.ItemsSource = defaultStopBitsOptions;

 // Pool controls get defaults too
 cmbPool1PortName.ItemsSource = systemPorts;
 cmbPool2PortName.ItemsSource = systemPorts;
 cmbPool3PortName.ItemsSource = systemPorts;
 cmbPool4PortName.ItemsSource = systemPorts;
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

 return;
 }

 using var fs = File.OpenRead(configPath);
 var parseOptions = new JsonDocumentOptions { AllowTrailingCommas = true };
 using var doc = JsonDocument.Parse(fs, parseOptions);
 if (doc.RootElement.TryGetProperty("CommunicationSettings", out var comm))
 {
 // If ModbusRTUPoolParameter exists, collect values for baud/parity/databits/stopbits and per-pool selections
 if (comm.TryGetProperty("ModbusRTUPoolParameter", out var pool) && pool.ValueKind == JsonValueKind.Array)
 {
 int idx =0;
 foreach (var item in pool.EnumerateArray())
 {
 if (item.TryGetProperty("Baudrate", out var b))
 {
 var s = b.GetRawText().Trim('"');
 if (!string.IsNullOrEmpty(s)) baudSet.Add(s);
 if (idx <4) poolBaudVals[idx] = s;
 }
 if (item.TryGetProperty("Parity", out var p))
 {
 var s = p.GetString();
 if (!string.IsNullOrEmpty(s)) paritySet.Add(s);
 if (idx <4) poolParityVals[idx] = s;
 }
 if (item.TryGetProperty("DataBits", out var db))
 {
 var s = db.GetRawText().Trim('"');
 if (!string.IsNullOrEmpty(s)) dataBitsSet.Add(s);
 if (idx <4) poolDataBitsVals[idx] = s;
 }
 if (item.TryGetProperty("StopBit", out var sb))
 {
 var s = sb.GetRawText().Trim('"');
 if (!string.IsNullOrEmpty(s)) stopBitsSet.Add(s);
 if (idx <4) poolStopBitVals[idx] = s;
 }
 // For pool portname, include it in the specific pool's port list but do not merge to global systemPorts
 if (item.TryGetProperty("PortName", out var pn))
 {
 var s = pn.GetString();
 if (!string.IsNullOrEmpty(s) && idx <4) poolPortNameVals[idx] = s;
 }
 idx++;
 }
 }

 // Build lists for itemsources
 var portNameListForMain = new List<string>(systemPorts);
 // main RTU PortName uses system ports only
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

 // Now read ModbusRTUParameter to set main RTU selected values (defaults if missing)
 if (comm.TryGetProperty("ModbusRTUParameter", out var rtu))
 {
 string portNameVal = null, baudVal = null, parityVal = null, dataBitsVal = null, stopBitVal = null;

 if (rtu.TryGetProperty("PortName", out var portName))
 {
 portNameVal = portName.GetString();
 }
 if (rtu.TryGetProperty("Baudrate", out var baud))
 {
 baudVal = baud.GetRawText().Trim('"');
 }
 if (rtu.TryGetProperty("Parity", out var parity))
 {
 parityVal = parity.GetString();
 }
 if (rtu.TryGetProperty("DataBits", out var db))
 {
 dataBitsVal = db.GetRawText().Trim('"');
 }
 if (rtu.TryGetProperty("StopBit", out var sb))
 {
 stopBitVal = sb.GetRawText().Trim('"');
 }

 if (!string.IsNullOrEmpty(portNameVal) && cmbRTUPortName.Items.Contains(portNameVal))
 cmbRTUPortName.SelectedItem = portNameVal;
 else if (cmbRTUPortName.Items.Count >0)
 cmbRTUPortName.SelectedIndex =0;

 if (!string.IsNullOrEmpty(baudVal) && cmbRTUBaudRate.Items.Contains(baudVal))
 cmbRTUBaudRate.SelectedItem = baudVal;
 else if (cmbRTUBaudRate.Items.Count >0)
 cmbRTUBaudRate.SelectedIndex =0;

 if (!string.IsNullOrEmpty(parityVal) && cmbRTUParity.Items.Contains(parityVal))
 cmbRTUParity.SelectedItem = parityVal;
 else if (cmbRTUParity.Items.Count >0)
 cmbRTUParity.SelectedIndex =0;

 if (!string.IsNullOrEmpty(dataBitsVal) && cmbRTUDataBits.Items.Contains(dataBitsVal))
 cmbRTUDataBits.SelectedItem = dataBitsVal;
 else if (cmbRTUDataBits.Items.Count >0)
 cmbRTUDataBits.SelectedIndex =0;

 if (!string.IsNullOrEmpty(stopBitVal) && cmbRTUStopBit.Items.Contains(stopBitVal))
 cmbRTUStopBit.SelectedItem = stopBitVal;
 else if (cmbRTUStopBit.Items.Count >0)
 cmbRTUStopBit.SelectedIndex =0;
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
