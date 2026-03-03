using CleanerControlApp.Utilities;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CleanerControlApp.Vision.SettingViews
{
    public partial class SetDryinTankView : UserControl
    {
        private ModuleSettings _moduleSettings;
        private UnitSettings _unitSettings;

        public SetDryinTankView()
        {
            InitializeComponent();

            // Load current settings
            _moduleSettings = ConfigLoader.GetModuleSettings();
            _unitSettings = ConfigLoader.GetUnitSettings();
            LoadToUI();
        }

        private void LoadToUI()
        {
            // If there are no module drying tank entries, nothing to show
            if (_moduleSettings?.DryingTanks == null || _moduleSettings.DryingTanks.Count ==0)
                return;

            // get unit list if available (may be null)
            var unitList = _unitSettings?.DryingTanks;

            // Tank1
            if (_moduleSettings.DryingTanks.Count >0)
            {
                var m = _moduleSettings.DryingTanks[0];
                var u = (unitList != null && unitList.Count >0) ? unitList[0] : null;
                var transfer = (u != null && Math.Abs(u.UnitTransfer) >0.000001f) ? u.UnitTransfer :1f;

                float displaySVLow = m.SV_Low * transfer;
                float displaySVHigh = m.SV_High * transfer;

                Txt1_SV_Low.Text = displaySVLow.ToString(CultureInfo.InvariantCulture);
                Txt1_SV_High.Text = displaySVHigh.ToString(CultureInfo.InvariantCulture);
                Txt1_ActTime.Text = m.ActTime_Second.ToString(CultureInfo.InvariantCulture);
            }

            // Tank2
            if (_moduleSettings.DryingTanks.Count >1)
            {
                var m = _moduleSettings.DryingTanks[1];
                var u = (unitList != null && unitList.Count >1) ? unitList[1] : null;
                var transfer = (u != null && Math.Abs(u.UnitTransfer) >0.000001f) ? u.UnitTransfer :1f;

                float displaySVLow = m.SV_Low * transfer;
                float displaySVHigh = m.SV_High * transfer;

                Txt2_SV_Low.Text = displaySVLow.ToString(CultureInfo.InvariantCulture);
                Txt2_SV_High.Text = displaySVHigh.ToString(CultureInfo.InvariantCulture);
                Txt2_ActTime.Text = m.ActTime_Second.ToString(CultureInfo.InvariantCulture);
            }
        }

        // helper to avoid null deref in older code
        private bool _unit_settings_safe(UnitSettings settings) => settings != null && settings.DryingTanks != null;

        private void BtnRead_Click(object sender, RoutedEventArgs e)
        {
            _moduleSettings = ConfigLoader.GetModuleSettings();
            _unitSettings = ConfigLoader.GetUnitSettings();
            LoadToUI();
            MessageBox.Show("讀取完成", "訊息", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnWrite_Click(object sender, RoutedEventArgs e)
        {
            if (_moduleSettings == null)
            {
                _moduleSettings = new ModuleSettings();
                _moduleSettings.DryingTanks = new System.Collections.Generic.List<MS_DryingTanks>();
            }

            if (_unit_settings_safe(_unitSettings) == false)
            {
                _unitSettings = ConfigLoader.GetUnitSettings();
                if (_unitSettings == null) _unitSettings = new UnitSettings { DryingTanks = new System.Collections.Generic.List<DryingTanks>() };
            }

            // ensure at least2 entries
            while (_moduleSettings.DryingTanks.Count <2)
            {
                _moduleSettings.DryingTanks.Add(new MS_DryingTanks());
            }
            while (_unit_settings_safe(_unitSettings) && _unitSettings.DryingTanks.Count <2)
            {
                _unitSettings.DryingTanks.Add(new DryingTanks { UnitTransfer =1f });
            }

            var errors = new StringBuilder();

            // Tank1: parse inputs
            var unit1 = (_unit_settings_safe(_unitSettings) && _unitSettings.DryingTanks.Count >0) ? _unitSettings.DryingTanks[0] : new DryingTanks { UnitTransfer =1f };
            var transfer1 = (unit1 != null && Math.Abs(unit1.UnitTransfer) >0.000001f) ? unit1.UnitTransfer :1f;

            bool parsed1_low = float.TryParse(Txt1_SV_Low.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float input1_low);
            bool parsed1_high = float.TryParse(Txt1_SV_High.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float input1_high);
            bool parsed1_time = int.TryParse(Txt1_ActTime.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int input1_time);

            if (!parsed1_low) errors.AppendLine("烘乾槽 #1: SV_Low不是有效數字。");
            if (!parsed1_high) errors.AppendLine("烘乾槽 #1: SV_High不是有效數字。");
            if (!parsed1_time) errors.AppendLine("烘乾槽 #1: ActTime_Second不是有效整數。");

            int conv1_low = parsed1_low ? (int)Math.Round(input1_low / transfer1) :0;
            int conv1_high = parsed1_high ? (int)Math.Round(input1_high / transfer1) :0;

            if (parsed1_low && parsed1_high)
            {
                if (!(conv1_low < conv1_high)) errors.AppendLine("烘乾槽 #1: SV_Low 必須小於 SV_High。");
            }

            if (parsed1_low && _unit_settings_safe(_unitSettings) && _unitSettings.DryingTanks.Count >0)
            {
                var limLow = _unitSettings.DryingTanks[0].SV_Low_Limit;
                if (conv1_low < limLow) errors.AppendLine($"烘乾槽 #1: SV_Low ({conv1_low}) 小於允許下限 ({limLow})。");
            }
            if (parsed1_high && _unit_settings_safe(_unitSettings) && _unitSettings.DryingTanks.Count >0)
            {
                var limHigh = _unitSettings.DryingTanks[0].SV_High_Limit;
                if (conv1_high > limHigh) errors.AppendLine($"烘乾槽 #1: SV_High ({conv1_high}) 大於允許上限 ({limHigh})。");
            }
            if (parsed1_time && _unit_settings_safe(_unitSettings) && _unitSettings.DryingTanks.Count >0)
            {
                var limTime = _unitSettings.DryingTanks[0].ActTime_Limit_Second;
                if (input1_time > limTime) errors.AppendLine($"烘乾槽 #1: ActTime_Second ({input1_time}) 大於允許上限 ({limTime}) 秒。");
            }

            // Tank2: parse inputs
            var unit2 = (_unit_settings_safe(_unitSettings) && _unitSettings.DryingTanks.Count >1) ? _unitSettings.DryingTanks[1] : new DryingTanks { UnitTransfer =1f };
            var transfer2 = (unit2 != null && Math.Abs(unit2.UnitTransfer) >0.000001f) ? unit2.UnitTransfer :1f;

            bool parsed2_low = float.TryParse(Txt2_SV_Low.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float input2_low);
            bool parsed2_high = float.TryParse(Txt2_SV_High.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float input2_high);
            bool parsed2_time = int.TryParse(Txt2_ActTime.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int input2_time);

            if (!parsed2_low) errors.AppendLine("烘乾槽 #2: SV_Low不是有效數字。");
            if (!parsed2_high) errors.AppendLine("烘乾槽 #2: SV_High不是有效數字。");
            if (!parsed2_time) errors.AppendLine("烘乾槽 #2: ActTime_Second不是有效整數。");

            int conv2_low = parsed2_low ? (int)Math.Round(input2_low / transfer2) :0;
            int conv2_high = parsed2_high ? (int)Math.Round(input2_high / transfer2) :0;

            if (parsed2_low && parsed2_high)
            {
                if (!(conv2_low < conv2_high)) errors.AppendLine("烘乾槽 #2: SV_Low 必須小於 SV_High。");
            }

            if (parsed2_low && _unit_settings_safe(_unitSettings) && _unitSettings.DryingTanks.Count >1)
            {
                var limLow = _unitSettings.DryingTanks[1].SV_Low_Limit;
                if (conv2_low < limLow) errors.AppendLine($"烘乾槽 #2: SV_Low ({conv2_low}) 小於允許下限 ({limLow})。");
            }
            if (parsed2_high && _unit_settings_safe(_unitSettings) && _unitSettings.DryingTanks.Count >1)
            {
                var limHigh = _unitSettings.DryingTanks[1].SV_High_Limit;
                if (conv2_high > limHigh) errors.AppendLine($"烘乾槽 #2: SV_High ({conv2_high}) 大於允許上限 ({limHigh})。");
            }
            if (parsed2_time && _unit_settings_safe(_unitSettings) && _unitSettings.DryingTanks.Count >1)
            {
                var limTime = _unitSettings.DryingTanks[1].ActTime_Limit_Second;
                if (input2_time > limTime) errors.AppendLine($"烘乾槽 #2: ActTime_Second ({input2_time}) 大於允許上限 ({limTime}) 秒。");
            }

            // If any validation errors, show and abort
            if (errors.Length >0)
            {
                MessageBox.Show(errors.ToString(), "驗證失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // All validations passed - write back to module settings
            // Tank1
            _moduleSettings.DryingTanks[0].SV_Low = conv1_low;
            _moduleSettings.DryingTanks[0].SV_High = conv1_high;
            _moduleSettings.DryingTanks[0].ActTime_Second = parsed1_time ? input1_time : _moduleSettings.DryingTanks[0].ActTime_Second;

            // Tank2
            _moduleSettings.DryingTanks[1].SV_Low = conv2_low;
            _moduleSettings.DryingTanks[1].SV_High = conv2_high;
            _moduleSettings.DryingTanks[1].ActTime_Second = parsed2_time ? input2_time : _moduleSettings.DryingTanks[1].ActTime_Second;

            try
            {
                ConfigLoader.SetModuleSettings(_moduleSettings);
                MessageBox.Show("寫入並儲存設定完成", "訊息", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"寫入失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
