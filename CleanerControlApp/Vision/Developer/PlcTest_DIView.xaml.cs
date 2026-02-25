using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Threading;

using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CleanerControlApp.Vision.Developer
{
    public partial class PlcTest_DIView : UserControl
    {
        public ObservableCollection<DIGroup> Groups { get; } = new ObservableCollection<DIGroup>();

        private readonly IPLCService? _plcService;
        private readonly DispatcherTimer _refreshTimer;

        public PlcTest_DIView()
        {
            InitializeComponent();
            DataContext = this;

            // resolve plc service from host if available
            _plcService = App.AppHost?.Services.GetService<IPLCService>();

            // Populate groups according to provided table (updated descriptions)
            AddGroup("X0~X7", new[] {
 ("X0","移載組X軸負極限"),
 ("X1","移載組X軸正極限"),
 ("X2","移載組Z軸負極限"),
 ("X3","移載組Z軸正極限"),
 ("X4","沖洗槽Z軸負極限"),
 ("X5","沖洗槽Z軸正極限"),
 ("X6","浸泡槽Z軸負極限"),
 ("X7","浸泡槽Z軸正極限")
 });

            AddGroup("X10~X17", new[] {
 ("X10","移載組X軸伺服馬達待機"),
 ("X11","移載組X軸伺服馬達定位完成"),
 ("X12","移載組X軸伺服馬達警報"),
 ("X13","移載組X軸伺服馬達回零完成"),
 ("X14","移載組Z軸伺服馬達待機"),
 ("X15","移載組Z軸伺服馬達定位完成"),
 ("X16","移載組Z軸伺服馬達警報"),
 ("X17","移載組Z軸伺服馬達回零完成")
 });

            AddGroup("X20~X27", new[] {
 ("X20","沖洗槽Z軸伺服馬達待機"),
 ("X21","沖洗槽Z軸伺服馬達定位完成"),
 ("X22","沖洗槽Z軸伺服馬達警報"),
 ("X23","沖洗槽Z軸伺服馬達回零完成"),
 ("X24","浸泡槽Z軸伺服馬達待機"),
 ("X25","浸泡槽Z軸伺服馬達定位完成"),
 ("X26","浸泡槽Z軸伺服馬達警報"),
 ("X27","浸泡槽Z軸伺服馬達回零完成")
 });

            AddGroup("X30~X37", new[] {
 ("X30","移載組Z軸夾爪在席感應器1"),
 ("X31","移載組Z軸夾爪在席感應器2"),
 ("X32","移載組Z軸前夾爪開磁簧"),
 ("X33","移載組Z軸前夾爪關磁簧"),
 ("X34","移載組Z軸後夾爪開磁簧"),
 ("X35","移載組Z軸後夾爪關磁簧"),
 ("X36","入料在席感應器1"),
 ("X37","入料在席感應器2")
 });

            AddGroup("X40~X47", new[] {
 ("X40","入料在席感應器3"),
 ("X41","入料在席感應器4"),
 ("X42","入料在席感應器5"),
 ("X43","出料在席感應器1"),
 ("X44","出料在席感應器2"),
 ("X45","出料在席感應器3"),
 ("X46","出料在席感應器4"),
 ("X47","出料在席感應器5")
 });

            AddGroup("X50~X57", new[] {
 ("X50","沖水槽開蓋氣缸前到位磁簧"),
 ("X51","沖水槽開蓋氣缸後到位磁簧"),
 ("X52","浸泡槽開蓋氣缸前到位磁簧"),
 ("X53","浸泡槽開蓋氣缸後到位磁簧"),
 ("X54","浸泡槽低水位浮球開關"),
 ("X55","浸泡槽高水位浮球開關"),
 ("X56","烘乾槽1開蓋氣缸前到位磁簧"),
 ("X57","烘乾槽1開蓋氣缸後到位磁簧")
 });

            AddGroup("X60~X67", new[] {
 ("X60","烘乾槽2開蓋氣缸前到位磁簧"),
 ("X61","烘乾槽2開蓋氣缸後到位磁簧"),
 ("X62","熱水槽水位浮球開關LL"),
 ("X63","熱水槽水位浮球開關L"),
 ("X64","熱水槽水位浮球開關H"),
 ("X65","熱水槽水位浮球開關HH"),
 ("X66","廢水槽水位浮球開關H"),
 ("X67","Reserved")
 });

            AddGroup("X70~X77", new[] {
 ("X70","緊急停止開關訊號"),
 ("X71","維修訊號"),
 ("X72","移載組Z軸夾爪開"),
 ("X73","移載組Z軸夾爪關"),
 ("X74","主氣源訊號"),
 ("X75","前門檢感應器1"),
 ("X76","前門檢感應器2"),
 ("X77","前門檢感應器3")
 });

            AddGroup("X100~X107", new[] {
 ("X100","前門檢感應器4"),
 ("X101","側門檢感應器1"),
 ("X102","側門檢感應器2"),
 ("X103","漏液感應器1"),
 ("X104","漏液感應器2"),
 ("X105","Reserved"),
 ("X106","Reserved"),
 ("X107","Reserved")
 });

            // Refresh timer to update lamp display from PLC
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
            var g = new DIGroup { Header = header };
            foreach (var e in entries)
            {
                g.Items.Add(new DIItem(e.addr, e.desc, _plcService));
            }
            Groups.Add(g);
        }
    }

    public class DIGroup
    {
        public string? Header { get; set; }
        public ObservableCollection<DIItem> Items { get; } = new ObservableCollection<DIItem>();
    }

    public class DIItem : INotifyPropertyChanged
    {
        private readonly IPLCService? _plc;
        private readonly int _wordIndex;
        private readonly int _bitIndex;

        public string Address { get; }
        public string Description { get; }

        public DIItem(string address, string description, IPLCService? plc)
        {
            Address = address;
            Description = description;
            _plc = plc;

            // compute mapping from address to DIO_X[word].Bit
            // address format: Xnn (e.g., X0, X10, X20, X100)
            if (int.TryParse(address.TrimStart('X', 'x'), out int num))
            {
                int tens = num /10;
                int units = num %10;
                _wordIndex = tens /2; // every two 'tens' groups map to next word
                _bitIndex = units + (tens %2) *8; // tens odd -> high byte (bits8..15)
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
                var arr = _plc.DIO_X;
                if (arr == null) return false;
                if (_wordIndex <0 || _wordIndex >= arr.Length) return false;
                // ensure bit index in0..15
                if (_bitIndex <0 || _bitIndex >15) return false;
                return arr[_wordIndex].GetBit(_bitIndex);
            }
        }

        public void Refresh() => OnPropertyChanged(nameof(IsOn));

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
