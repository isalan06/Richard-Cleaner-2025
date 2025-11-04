using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Extensions.Logging;

namespace CleanerControlApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger; // Logger 欄位

        /// <summary>
        /// 建構式，注入 Logger
        /// </summary>
        /// <param name="logger"></param>
        public MainWindow(ILogger<MainWindow> logger)
        {
            InitializeComponent();
            _logger = logger; // 注入的 Logger
            
        }

        /// <summary>
        /// 視窗內容呈現完成後觸發
        /// </summary>
        /// <param name="e"></param>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            //_logger.LogInformation("MainWindow Show.");
            //Console.WriteLine("MainWindow Show.");
        }
    }
}