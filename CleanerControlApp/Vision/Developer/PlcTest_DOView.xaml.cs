using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CleanerControlApp.Vision.Developer
{
    public partial class PlcTest_DOView : UserControl
    {
        public ObservableCollection<DOGroup> Groups { get; } = new ObservableCollection<DOGroup>();

        private readonly IPLCService? _plcService;
        private readonly DispatcherTimer _refreshTimer;

        public PlcTest_DOView()
        {
            InitializeComponent();
            DataContext = this;

            _plcService = App.AppHost?.Services.GetService<IPLCService>();

            AddGroup("Y0~Y7", new[] {
 ("Y0","移載組X軸伺服馬達PLS"),
 ("Y1","移載組Z軸伺服馬達PLS"),
 ("Y2","沖水槽Z軸伺服馬達PLS"),
 ("Y3","浸泡槽Z軸伺服馬達PLS"),
 ("Y4","移載組X軸伺服馬達SIGN"),
 ("Y5","移載組Z軸伺服馬達SIGN"),
 ("Y6","沖水槽Z軸伺服馬達SIGN"),
 ("Y7","浸泡槽Z軸伺服馬達SIGN")
 });

            AddGroup("Y10~Y17", new[] {
 ("Y10","移載組X軸伺服位置指令禁止"),
 ("Y11","移載組X軸伺服警報復位"),
 ("Y12","移載組X軸伺服ON"),
 ("Y13","移載組Z軸伺服位置指令禁止"),
 ("Y14","移載組Z軸伺服警報復位"),
 ("Y15","移載組Z軸伺服ON"),
 ("Y16","沖洗槽Z軸伺服位置指令禁止"),
 ("Y17","沖洗槽Z軸伺服警報復位")
 });

            AddGroup("Y20~Y27", new[] {
 ("Y20","沖洗槽Z軸伺服ON"),
 ("Y21","浸泡槽Z軸伺服位置指令禁止"),
 ("Y22","浸泡槽Z軸伺服警報復位"),
 ("Y23","浸泡槽Z軸伺服ON"),
 ("Y24","Reserved"),
 ("Y25","Reserved"),
 ("Y26","Reserved"),
 ("Y27","Reserved")
 });

            AddGroup("Y30~Y37", new[] {
 ("Y30","移載組X軸伺服馬達剎車"),
 ("Y31","移載組Z軸伺服馬達剎車"),
 ("Y32","沖水槽Z軸伺服馬達剎車"),
 ("Y33","浸泡槽Z軸伺服馬達剎車"),
 ("Y34","烘乾槽1鼓風機開"),
 ("Y35","烘乾槽2鼓風機開"),
 ("Y36","Reserved"),
 ("Y37","Reserved")
 });

            AddGroup("Y40~Y47", new[] {
 ("Y40","移載組Z軸夾爪開"),
 ("Y41","移載組Z軸夾爪關"),
 ("Y42","入水氣動閥開"),
 ("Y43","浸泡槽排水氣動閥開"),
 ("Y44","加熱槽&浸泡槽切換氣動閥開"),
 ("Y45","Reserved"),
 ("Y46","Reserved"),
 ("Y47","Reserved")
 });

            AddGroup("Y50~Y57", new[] {
 ("Y50","沖水槽上蓋開"),
 ("Y51","浸泡槽上蓋開"),
 ("Y52","烘乾槽1上蓋開"),
 ("Y53","烘乾槽2上蓋開"),
 ("Y54","沖水槽風刀開"),
 ("Y55","浸泡槽風刀開"),
 ("Y56","烘乾槽1正壓入氣開關"),
 ("Y57","烘乾槽2正壓入氣開關")
 });

            AddGroup("Y60~Y67", new[] {
 ("Y60","三色燈紅"),
 ("Y61","三色燈黃"),
 ("Y62","三色燈綠"),
 ("Y63","三色燈蜂鳴器"),
 ("Y64","Reserved"),
 ("Y65","Reserved"),
 ("Y66","Reserved"),
 ("Y67","Reserved")
 });

            _refreshTimer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = System.TimeSpan.FromMilliseconds(200)
 };

            _refreshTimer.Tick += (s, e) =>
 {
 foreach (var g in Groups)
 {
 foreach (var it in g.Items)
 it.Refresh();
 }
 };
            _refreshTimer.Start();
 }

 private void AddGroup(string header, (string addr, string desc)[] entries)
 {
 var g = new DOGroup { Header = header };
 foreach (var e in entries)
 {
 g.Items.Add(new DOItem(e.addr, e.desc, _plcService));
 }
 Groups.Add(g);
 }

 private void OnSetButtonClick(object sender, RoutedEventArgs e)
 {
 if (sender is Button btn && btn.DataContext is DOItem item)
 {
 // toggle command bit for this item
 item.ToggleCommand();
 }
 }
 }

 public class DOGroup
 {
 public string? Header { get; set; }
 public ObservableCollection<DOItem> Items { get; } = new ObservableCollection<DOItem>();
 }

 public class DOItem : INotifyPropertyChanged
 {
 private readonly IPLCService? _plc = null;
 private readonly int _wordIndex;
 private readonly int _bitIndex;

 public string Address { get; }
 public string Description { get; }

 public DOItem(string address, string description, IPLCService? plc)
 {
 Address = address;
 Description = description;
 _plc = plc;

 if (int.TryParse(address.TrimStart('Y', 'y'), out int num))
 {
 int tens = num /10;
 int units = num %10;
 _wordIndex = tens /2; // every two tens groups -> next word
 _bitIndex = units + (tens %2) *8; // odd tens -> high byte (bits8..15)
 }
 else
 {
 _wordIndex =0;
 _bitIndex =0;
 }
 }

 public bool IsOn
 {
 get
 {
 if (_plc == null) return false;
 var arr = _plc.DIO_Y;
 if (arr == null) return false;
 if (_wordIndex <0 || _wordIndex >= arr.Length) return false;
 if (_bitIndex <0 || _bitIndex >15) return false;
 return arr[_wordIndex].GetBit(_bitIndex);
 }
 }

 // Command mapping: base command index5 + _wordIndex
 private int CommandWordIndex =>5 + _wordIndex;

 public bool CommandIsOn
 {
 get
 {
 if (_plc == null) return false;
 var cmd = _plc.Command;
 if (cmd == null) return false;
 if (CommandWordIndex <0 || CommandWordIndex >= cmd.Length) return false;
 if (_bitIndex <0 || _bitIndex >15) return false;
 return cmd[CommandWordIndex].GetBit(_bitIndex);
 }
 }

 public void ToggleCommand()
 {
 if (_plc == null) return;
 var cmd = _plc.Command;
 if (cmd == null) return;
 if (CommandWordIndex <0 || CommandWordIndex >= cmd.Length) return;
 if (_bitIndex <0 || _bitIndex >15) return;

 var union = cmd[CommandWordIndex];
 bool current = union.GetBit(_bitIndex);
 union.SetBit(_bitIndex, !current);
 // write back the modified struct
 cmd[CommandWordIndex] = union;
 // assign back to plc service
 _plc.Command = cmd;

 // notify change
 OnPropertyChanged(nameof(CommandIsOn));
 }

 public void Refresh()
 {
 OnPropertyChanged(nameof(IsOn));
 OnPropertyChanged(nameof(CommandIsOn));
 }

 public event PropertyChangedEventHandler? PropertyChanged;
 protected void OnPropertyChanged([CallerMemberName] string? propName = null)
 {
 PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
 }
 }
}
