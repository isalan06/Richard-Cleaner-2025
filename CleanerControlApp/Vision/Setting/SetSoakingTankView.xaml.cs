using CleanerControlApp.Utilities;
using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CleanerControlApp.Vision.SettingViews
{
    public partial class SetSoakingTankView : UserControl
    {
        private ModuleSettings _moduleSettings;
        private UnitSettings _unitSettings;

        public SetSoakingTankView()
        {
            InitializeComponent();

            // assign title and labels at runtime to avoid XAML encoding issues
            if (this.FindName("LblTitle") is TextBlock tb) tb.Text = "浸泡槽參數";
            if (this.FindName("HdrSoaking") is TextBlock hdr) hdr.Text = "浸泡槽";
            if (this.FindName("TB_ActTime_Label") is TextBlock l3) l3.Text = "浸泡時間(秒):";
            if (this.FindName("TB_MotorPos1_Label") is TextBlock l4) l4.Text = "馬達位置 #1-承接位(mm):";
            if (this.FindName("TB_MotorPos2_Label") is TextBlock l5) l5.Text = "馬達位置 #2-槽內位(mm):";
            if (this.FindName("TB_MotorPos3_Label") is TextBlock l6) l6.Text = "馬達位置 #3-搖擺位(mm):";
            if (this.FindName("TB_MotorVel1_Label") is TextBlock l7) l7.Text = "馬達速度 #1-升降(mm/s):";
            if (this.FindName("TB_MotorVel2_Label") is TextBlock l8) l8.Text = "馬達速度 #2-搖擺(mm/s):";
            if (this.FindName("TB_AirRetry_Label") is TextBlock l9) l9.Text = "風刀往復次數:";
            if (this.FindName("TB_UltrasonicSet_Label") is TextBlock lu) lu.Text = "超音波設定電流 (mA):";

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

            var tbAir = GetTextBox("Txt_AirKnifeRetryCount");
            if (tbAir != null)
            {
                tbAir.PreviewTextInput += Txt_AirKnifeRetryCount_PreviewTextInput;
                tbAir.LostFocus += Txt_AirKnifeRetryCount_LostFocus;
                DataObject.AddPastingHandler(tbAir, OnAirRetryPaste);
            }

            // allow numeric input for ultrasonic textbox
            var tbUl = GetTextBox("Txt_UltrasonicSetCurrent");
            if (tbUl != null)
            {
                tbUl.PreviewTextInput += Txt_Ultrasonic_PreviewTextInput;
                DataObject.AddPastingHandler(tbUl, OnUltrasonicPaste);
            }
        }

        // helper to safely assign unit fields
        private void _unit_settings_assign(UnitSettings unit)
        {
            _unitSettings = unit;
        }

        private TextBox? GetTextBox(string name) => this.FindName(name) as TextBox;

        private void LoadToUI()
        {
            if (_moduleSettings?.SoakingTank == null)
                return;

            var m = _moduleSettings.SoakingTank;
            var u = _unitSettings?.SoakingTank;
            var motorTransfer = (u != null && Math.Abs(u.MotorUnitTransfer) >0.000001f) ? u.MotorUnitTransfer :1f;

            var tbTime = GetTextBox("Txt_ActTime");
            var tbMPos1 = GetTextBox("Txt_MotorPos1");
            var tbMPos2 = GetTextBox("Txt_MotorPos2");
            var tbMPos3 = GetTextBox("Txt_MotorPos3");
            var tbMVel1 = GetTextBox("Txt_MotorVel1");
            var tbMVel2 = GetTextBox("Txt_MotorVel2");
            var tbAirRetry = GetTextBox("Txt_AirKnifeRetryCount");
            var tbUltrasonic = GetTextBox("Txt_UltrasonicSetCurrent");

            if (tbTime != null) tbTime.Text = m.ActTime_Second.ToString(CultureInfo.InvariantCulture);

            if (tbMPos1 != null) tbMPos1.Text = (m.MotorPosition_01 * motorTransfer).ToString(CultureInfo.InvariantCulture);
            if (tbMPos2 != null) tbMPos2.Text = (m.MotorPosition_02 * motorTransfer).ToString(CultureInfo.InvariantCulture);
            if (tbMPos3 != null) tbMPos3.Text = (m.MotorPosition_03 * motorTransfer).ToString(CultureInfo.InvariantCulture);
            if (tbMVel1 != null) tbMVel1.Text = (m.MotorVelocity_01 * motorTransfer).ToString(CultureInfo.InvariantCulture);
            if (tbMVel2 != null) tbMVel2.Text = (m.MotorVelocity_02 * motorTransfer).ToString(CultureInfo.InvariantCulture);

            if (tbAirRetry != null) tbAirRetry.Text = m.AirKnifeRetryCount.ToString(CultureInfo.InvariantCulture);

            if (tbUltrasonic != null) tbUltrasonic.Text = m.UltrasonicSetCurrent.ToString(CultureInfo.InvariantCulture);
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
            MessageBox.Show("讀取完成", "資訊", MessageBoxButton.OK, MessageBoxImage.Information);
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

            var tbTime = GetTextBox("Txt_ActTime");
            var tbMPos1 = GetTextBox("Txt_MotorPos1");
            var tbMPos2 = GetTextBox("Txt_MotorPos2");
            var tbMPos3 = GetTextBox("Txt_MotorPos3");
            var tbMVel1 = GetTextBox("Txt_MotorVel1");
            var tbMVel2 = GetTextBox("Txt_MotorVel2");
            var tbAirRetry = GetTextBox("Txt_AirKnifeRetryCount");
            var tbUltrasonic = GetTextBox("Txt_UltrasonicSetCurrent");

            var errors = new StringBuilder();

            var u = _unitSettings?.SoakingTank;
            var motorTransfer = (u != null && Math.Abs(u.MotorUnitTransfer) >0.000001f) ? u.MotorUnitTransfer :1f;

            // inputs
            int input_time =0;
            int input_airRetry =0;
            float input_mpos1 =0, input_mpos2 =0, input_mpos3 =0, input_mvel1 =0, input_mvel2 =0;
            float input_ultrasonic =0f;

            bool parsed_time = tbTime != null && int.TryParse(tbTime.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out input_time);
            bool parsed_mpos1 = tbMPos1 != null && float.TryParse(tbMPos1.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_mpos1);
            bool parsed_mpos2 = tbMPos2 != null && float.TryParse(tbMPos2.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_mpos2);
            bool parsed_mpos3 = tbMPos3 != null && float.TryParse(tbMPos3.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_mpos3);
            bool parsed_mvel1 = tbMVel1 != null && float.TryParse(tbMVel1.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_mvel1);
            bool parsed_mvel2 = tbMVel2 != null && float.TryParse(tbMVel2.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_mvel2);
            bool parsed_airRetry = tbAirRetry != null && int.TryParse(tbAirRetry.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out input_airRetry);
            bool parsed_ultrasonic = tbUltrasonic != null && float.TryParse(tbUltrasonic.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out input_ultrasonic);

            if (!parsed_time) errors.AppendLine("錯誤: 浸泡時間輸入錯誤");
            if (!parsed_mpos1) errors.AppendLine("錯誤: 馬達位置 #1 輸入錯誤");
            if (!parsed_mpos2) errors.AppendLine("錯誤: 馬達位置 #2 輸入錯誤");
            if (!parsed_mpos3) errors.AppendLine("錯誤: 馬達位置 #3 輸入錯誤");
            if (!parsed_mvel1) errors.AppendLine("錯誤: 馬達速度 #1 輸入錯誤");
            if (!parsed_mvel2) errors.AppendLine("錯誤: 馬達速度 #2 輸入錯誤");
            if (!parsed_airRetry) errors.AppendLine("錯誤:風刀往復次數輸入錯誤");
            if (!parsed_ultrasonic) errors.AppendLine("錯誤: 超音波設定電流輸入錯誤");

            if (parsed_airRetry)
            {
                if (input_airRetry <0) errors.AppendLine("錯誤:風刀往復次數不能為負數");
            }

            // determine ultrasonic max limit via reflection to avoid direct dependency
            float ultrasonicMax = float.MaxValue;
            if (u != null)
            {
                try
                {
                    var prop = u.GetType().GetProperty("UltrasonicCurrentMaxLimit");
                    if (prop != null)
                    {
                        var val = prop.GetValue(u);
                        if (val is float f) ultrasonicMax = f;
                        else if (val is double d) ultrasonicMax = (float)d;
                        else if (val is int i) ultrasonicMax = i;
                    }
                }
                catch { }
            }

            if (parsed_ultrasonic)
            {
                if (input_ultrasonic <0f) errors.AppendLine("錯誤: 超音波設定電流不能為負值");
                else if (input_ultrasonic > ultrasonicMax) errors.AppendLine($"錯誤: 超音波設定電流({input_ultrasonic}) 超過上限 ({ultrasonicMax})");
            }

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

            if (_moduleSettings.SoakingTank == null) _moduleSettings.SoakingTank = new MS_SoakingTank();

            _moduleSettings.SoakingTank.ActTime_Second = parsed_time ? input_time : _moduleSettings.SoakingTank.ActTime_Second;

            _moduleSettings.SoakingTank.MotorPosition_01 = conv_mpos1;
            _moduleSettings.SoakingTank.MotorPosition_02 = conv_mpos2;
            _moduleSettings.SoakingTank.MotorPosition_03 = conv_mpos3;
            _moduleSettings.SoakingTank.MotorVelocity_01 = conv_mvel1;
            _moduleSettings.SoakingTank.MotorVelocity_02 = conv_mvel2;

            _moduleSettings.SoakingTank.AirKnifeRetryCount = parsed_airRetry ? input_airRetry : _moduleSettings.SoakingTank.AirKnifeRetryCount;

            // assign ultrasonic value
            if (parsed_ultrasonic)
            {
                _moduleSettings.SoakingTank.UltrasonicSetCurrent = input_ultrasonic;
            }

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
                            diModule.SoakingTank = _moduleSettings.SoakingTank;
                        }
                    }
                    catch { }
                }

                MessageBox.Show("寫入完成", "資訊", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"寫入失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // +/- button handlers for AirKnifeRetryCount
        private void Btn_AirInc_Click(object sender, RoutedEventArgs e)
        {
            var tb = GetTextBox("Txt_AirKnifeRetryCount");
            if (tb == null) return;
            if (!int.TryParse(tb.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) v =0;
            v++;
            tb.Text = v.ToString(CultureInfo.InvariantCulture);
        }

        private void Btn_AirDec_Click(object sender, RoutedEventArgs e)
        {
            var tb = GetTextBox("Txt_AirKnifeRetryCount");
            if (tb == null) return;
            if (!int.TryParse(tb.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) v =0;
            v = Math.Max(0, v -1);
            tb.Text = v.ToString(CultureInfo.InvariantCulture);
        }

        // restrict input to digits
        private void Txt_AirKnifeRetryCount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        // sanitize pasted text
        private void OnAirRetryPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!int.TryParse(text, out _))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        // ensure textbox has at least zero
        private void Txt_AirKnifeRetryCount_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            if (!int.TryParse(tb.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) v =0;
            if (v <0) v =0;
            tb.Text = v.ToString(CultureInfo.InvariantCulture);
        }

        // ultrasonic input handlers
        private void Txt_Ultrasonic_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!(char.IsDigit(c) || c == '.' || c == ','))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void OnUltrasonicPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        // helper to avoid inlining multiple assignments (keeps edits clearer)
        private void _module_settings_assign_motor_vel(ref MS_SoakingTank sink, int v1, int v2)
        {
            if (sink != null)
            {
                sink.MotorVelocity_01 = v1;
                sink.MotorVelocity_02 = v2;
            }
        }
    }
}
