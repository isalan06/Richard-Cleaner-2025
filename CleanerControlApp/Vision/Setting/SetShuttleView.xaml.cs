using CleanerControlApp.Utilities;
using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using CleanerControlApp.Hardwares.Shuttle.Models;

namespace CleanerControlApp.Vision.SettingViews
{
    public partial class SetShuttleView : UserControl
    {
        private ModuleSettings? _moduleSettings;
        private UnitSettings? _unitSettings;

        public SetShuttleView()
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

        private void BuildMotorXPosControls(float unitTransfer)
        {
            Sp_MotorX_PosList.Children.Clear();
            for (int i = 0; i < ShuttleXMotorName.Name.Length; i++)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                var lbl = new TextBlock { Text = ShuttleXMotorName.Name[i], Width = 140, VerticalAlignment = VerticalAlignment.Center };
                var tb = new TextBox { Width =120, Name = $"Tb_MotorX_Pos_{i}", Margin = new Thickness(8,0,0,0) };
                panel.Children.Add(lbl);
                panel.Children.Add(tb);
                Sp_MotorX_PosList.Children.Add(panel);
                // register name so FindName can be used if needed
                try { this.RegisterName(tb.Name, tb); } catch { }
            }
        }

        private void BuildMotorZPosControls(float unitTransfer)
        {
            Sp_MotorZ_PosList.Children.Clear();
            for (int i = 0; i < ShuttleZMotorName.Name.Length; i++)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                var lbl = new TextBlock { Text = ShuttleZMotorName.Name[i], Width = 140, VerticalAlignment = VerticalAlignment.Center };
                var tb = new TextBox { Width =120, Name = $"Tb_MotorZ_Pos_{i}", Margin = new Thickness(8,0,0,0) };
                panel.Children.Add(lbl);
                panel.Children.Add(tb);
                Sp_MotorZ_PosList.Children.Add(panel);
                // register name so FindName can be used if needed
                try { this.RegisterName(tb.Name, tb); } catch { }
            }
        }

        private TextBox? FindPosZTextBox(int index)
        {
            var name = $"Tb_MotorZ_Pos_{index}";
            try { return this.FindName(name) as TextBox; } catch { return null; }
        }

        private TextBox? FindPosTextBox(int index)
        {
            var name = $"Tb_MotorX_Pos_{index}";
            try { return this.FindName(name) as TextBox; } catch { return null; }
        }

        private void LoadToUI()
        {
            if (_moduleSettings == null)
                return;

            var sh = _moduleSettings.Shuttle;
            var motors = _moduleSettings.Motors;

            var tbStable = GetTextBox("Txt_Shuttle_ZAxis_StableTime");
            var tbMx0 = GetTextBox("Txt_MotorX_Vel0");
            var tbMx1 = GetTextBox("Txt_MotorX_Vel1");
            var tbMz0 = GetTextBox("Txt_MotorZ_Vel0");
            var tbMz1 = GetTextBox("Txt_MotorZ_Vel1");
            // positions now per-control

            if (tbStable != null) tbStable.Text = sh?.Shuttle_ZAxis_StableTime_Second.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

            // determine unit transfer for motors[0] and motors[1]
            float unitTransferX = 1f;
            float unitTransferZ = 1f;
            try
            {
                if (_unitSettings?.Motors != null && _unitSettings.Motors.Count > 0 && _unitSettings.Motors[0] != null)
                    unitTransferX = _unitSettings.Motors[0].UnitTransfer;
            }
            catch { unitTransferX = 1f; }
            try
            {
                if (_unitSettings?.Motors != null && _unitSettings.Motors.Count > 1 && _unitSettings.Motors[1] != null)
                    unitTransferZ = _unitSettings.Motors[1].UnitTransfer;
            }
            catch { unitTransferZ = 1f; }

            // build controls for motor X positions based on ShuttleXMotorName
            BuildMotorXPosControls(unitTransferX);
            // build controls for motor Z positions
            BuildMotorZPosControls(unitTransferZ);

            if (motors != null && motors.Count > 0 && motors[0]?.Velocities != null)
            {
                var v = motors[0].Velocities;
                if (tbMx0 != null) tbMx0.Text = v.Count > 0 ? ((v[0] * unitTransferX).ToString(CultureInfo.InvariantCulture)) : string.Empty;
                if (tbMx1 != null) tbMx1.Text = v.Count > 1 ? ((v[1] * unitTransferX).ToString(CultureInfo.InvariantCulture)) : string.Empty;
                // populate per-parameter positions
                for (int i = 0; i < ShuttleXMotorName.Name.Length; i++)
                {
                    var tb = FindPosTextBox(i);
                    if (tb != null)
                    {
                        if (motors[0].Positions != null && i < motors[0].Positions.Count)
                        {
                            // display converted value
                            float conv = motors[0].Positions[i] * unitTransferX;
                            tb.Text = conv.ToString(CultureInfo.InvariantCulture);
                        }
                        else tb.Text = string.Empty;
                    }
                }
            }
            else
            {
                if (tbMx0 != null) tbMx0.Text = string.Empty;
                if (tbMx1 != null) tbMx1.Text = string.Empty;
                // clear per-parameter
                for (int i = 0; i < ShuttleXMotorName.Name.Length; i++)
                {
                    var tb = FindPosTextBox(i);
                    if (tb != null) tb.Text = string.Empty;
                }
            }

            if (motors != null && motors.Count > 1 && motors[1]?.Velocities != null)
            {
                var v = motors[1].Velocities;
                if (tbMz0 != null) tbMz0.Text = v.Count > 0 ? ((v[0] * unitTransferZ).ToString(CultureInfo.InvariantCulture)) : string.Empty;
                if (tbMz1 != null) tbMz1.Text = v.Count > 1 ? ((v[1] * unitTransferZ).ToString(CultureInfo.InvariantCulture)) : string.Empty;
                // populate Z per-parameter positions if present
                for (int i = 0; i < ShuttleZMotorName.Name.Length; i++)
                {
                    var tb = FindPosZTextBox(i);
                    if (tb != null)
                    {
                        if (motors.Count > 1 && motors[1].Positions != null && i < motors[1].Positions.Count)
                        {
                            float conv = motors[1].Positions[i] * unitTransferZ;
                            tb.Text = conv.ToString(CultureInfo.InvariantCulture);
                        }
                        else tb.Text = string.Empty;
                    }
                }
            }
            else
            {
                if (tbMz0 != null) tbMz0.Text = string.Empty;
                if (tbMz1 != null) tbMz1.Text = string.Empty;
                for (int i = 0; i < ShuttleZMotorName.Name.Length; i++)
                {
                    var tb = FindPosZTextBox(i);
                    if (tb != null) tb.Text = string.Empty;
                }
            }
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

            var tbStable = GetTextBox("Txt_Shuttle_ZAxis_StableTime");
            var tbMx0 = GetTextBox("Txt_MotorX_Vel0");
            var tbMx1 = GetTextBox("Txt_MotorX_Vel1");
            var tbMz0 = GetTextBox("Txt_MotorZ_Vel0");
            var tbMz1 = GetTextBox("Txt_MotorZ_Vel1");
            // positions per-control

            var errors = new StringBuilder();

            int stable = 0;
            int.TryParse(tbStable?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out stable);

            // determine unit transfer for motors
            float unitTransferX = 1f;
            float unitTransferZ = 1f;
            try
            {
                if (_unitSettings?.Motors != null && _unitSettings.Motors.Count > 0 && _unitSettings.Motors[0] != null)
                    unitTransferX = _unitSettings.Motors[0].UnitTransfer;
            }
            catch { unitTransferX = 1f; }
            try
            {
                if (_unitSettings?.Motors != null && _unitSettings.Motors.Count > 1 && _unitSettings.Motors[1] != null)
                    unitTransferZ = _unitSettings.Motors[1].UnitTransfer;
            }
            catch { unitTransferZ = 1f; }

            // parse velocities from UI (these are in converted units) and convert back to module integers
            var mxvels = new List<int>();
            var mzvels = new List<int>();

            float parsedV;
            if (tbMx0 != null && float.TryParse(tbMx0.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedV))
                mxvels.Add((int)Math.Round(parsedV / unitTransferX));
            else mxvels.Add(0);

            if (tbMx1 != null && float.TryParse(tbMx1.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedV))
                mxvels.Add((int)Math.Round(parsedV / unitTransferX));
            else mxvels.Add(0);

            if (tbMz0 != null && float.TryParse(tbMz0.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedV))
                mzvels.Add((int)Math.Round(parsedV / unitTransferZ));
            else mzvels.Add(0);

            if (tbMz1 != null && float.TryParse(tbMz1.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedV))
                mzvels.Add((int)Math.Round(parsedV / unitTransferZ));
            else mzvels.Add(0);

            // parse positions lists from per-parameter TextBoxes
            var listMxPos = new List<int>();
            for (int i = 0; i < ShuttleXMotorName.Name.Length; i++)
            {
                var tb = FindPosTextBox(i);
                if (tb != null && float.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var fv))
                {
                    // convert back to module integer
                    listMxPos.Add((int)Math.Round(fv / unitTransferX));
                }
                else listMxPos.Add(0);
            }

            var listMzPos = new List<int>();
            for (int i = 0; i < ShuttleZMotorName.Name.Length; i++)
            {
                var tb = FindPosZTextBox(i);
                if (tb != null && float.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var fv))
                {
                    listMzPos.Add((int)Math.Round(fv / unitTransferZ));
                }
                else listMzPos.Add(0);
            }
            
            // ensure ModuleSettings structures exist
            if (_moduleSettings.Shuttle == null) _moduleSettings.Shuttle = new MS_Shuttle();
            if (_moduleSettings.Motors == null) _moduleSettings.Motors = new List<MS_Motor>();
            while (_moduleSettings.Motors.Count < 2) _moduleSettings.Motors.Add(new MS_Motor());

            _moduleSettings.Shuttle.Shuttle_ZAxis_StableTime_Second = stable;
            _moduleSettings.Motors[0].Velocities = mxvels;
            _moduleSettings.Motors[1].Velocities = mzvels;
            _moduleSettings.Motors[0].Positions = listMxPos;
            _moduleSettings.Motors[1].Positions = listMzPos;

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
                            diModule.Shuttle = _moduleSettings.Shuttle;
                            diModule.Motors = _moduleSettings.Motors;
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
