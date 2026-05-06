using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CleanerControlApp.Vision.Template
{
    /// <summary>
    /// Template_Home.xaml 的互動邏輯
    /// </summary>
    public partial class Template_Home : UserControl
    {
        public Template_Home()
        {
            InitializeComponent();

            this.Loaded += Template_Home_Loaded;
        }

        private void Template_Home_Loaded(object? sender, RoutedEventArgs e)
        {
            // ensure SystemLineControl width follows HostWidth if provided, otherwise follows LineHostCanvas ActualWidth
            UpdateSystemLineWidth();

            // keep in sync when LineHostCanvas size changes and HostWidth is not provided
            LineHostCanvas.SizeChanged += (s, ev) =>
            {
                if (double.IsNaN(HostWidth) || HostWidth <=0)
                {
                    // if binding is active, no need to set explicitly; ensure binding exists
                    BindingOperations.SetBinding(SystemLineControl, WidthProperty, new Binding("ActualWidth") { Source = LineHostCanvas });
                }
            };
        }

        // HostWidth dependency property: when set (>0) this will be used as the width of SystemLineControl.
        // If not set (NaN or <=0) SystemLineControl will bind to LineHostCanvas.ActualWidth.
        public static readonly DependencyProperty HostWidthProperty = DependencyProperty.Register(
            nameof(HostWidth), typeof(double), typeof(Template_Home),
            new PropertyMetadata(double.NaN, OnHostWidthChanged));

        public double HostWidth
        {
            get => (double)GetValue(HostWidthProperty);
            set => SetValue(HostWidthProperty, value);
        }

        private static void OnHostWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Template_Home th)
            {
                th.UpdateSystemLineWidth();
            }
        }

        private void UpdateSystemLineWidth()
        {
            if (SystemLineControl == null || LineHostCanvas == null) return;

            if (double.IsNaN(HostWidth) || HostWidth <=0)
            {
                // bind to LineHostCanvas.ActualWidth so right edge follows host
                BindingOperations.SetBinding(SystemLineControl, WidthProperty, new Binding("ActualWidth") { Source = LineHostCanvas });
            }
            else
            {
                // clear any binding and set explicit width
                BindingOperations.ClearBinding(SystemLineControl, WidthProperty);
                SystemLineControl.Width = HostWidth;
            }
        }
    }
}
