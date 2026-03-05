using CleanerControlApp.Utilities;
using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CleanerControlApp.Vision.SettingViews
{
    public partial class SetSinkView : UserControl
    {
        private ModuleSettings _moduleSettings;
        private UnitSettings _unitSettings;

        public SetSinkView()
        {
            InitializeComponent();

            if (App.AppHost != null)
            {
                try
                {
                    var mod = App.AppHost.Services.GetService(typeof(ModuleSettings)) as ModuleSettings;
                    var unit = App.AppHost.Services.GetService(typeof(UnitSettings)) as UnitSettings;
                    _moduleSettings = mod;
                    _unit_settings_assign(unit);
                }
                catch
                {
                }
            }

            // ensure configuration is loaded so GetModuleSettings/GetUnitSettings return data
            try { ConfigLoader.Load(); } catch { }

            // fallback to ConfigLoader if DI didn't provide settings
            if (_moduleSettings == null)
            {
                try { _moduleSettings = ConfigLoader.GetModuleSettings(); } catch { }
            }
            if (_unitSettings == null)
            {
                try { _unitSettings = ConfigLoader.GetUnitSettings(); } catch { }
            }

            LoadToUI();
        }

        // helper to safely assign unit fields
        private void _unit_settings_assign(UnitSettings unit)
        {
            _unitSettings = unit;
        }

        private TextBox? GetTextBox(string name) => this.FindName(name) as TextBox;

        private void LoadToUI()
        {
            if (_moduleSettings?.Sink == null)
                return;

            var m = _moduleSettings.Sink;
            var u = _unitSettings?.Sink;
            var transfer = (u != null && Math.Abs(u.UnitTransfer) >0.000001f) ? u.UnitTransfer :1f;
            var motorTransfer = (u != null && Math.Abs(u.MotorUnitTransfer) >0.000001f) ? u.MotorUnitTransfer :1f;

            float displaySVLow = m.SV_Low * transfer;
            float displaySVHigh = m.SV_High * transfer;

            var tbLow = GetTextBox("Txt_SV_Low");
            var tbHigh = GetTextBox("Txt_SV_High");
            var tbTime = GetTextBox("Txt_ActTime");
            var tbMPos1 = GetTextBox("Txt_MotorPos1");
            var tbMPos2 = GetTextBox("Txt_MotorPos2");
            var tbMPos3 = GetTextBox("Txt_MotorPos3");
            var tbMVel1 = GetTextBox("Txt_MotorVel1");
            var tbMVel2 = GetTextBox("Txt_MotorVel2");

            if (tbLow != null) tbLow.Text = displaySVLow.ToString(CultureInfo.InvariantCulture);
            if (tbHigh != null) tbHigh.Text = displaySVHigh.ToString(CultureInfo.InvariantCulture);
            if (tbTime != null) tbTime.Text = m.ActTime_Second.ToString(CultureInfo.InvariantCulture);

            if (tbMPos1 != null) tbMPos1.Text = (m.MotorPosition_01 * motorTransfer).ToString(CultureInfo.InvariantCulture);
            if (tbMPos2 != null) tbMPos2.Text = (m.MotorPosition_02 * motorTransfer).ToString(CultureInfo.InvariantCulture);
            if (tbMPos3 != null) tbMPos3.Text = (m.MotorPosition_03 * motorTransfer).ToString(CultureInfo.InvariantCulture);
            if (tbMVel1 != null) tbMVel1.Text = (m.MotorVelocity_01 * motorTransfer).ToString(CultureInfo.InvariantCulture);
            if (tbMVel2 != null) tbMVel2.Text = (m.MotorVelocity_02 * motorTransfer).ToString(CultureInfo.InvariantCulture);
        }

        private void BtnRead_Click(object sender, RoutedEventArgs e)
        {
            if (App.AppHost != null)
            {
                try
                {
                    var mod = App.AppHost.Services.GetService(typeof(ModuleSettings)) as ModuleSettings;
                    var unit = App.AppHost.Services.GetService(typeof(UnitSettings)) as UnitSettings;
                    if (mod != null) _moduleSettings = mod;
                    if (unit != null) _unit_settings_assign(unit);
                }
                catch { }
            }

            // ensure configuration loaded then fallback to ConfigLoader if DI didn't provide settings
            try { ConfigLoader.Load(); } catch { }
            if (_moduleSettings == null)
            {
                try { _moduleSettings = ConfigLoader.GetModuleSettings(); } catch { }
            }
            if (_unitSettings == null)
            {
                try { _unitSettings = ConfigLoader.GetUnitSettings(); } catch { }
            }

            LoadToUI();
            MessageBox.Show("讀取完成", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    // fallback to ConfigLoader
                    try { ConfigLoader.Load(); _moduleSettings = ConfigLoader.GetModuleSettings(); } catch { }
                }

                if (_moduleSettings == null)
                {
                    _moduleSettings = new ModuleSettings();
                }
            }

            if (_unitSettings == null)
            {
                if (App.AppHost != null)
                {
                    try { _unitSettings = App.AppHost.Services.GetService(typeof(UnitSettings)) as UnitSettings; } catch { }
                }

                if (_unitSettings == null)
                {
                    try { ConfigLoader.Load(); _unitSettings = ConfigLoader.GetUnitSettings(); } catch { }
                }

                if (_unitSettings == null) _unitSettings = new UnitSettings();
            }

            var tbLow = GetTextBox("Txt_SV_Low");
            var tbHigh = GetTextBox("Txt_SV_High");
            var tbTime = GetTextBox("Txt_ActTime");
            var tbMPos1 = GetTextBox("Txt_MotorPos1");
            var tbMPos2 = GetTextBox("Txt_MotorPos2");
            var tbMPos3 = GetTextBox("Txt_MotorPos3");
            var tbMVel1 = GetTextBox("Txt_MotorVel1");
            var tbMVel2 = GetTextBox("Txt_MotorVel2");

            var errors = new StringBuilder();

            var u = _unitSettings?.Sink;
            var transfer = (u != null && Math.Abs(u.UnitTransfer) >0.000001f) ? u.UnitTransfer :1f;
            var motorTransfer = (u != null && Math.Abs(u.MotorUnitTransfer) >0.000001f) ? u.MotorUnitTransfer :1f;

            // 將 input_low, input_high, input_time, input_mpos1, input_mpos2, input_mpos3, input_mvel1, input_mvel2 預先宣告並初始化
            float input_low =0, input_high =0, input_mpos1 =0, input_mpos2 =0, input_mpos3 =0, input_mvel1 =0, input_mvel2 =0;
            int input_time =0;

            bool parsed_low = tbLow != null && float.TryParse(tbLow.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_low);
            bool parsed_high = tbHigh != null && float.TryParse(tbHigh.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_high);
            bool parsed_time = tbTime != null && int.TryParse(tbTime.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out input_time);

            bool parsed_mpos1 = tbMPos1 != null && float.TryParse(tbMPos1.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_mpos1);
            bool parsed_mpos2 = tbMPos2 != null && float.TryParse(tbMPos2.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_mpos2);
            bool parsed_mpos3 = tbMPos3 != null && float.TryParse(tbMPos3.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_mpos3);
            bool parsed_mvel1 = tbMVel1 != null && float.TryParse(tbMVel1.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_mvel1);
            bool parsed_mvel2 = tbMVel2 != null && float.TryParse(tbMVel2.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_mvel2);

            if (!parsed_low) errors.AppendLine("沖水槽:低設定值格式錯誤");
            if (!parsed_high) errors.AppendLine("沖水槽:高設定值格式錯誤");
            if (!parsed_time) errors.AppendLine("沖水槽:動作時間格式錯誤");
            if (!parsed_mpos1) errors.AppendLine("沖水槽:馬達位置 #1 格式錯誤");
            if (!parsed_mpos2) errors.AppendLine("沖水槽:馬達位置 #2 格式錯誤");
            if (!parsed_mpos3) errors.AppendLine("沖水槽:馬達位置 #3 格式錯誤");
            if (!parsed_mvel1) errors.AppendLine("沖水槽:馬達速度 #1 格式錯誤");
            if (!parsed_mvel2) errors.AppendLine("沖水槽:馬達速度 #2 格式錯誤");

            int conv_low = parsed_low ? (int)Math.Round(input_low / transfer) :0;
            int conv_high = parsed_high ? (int)Math.Round(input_high / transfer) :0;

            if (parsed_low && parsed_high)
            {
                if (!(conv_low < conv_high)) errors.AppendLine("沖水槽:低設定必須小於高設定");
            }

            // enforce unit limits for sink SV low/high
            if (parsed_low && _unitSettings?.Sink != null)
            {
                var limLow = _unitSettings.Sink.SV_Low_Limit;
                if (conv_low < limLow) errors.AppendLine($"沖水槽:低設定 ({conv_low}) 小於下限 ({limLow})");
            }
            if (parsed_high && _unitSettings?.Sink != null)
            {
                var limHigh = _unitSettings.Sink.SV_High_Limit;
                if (conv_high > limHigh) errors.AppendLine($"沖水槽: 高設定 ({conv_high}) 大於上限 ({limHigh})");
            }

            if (parsed_time && _unitSettings?.Sink != null)
            {
                var limTime = _unitSettings.Sink.ActTime_Limit_Second;
                if (input_time > limTime) errors.AppendLine($"沖水槽: 動作時間 ({input_time}) 大於上限 ({limTime}) 秒");
            }

            // motor positions/vel converted to int by rounding after dividing by motorTransfer
            int conv_mpos1 = parsed_mpos1 ? (int)Math.Round(input_mpos1 / motorTransfer) :0;
            int conv_mpos2 = parsed_mpos2 ? (int)Math.Round(input_mpos2 / motorTransfer) :0;
            int conv_mpos3 = parsed_mpos3 ? (int)Math.Round(input_mpos3 / motorTransfer) :0;
            int conv_mvel1 = parsed_mvel1 ? (int)Math.Round(input_mvel1 / motorTransfer) :0;
            int conv_mvel2 = parsed_mvel2 ? (int)Math.Round(input_mvel2 / motorTransfer) :0;

            if (errors.Length >0)
            {
                MessageBox.Show(errors.ToString(), "驗證錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_moduleSettings.Sink == null) _moduleSettings.Sink = new MS_Sink();

            _moduleSettings.Sink.SV_Low = conv_low;
            _moduleSettings.Sink.SV_High = conv_high;
            _moduleSettings.Sink.ActTime_Second = parsed_time ? input_time : _moduleSettings.Sink.ActTime_Second;

            _moduleSettings.Sink.MotorPosition_01 = conv_mpos1;
            _moduleSettings.Sink.MotorPosition_02 = conv_mpos2;
            _moduleSettings.Sink.MotorPosition_03 = conv_mpos3;
            _moduleSettings.Sink.MotorVelocity_01 = conv_mvel1;
            _moduleSettings.Sink.MotorVelocity_02 = conv_mvel2;

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
                            diModule.Sink = _moduleSettings.Sink;
                        }
                    }
                    catch { }
                }

                MessageBox.Show("寫入完成", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"寫入失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
