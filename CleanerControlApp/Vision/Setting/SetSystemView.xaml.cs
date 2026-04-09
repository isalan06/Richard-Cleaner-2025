using CleanerControlApp.Utilities;
using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CleanerControlApp.Vision.SettingViews
{
    public partial class SetSystemView : UserControl
    {
        private ModuleSettings _moduleSettings;

        public SetSystemView()
        {
            InitializeComponent();

            // set title if needed (XAML already contains Chinese text)
            if (App.AppHost != null)
            {
                try
                {
                    var mod = App.AppHost.Services.GetService(typeof(ModuleSettings)) as ModuleSettings;
                    if (mod != null) _moduleSettings = mod;
                }
                catch { }
            }

            try { ConfigLoader.Load(); } catch { }

            if (_moduleSettings == null)
            {
                try { _moduleSettings = ConfigLoader.GetModuleSettings(); } catch { }
            }

            LoadToUI();
        }

        private TextBox? GetTextBox(string name) => this.FindName(name) as TextBox;

        private void LoadToUI()
        {
            if (_moduleSettings?.MS_System == null)
                return;

            var s = _moduleSettings.MS_System;

            var tbSink = GetTextBox("Txt_SinkModulePass");
            var tbSoak = GetTextBox("Txt_SoakingTankModulePass");
            var tbDry1 = GetTextBox("Txt_DryingTank1ModulePass");
            var tbDry2 = GetTextBox("Txt_DryingTank2ModulePass");
            var tbWrite = GetTextBox("Txt_WriteMotionParameterAfterInitialization");

            if (tbSink != null) tbSink.Text = s.SinkModulePass.ToString(CultureInfo.InvariantCulture);
            if (tbSoak != null) tbSoak.Text = s.SoakingTankModulePass.ToString(CultureInfo.InvariantCulture);
            if (tbDry1 != null) tbDry1.Text = s.DryingTank1ModulePass.ToString(CultureInfo.InvariantCulture);
            if (tbDry2 != null) tbDry2.Text = s.DryingTank2ModulePass.ToString(CultureInfo.InvariantCulture);
            if (tbWrite != null) tbWrite.Text = s.WriteMotionParameterAfterInitialization.ToString(CultureInfo.InvariantCulture);
        }

        private void BtnRead_Click(object sender, RoutedEventArgs e)
        {
            if (App.AppHost != null)
            {
                try
                {
                    var mod = App.AppHost.Services.GetService(typeof(ModuleSettings)) as ModuleSettings;
                    if (mod != null) _moduleSettings = mod;
                }
                catch { }
            }

            try { ConfigLoader.Load(); } catch { }
            if (_moduleSettings == null)
            {
                try { _moduleSettings = ConfigLoader.GetModuleSettings(); } catch { }
            }

            LoadToUI();
            MessageBox.Show("讀取完成", "系統", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnWrite_Click(object sender, RoutedEventArgs e)
        {
            if (_moduleSettings == null)
            {
                if (App.AppHost != null)
                {
                    try { _moduleSettings = App.AppHost.Services.GetService(typeof(ModuleSettings)) as ModuleSettings; } catch { }
                }

                if (_moduleSettings == null)
                {
                    try { ConfigLoader.Load(); _moduleSettings = ConfigLoader.GetModuleSettings(); } catch { }
                }

                if (_moduleSettings == null)
                {
                    _moduleSettings = new ModuleSettings();
                }
            }

            var tbSink = GetTextBox("Txt_SinkModulePass");
            var tbSoak = GetTextBox("Txt_SoakingTankModulePass");
            var tbDry1 = GetTextBox("Txt_DryingTank1ModulePass");
            var tbDry2 = GetTextBox("Txt_DryingTank2ModulePass");
            var tbWrite = GetTextBox("Txt_WriteMotionParameterAfterInitialization");

            var errors = new StringBuilder();

            int sink = 0, soak = 0, d1 = 0, d2 = 0, write = 0;

            bool pSink = tbSink != null && int.TryParse(tbSink.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out sink);
            bool pSoak = tbSoak != null && int.TryParse(tbSoak.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out soak);
            bool pD1 = tbDry1 != null && int.TryParse(tbDry1.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out d1);
            bool pD2 = tbDry2 != null && int.TryParse(tbDry2.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out d2);
            bool pWrite = tbWrite != null && int.TryParse(tbWrite.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out write);

            if (!pSink) errors.AppendLine("Sink 模組通過數 格式錯誤");
            if (!pSoak) errors.AppendLine("浸泡槽模組通過數 格式錯誤");
            if (!pD1) errors.AppendLine("乾燥槽1模組通過數 格式錯誤");
            if (!pD2) errors.AppendLine("乾燥槽2模組通過數 格式錯誤");
            if (!pWrite) errors.AppendLine("初始化後寫入運動參數 格式錯誤");

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_moduleSettings.MS_System == null) _moduleSettings.MS_System = new MS_System();

            _moduleSettings.MS_System.SinkModulePass = sink;
            _moduleSettings.MS_System.SoakingTankModulePass = soak;
            _moduleSettings.MS_System.DryingTank1ModulePass = d1;
            _moduleSettings.MS_System.DryingTank2ModulePass = d2;
            _moduleSettings.MS_System.WriteMotionParameterAfterInitialization = write;

            try
            {
                ConfigLoader.SetModuleSettings(_moduleSettings);

                if (App.AppHost != null)
                {
                    try
                    {
                        var diModule = App.AppHost.Services.GetService(typeof(ModuleSettings)) as ModuleSettings;
                        if (diModule != null)
                        {
                            diModule.MS_System = _moduleSettings.MS_System;
                        }
                    }
                    catch { }
                }

                MessageBox.Show("寫入完成", "系統", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"寫入失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
