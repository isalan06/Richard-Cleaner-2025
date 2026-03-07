using CleanerControlApp.Utilities;
using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CleanerControlApp.Vision.SettingViews
{
    public partial class SetHeatingTankView : UserControl
    {
        private ModuleSettings? _moduleSettings;
        private UnitSettings? _unitSettings;

        public SetHeatingTankView()
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
                catch { }
            }

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
        }

        private void _unit_settings_assign(UnitSettings? unit)
        {
            _unitSettings = unit;
        }

        private TextBox? GetTextBox(string name) => this.FindName(name) as TextBox;

        private void LoadToUI()
        {
            if (_moduleSettings?.HeatingTank == null)
                return;

            var m = _moduleSettings.HeatingTank;
            var u = _unitSettings?.HeatingTank;
            var transfer = (u != null && Math.Abs(u.UnitTransfer) > 0.000001f) ? u.UnitTransfer : 1f;

            var tbLow = GetTextBox("Txt_SV_Low");
            var tbHigh = GetTextBox("Txt_SV_High");
            var tbINV_H = GetTextBox("Txt_INV_High");
            var tbINV_L = GetTextBox("Txt_INV_Low");
            var tbINV_Zero = GetTextBox("Txt_INV_Zero");
            var tbWaterH = GetTextBox("Txt_Water_H_CheckDelay");
            var tbWaterL = GetTextBox("Txt_Water_L_CheckDelay");

            if (tbLow != null) tbLow.Text = (m.SV_Low * transfer).ToString(CultureInfo.InvariantCulture);
            if (tbHigh != null) tbHigh.Text = (m.SV_High * transfer).ToString(CultureInfo.InvariantCulture);

            if (tbINV_H != null) tbINV_H.Text = m.INV_High.ToString(CultureInfo.InvariantCulture);
            if (tbINV_L != null) tbINV_L.Text = m.INV_Low.ToString(CultureInfo.InvariantCulture);
            if (tbINV_Zero != null) tbINV_Zero.Text = m.INV_Zero.ToString(CultureInfo.InvariantCulture);

            if (tbWaterH != null) tbWaterH.Text = m.Water_H_CheckDelay_Second.ToString(CultureInfo.InvariantCulture);
            if (tbWaterL != null) tbWaterL.Text = m.Water_L_CheckDelay_Second.ToString(CultureInfo.InvariantCulture);
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
            MessageBox.Show("讀取完成", "讀取", MessageBoxButton.OK, MessageBoxImage.Information);
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
            var tbINV_H = GetTextBox("Txt_INV_High");
            var tbINV_L = GetTextBox("Txt_INV_Low");
            var tbINV_Zero = GetTextBox("Txt_INV_Zero");
            var tbWaterH = GetTextBox("Txt_Water_H_CheckDelay");
            var tbWaterL = GetTextBox("Txt_Water_L_CheckDelay");

            var errors = new StringBuilder();

            var u = _unitSettings?.HeatingTank;
            var transfer = (u != null && Math.Abs(u.UnitTransfer) > 0.000001f) ? u.UnitTransfer : 1f;

            float input_low = 0f, input_high = 0f, input_inv_h = 0f, input_inv_l = 0f, input_inv_zero = 0f;
            int input_water_h = 0, input_water_l = 0;

            bool parsed_low = tbLow != null && float.TryParse(tbLow.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_low);
            bool parsed_high = tbHigh != null && float.TryParse(tbHigh.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_high);

            bool parsed_inv_h = tbINV_H != null && float.TryParse(tbINV_H.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_inv_h);
            bool parsed_inv_l = tbINV_L != null && float.TryParse(tbINV_L.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_inv_l);
            bool parsed_inv_zero = tbINV_Zero != null && float.TryParse(tbINV_Zero.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_inv_zero);

            bool parsed_water_h = tbWaterH != null && int.TryParse(tbWaterH.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out input_water_h);
            bool parsed_water_l = tbWaterL != null && int.TryParse(tbWaterL.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out input_water_l);

            if (!parsed_low) errors.AppendLine("錯誤:低溫設定值格式不正確");
            if (!parsed_high) errors.AppendLine("錯誤: 高溫設定值格式不正確");
            if (!parsed_inv_h) errors.AppendLine("錯誤: INV 高頻率格式不正確");
            if (!parsed_inv_l) errors.AppendLine("錯誤: INV低頻率格式不正確");
            if (!parsed_inv_zero) errors.AppendLine("錯誤: INV 零點格式不正確");
            if (!parsed_water_h) errors.AppendLine("錯誤: 高水位檢查延遲格式不正確");
            if (!parsed_water_l) errors.AppendLine("錯誤:低水位檢查延遲格式不正確");

            int conv_low = parsed_low ? (int)Math.Round(input_low / transfer) : 0;
            int conv_high = parsed_high ? (int)Math.Round(input_high / transfer) : 0;

            if (parsed_low && parsed_high)
            {
                if (!(conv_low < conv_high)) errors.AppendLine("錯誤:低溫設定值必須小於高溫設定值");
            }

            if (parsed_low && _unitSettings?.HeatingTank != null)
            {
                var limLow = _unitSettings.HeatingTank.SV_Low_Limit;
                if (conv_low < limLow) errors.AppendLine($"錯誤:低溫設定值 ({conv_low}) 小於允許下限 ({limLow})");
            }
            if (parsed_high && _unitSettings?.HeatingTank != null)
            {
                var limHigh = _unitSettings.HeatingTank.SV_High_Limit;
                if (conv_high > limHigh) errors.AppendLine($"錯誤: 高溫設定值 ({conv_high}) 大於允許上限 ({limHigh})");
            }

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "輸入錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_moduleSettings.HeatingTank == null) _moduleSettings.HeatingTank = new MS_HeatingTank();

            _moduleSettings.HeatingTank.SV_Low = conv_low;
            _moduleSettings.HeatingTank.SV_High = conv_high;
            _moduleSettings.HeatingTank.INV_High = parsed_inv_h ? input_inv_h : _moduleSettings.HeatingTank.INV_High;
            _moduleSettings.HeatingTank.INV_Low = parsed_inv_l ? input_inv_l : _moduleSettings.HeatingTank.INV_Low;
            _moduleSettings.HeatingTank.INV_Zero = parsed_inv_zero ? input_inv_zero : _moduleSettings.HeatingTank.INV_Zero;
            _moduleSettings.HeatingTank.Water_H_CheckDelay_Second = parsed_water_h ? input_water_h : _moduleSettings.HeatingTank.Water_H_CheckDelay_Second;
            _moduleSettings.HeatingTank.Water_L_CheckDelay_Second = parsed_water_l ? input_water_l : _moduleSettings.HeatingTank.Water_L_CheckDelay_Second;

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
                            diModule.HeatingTank = _moduleSettings.HeatingTank;
                        }
                    }
                    catch { }
                }

                MessageBox.Show("寫入完成", "寫入", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"寫入失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
