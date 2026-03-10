using System;
using System.Windows.Controls;
using System.Windows;
using CleanerControlApp.Utilities;
using System.Globalization;
using System.Linq;

namespace CleanerControlApp.Vision.Developer
{
 /// <summary>
 /// Interaction logic for DevUnitParameterView.xaml
 /// </summary>
 public partial class DevUnitParameterView : UserControl
 {
 private UnitSettings? _unitSettings;
 private bool _isDirty = false; // true when UI changes written to memory but not saved to file
 private int _currentIndex = -1; // currently displayed tank index

 public DevUnitParameterView()
 {
 InitializeComponent();
 }

 // Public method so parent view can request a reload and UI update
 public void LoadUnitSettings()
 {
 try
 {
 // If we have unsaved in-memory changes, confirm with user before discarding
 if (_isDirty)
 {
 var result = MessageBox.Show("目前有未儲存的變更，是否要放棄？\nYes = 放棄記憶體變更，No = 保留", "未儲存變更", MessageBoxButton.YesNo, MessageBoxImage.Warning);
 if (result == MessageBoxResult.No)
 {
 // keep current in-memory values
 return;
 }
 // otherwise proceed to reload and clear dirty flag
 _isDirty = false;
 }

 // preserve currently selected index so we can restore after reload if possible
 int previousIndex = -1;
 try { previousIndex = cmbDryingTankIndex?.SelectedIndex ?? -1; } catch { previousIndex = -1; }

 // Ensure DI singletons and configuration are refreshed
 var op = new UnitsOperator();
 op.RefreshParameter();

 ConfigLoader.Load();
 _unitSettings = ConfigLoader.GetUnitSettings();

 // Populate combo box with exactly two items (per requirement)
 cmbDryingTankIndex.ItemsSource = Enumerable.Range(0,2).Select(i => i +1).ToList();
 if (2 >0)
 {
 // try to restore previous selection if still valid, otherwise select first
 if (previousIndex >=0 && previousIndex <2)
 {
 cmbDryingTankIndex.SelectedIndex = previousIndex;
 }
 else
 {
 cmbDryingTankIndex.SelectedIndex =0;
 }
 }

 // Populate sink UI
 LoadSinkToUi();
 // Populate heating tank UI
 LoadHeatingToUi();
 // Populate soaking tank UI
 LoadSoakingToUi();
 // Populate shuttle UI
 LoadShuttleToUi();
 // Populate motor UI (X=motors[0], Z=motors[1])
 LoadMotorsToUi();
 }
 catch
 {
 // swallow exceptions; callers may show messages
 }
 }

 private void BtnLoadParams_Click(object sender, RoutedEventArgs e)
 {
 try
 {
 LoadUnitSettings();
 MessageBox.Show("讀取完成", "讀取結果", MessageBoxButton.OK, MessageBoxImage.Information);
 }
 catch (Exception ex)
 {
 MessageBox.Show("讀取參數失敗: " + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }

 private void BtnWriteParams_Click(object sender, RoutedEventArgs e)
 {
 try
 {
 // Update current selected tank from UI
 if (_unitSettings != null && cmbDryingTankIndex.SelectedIndex >=0)
 {
 var idx = cmbDryingTankIndex.SelectedIndex;
 var list = _unitSettings.DryingTanks ?? new System.Collections.Generic.List<US_DryingTanks>();
 // ensure enough items
 while (list.Count <= idx) list.Add(new US_DryingTanks());

 var item = list[idx];

 if (float.TryParse(TxtUnitTransfer.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var ut)) item.UnitTransfer = ut;
 if (int.TryParse(TxtSVLowLimit.Text, out var svl)) item.SV_Low_Limit = svl;
 if (int.TryParse(TxtSVHighLimit.Text, out var svh)) item.SV_High_Limit = svh;
 if (int.TryParse(TxtPVLowTimeout.Text, out var pvl)) item.PV_Low_Timeout_Second = pvl;
 if (int.TryParse(TxtPVHighTimeout.Text, out var pvh)) item.PV_High_Timeout_Second = pvh;
 if (int.TryParse(TxtCoverOpenTimeout.Text, out var co)) item.Cover_Open_Timeout_Second = co;
 if (int.TryParse(TxtCoverCloseTimeout.Text, out var cc)) item.Cover_Close_Timeout_Second = cc;
 if (int.TryParse(TxtSVCheckOffset.Text, out var sco)) item.SV_CheckOffet = sco;
 if (int.TryParse(TxtActTimeLimit.Text, out var atl)) item.ActTime_Limit_Second = atl;

 // assign back
 _unitSettings.DryingTanks = list;

 // mark as dirty (in-memory changes not yet saved to file)
 _isDirty = true;

 // Save sink UI into memory as well
 SaveSinkUi();
 // Save heating UI into memory
 SaveHeatingUi();
 // Save soaking UI into memory
 SaveSoakingUi();
 // Save shuttle and motors into memory
 SaveShuttleUi();
 SaveMotorsUi();

 // Also update DI singleton (if available) so runtime consumers see changes immediately
 try
 {
 var host = global::CleanerControlApp.App.AppHost;
 if (host != null)
 {
 var diUnit = host.Services.GetService(typeof(UnitSettings)) as UnitSettings;
 if (diUnit != null)
 {
 diUnit.DryingTanks = _unitSettings.DryingTanks;
 // also propagate heating tank, sink and soaking tank if present
 diUnit.HeatingTank = _unitSettings.HeatingTank;
 diUnit.Sink = _unitSettings.Sink;
 diUnit.SoakingTank = _unitSettings.SoakingTank;
 diUnit.Shuttle = _unitSettings.Shuttle;
 diUnit.Motors = _unitSettings.Motors;
 }
 }
 }
 catch
 {
 // ignore DI update failures
 }
 }

 MessageBox.Show("已寫入記憶體參數 (尚未寫入檔案)", "寫入記憶體", MessageBoxButton.OK, MessageBoxImage.Information);
 }
 catch (Exception ex)
 {
 MessageBox.Show("寫入記憶體參數失敗: " + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }

 private void BtnSaveParams_Click(object sender, RoutedEventArgs e)
 {
 try
 {
 // First, persist current UI values into the in-memory unit settings
 SaveCurrentUiToIndex(_currentIndex);
 // Also save sink fields
 SaveSinkUi();
 // Also save heating fields
 SaveHeatingUi();
 // Also save soaking fields
 SaveSoakingUi();
 // Also save shuttle and motor fields
 SaveShuttleUi();
 SaveMotorsUi();

 if (_unitSettings != null)
 {
 // Update DI singleton if present so runtime consumers see the latest values
 try
 {
 var host = global::CleanerControlApp.App.AppHost;
 if (host != null)
 {
 var diUnit = host.Services.GetService(typeof(UnitSettings)) as UnitSettings;
 if (diUnit != null)
 {
 diUnit.DryingTanks = _unitSettings.DryingTanks;
 diUnit.Sink = _unitSettings.Sink;
 diUnit.HeatingTank = _unitSettings.HeatingTank;
 diUnit.SoakingTank = _unitSettings.SoakingTank;
 diUnit.Shuttle = _unitSettings.Shuttle;
 diUnit.Motors = _unitSettings.Motors;
 }
 }
 }
 catch
 {
 // ignore DI update failures
 }

 // Persist to appsettings.json
 ConfigLoader.SetUnitSettings(_unitSettings);
 // persisted to file, clear dirty flag
 _isDirty = false;
 MessageBox.Show("已儲存設定到 appsettings.json 並更新 DI singleton", "儲存完成", MessageBoxButton.OK, MessageBoxImage.Information);
 }
 else
 {
 MessageBox.Show("沒有可儲存的設定", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
 }
 }
 catch (Exception ex)
 {
 MessageBox.Show("儲存設定失敗: " + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }

 private void cmbDryingTankIndex_SelectionChanged(object sender, SelectionChangedEventArgs e)
 {
 try
 {
 // Save current UI values into the previously selected index to avoid losing edits
 SaveCurrentUiToIndex(_currentIndex);

 if (_unitSettings == null || _unitSettings.DryingTanks == null) return;
 var idx = cmbDryingTankIndex.SelectedIndex;
 if (idx <0) return;
 // if the list doesn't have this many items, just clear UI
 if (idx >= _unitSettings.DryingTanks.Count)
 {
 TxtUnitTransfer.Text = string.Empty;
 TxtSVLowLimit.Text = string.Empty;
 TxtSVHighLimit.Text = string.Empty;
 TxtPVLowTimeout.Text = string.Empty;
 TxtPVHighTimeout.Text = string.Empty;
 TxtCoverOpenTimeout.Text = string.Empty;
 TxtCoverCloseTimeout.Text = string.Empty;
 TxtSVCheckOffset.Text = string.Empty;
 TxtActTimeLimit.Text = string.Empty;
 _currentIndex = idx;
 return;
 }

 var item = _unitSettings.DryingTanks[idx];

 // populate UI from selected item
 TxtUnitTransfer.Text = item.UnitTransfer.ToString(CultureInfo.InvariantCulture);
 TxtSVLowLimit.Text = item.SV_Low_Limit.ToString();
 TxtSVHighLimit.Text = item.SV_High_Limit.ToString();
 TxtPVLowTimeout.Text = item.PV_Low_Timeout_Second.ToString();
 TxtPVHighTimeout.Text = item.PV_High_Timeout_Second.ToString();
 TxtCoverOpenTimeout.Text = item.Cover_Open_Timeout_Second.ToString();
 TxtCoverCloseTimeout.Text = item.Cover_Close_Timeout_Second.ToString();
 TxtSVCheckOffset.Text = item.SV_CheckOffet.ToString();
 TxtActTimeLimit.Text = item.ActTime_Limit_Second.ToString();

 // update current index
 _currentIndex = idx;
 }
 catch
 {
 // ignore
 }
 }

 // Helper: save current UI fields into the provided index of _unitSettings.DryingTanks
 private void SaveCurrentUiToIndex(int index)
 {
 try
 {
 if (index <0) return;
 if (_unitSettings == null) _unitSettings = new UnitSettings();
 var list = _unitSettings.DryingTanks ?? new System.Collections.Generic.List<US_DryingTanks>();
 while (list.Count <= index) list.Add(new US_DryingTanks());
 var item = list[index];

 // try parse and update fields; if parsing fails, keep previous values
 if (float.TryParse(TxtUnitTransfer.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var ut)) item.UnitTransfer = ut;
 if (int.TryParse(TxtSVLowLimit.Text, out var svl)) item.SV_Low_Limit = svl;
 if (int.TryParse(TxtSVHighLimit.Text, out var svh)) item.SV_High_Limit = svh;
 if (int.TryParse(TxtPVLowTimeout.Text, out var pvl)) item.PV_Low_Timeout_Second = pvl;
 if (int.TryParse(TxtPVHighTimeout.Text, out var pvh)) item.PV_High_Timeout_Second = pvh;
 if (int.TryParse(TxtCoverOpenTimeout.Text, out var co)) item.Cover_Open_Timeout_Second = co;
 if (int.TryParse(TxtCoverCloseTimeout.Text, out var cc)) item.Cover_Close_Timeout_Second = cc;
 if (int.TryParse(TxtSVCheckOffset.Text, out var sco)) item.SV_CheckOffet = sco;
 if (int.TryParse(TxtActTimeLimit.Text, out var atl)) item.ActTime_Limit_Second = atl;

 // assign back and mark dirty
 _unitSettings.DryingTanks = list;
 _isDirty = true;
 }
 catch
 {
 // ignore
 }
 }

 // Helper: save sink UI controls into _unitSettings.Sink
 private void SaveSinkUi()
 {
 try
 {
 if (_unitSettings == null) _unitSettings = new UnitSettings();
 if (_unitSettings.Sink == null) _unitSettings.Sink = new US_Sink();
 var s = _unitSettings.Sink;

 if (float.TryParse(TxtSinkUnitTransfer.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var ut)) s.UnitTransfer = ut;
 if (int.TryParse(TxtSinkSVLowLimit.Text, out var svl)) s.SV_Low_Limit = svl;
 if (int.TryParse(TxtSinkSVHighLimit.Text, out var svh)) s.SV_High_Limit = svh;
 if (int.TryParse(TxtSinkPVLowTimeout.Text, out var pvl)) s.PV_Low_Timeout_Second = pvl;
 if (int.TryParse(TxtSinkPVHighTimeout.Text, out var pvh)) s.PV_High_Timeout_Second = pvh;
 if (int.TryParse(TxtSinkCoverOpenTimeout.Text, out var co)) s.Cover_Open_Timeout_Second = co;
 if (int.TryParse(TxtSinkCoverCloseTimeout.Text, out var cc)) s.Cover_Close_Timeout_Second = cc;
 if (int.TryParse(TxtSinkSVCheckOffset.Text, out var sco)) s.SV_CheckOffet = sco;
 if (int.TryParse(TxtSinkActTimeLimit.Text, out var atl)) s.ActTime_Limit_Second = atl;
 if (float.TryParse(TxtSinkMotorUnitTransfer.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var mut)) s.MotorUnitTransfer = mut;

 _unitSettings.Sink = s;
 _isDirty = true;
 }
 catch
 {
 // ignore
 }
 }

 // Helper: load sink values into UI
 private void LoadSinkToUi()
 {
 try
 {
 if (_unitSettings?.Sink != null)
 {
 var s = _unitSettings.Sink;
 TxtSinkUnitTransfer.Text = s.UnitTransfer.ToString(CultureInfo.InvariantCulture);
 TxtSinkSVLowLimit.Text = s.SV_Low_Limit.ToString();
 TxtSinkSVHighLimit.Text = s.SV_High_Limit.ToString();
 TxtSinkPVLowTimeout.Text = s.PV_Low_Timeout_Second.ToString();
 TxtSinkPVHighTimeout.Text = s.PV_High_Timeout_Second.ToString();
 TxtSinkCoverOpenTimeout.Text = s.Cover_Open_Timeout_Second.ToString();
 TxtSinkCoverCloseTimeout.Text = s.Cover_Close_Timeout_Second.ToString();
 TxtSinkSVCheckOffset.Text = s.SV_CheckOffet.ToString();
 TxtSinkActTimeLimit.Text = s.ActTime_Limit_Second.ToString();
 TxtSinkMotorUnitTransfer.Text = s.MotorUnitTransfer.ToString(CultureInfo.InvariantCulture);
 }
 else
 {
 // clear fields
 TxtSinkUnitTransfer.Text = string.Empty;
 TxtSinkSVLowLimit.Text = string.Empty;
 TxtSinkSVHighLimit.Text = string.Empty;
 TxtSinkPVLowTimeout.Text = string.Empty;
 TxtSinkPVHighTimeout.Text = string.Empty;
 TxtSinkCoverOpenTimeout.Text = string.Empty;
 TxtSinkCoverCloseTimeout.Text = string.Empty;
 TxtSinkSVCheckOffset.Text = string.Empty;
 TxtSinkActTimeLimit.Text = string.Empty;
 TxtSinkMotorUnitTransfer.Text = string.Empty;
 }
 }
 catch
 {
 // ignore
 }
 }

 // Helper: save heating tank UI controls into _unitSettings.HeatingTank
 private void SaveHeatingUi()
 {
 try
 {
 if (_unitSettings == null) _unitSettings = new UnitSettings();
 if (_unitSettings.HeatingTank == null) _unitSettings.HeatingTank = new US_HeatingTank();
 var h = _unitSettings.HeatingTank;

 if (float.TryParse(TxtHeatingUnitTransfer.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var ut)) h.UnitTransfer = ut;
 if (int.TryParse(TxtHeatingSVLowLimit.Text, out var svl)) h.SV_Low_Limit = svl;
 if (int.TryParse(TxtHeatingSVHighLimit.Text, out var svh)) h.SV_High_Limit = svh;
 if (int.TryParse(TxtHeatingPVLowTimeout.Text, out var pvl)) h.PV_Low_Timeout_Second = pvl;
 if (int.TryParse(TxtHeatingPVHighTimeout.Text, out var pvh)) h.PV_High_Timeout_Second = pvh;
 if (int.TryParse(TxtHeatingINVHighTimeout.Text, out var invh)) h.INV_High_Timeout_Second = invh;
 if (int.TryParse(TxtHeatingINVLowTimeout.Text, out var invl)) h.INV_Low_Timeout_Second = invl;
 if (int.TryParse(TxtHeatingINVZeroTimeout.Text, out var invz)) h.INV_Zero_Timeout_Second = invz;
 if (int.TryParse(TxtHeatingSVCheckOffset.Text, out var sco)) h.SV_CheckOffet = sco;
 if (float.TryParse(TxtHeatingINVCheckOffset.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var invco)) h.INV_CheckOffset = invco;

 _unitSettings.HeatingTank = h;
 _isDirty = true;
 }
 catch
 {
 // ignore
 }
 }

 // Helper: load heating tank values into UI
 private void LoadHeatingToUi()
 {
 try
 {
 if (_unitSettings?.HeatingTank != null)
 {
 var h = _unitSettings.HeatingTank;
 TxtHeatingUnitTransfer.Text = h.UnitTransfer.ToString(CultureInfo.InvariantCulture);
 TxtHeatingSVLowLimit.Text = h.SV_Low_Limit.ToString();
 TxtHeatingSVHighLimit.Text = h.SV_High_Limit.ToString();
 TxtHeatingPVLowTimeout.Text = h.PV_Low_Timeout_Second.ToString();
 TxtHeatingPVHighTimeout.Text = h.PV_High_Timeout_Second.ToString();
 TxtHeatingINVHighTimeout.Text = h.INV_High_Timeout_Second.ToString();
 TxtHeatingINVLowTimeout.Text = h.INV_Low_Timeout_Second.ToString();
 TxtHeatingINVZeroTimeout.Text = h.INV_Zero_Timeout_Second.ToString();
 TxtHeatingSVCheckOffset.Text = h.SV_CheckOffet.ToString();
 TxtHeatingINVCheckOffset.Text = h.INV_CheckOffset.ToString(CultureInfo.InvariantCulture);
 }
 else
 {
 TxtHeatingUnitTransfer.Text = string.Empty;
 TxtHeatingSVLowLimit.Text = string.Empty;
 TxtHeatingSVHighLimit.Text = string.Empty;
 TxtHeatingPVLowTimeout.Text = string.Empty;
 TxtHeatingPVHighTimeout.Text = string.Empty;
 TxtHeatingINVHighTimeout.Text = string.Empty;
 TxtHeatingINVLowTimeout.Text = string.Empty;
 TxtHeatingINVZeroTimeout.Text = string.Empty;
 TxtHeatingSVCheckOffset.Text = string.Empty;
 TxtHeatingINVCheckOffset.Text = string.Empty;
 }
 }
 catch
 {
 // ignore
 }
 }

 // Helper: save soaking tank UI controls into _unitSettings.SoakingTank
 private void SaveSoakingUi()
 {
 try
 {
 if (_unitSettings == null) _unitSettings = new UnitSettings();
 if (_unitSettings.SoakingTank == null) _unitSettings.SoakingTank = new US_SoakingTank();
 var s = _unitSettings.SoakingTank;

 if (float.TryParse(TxtSoakMotorUnitTransfer.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var mut)) s.MotorUnitTransfer = mut;
 if (int.TryParse(TxtSoakCoverOpenTimeout.Text, out var co)) s.Cover_Open_Timeout_Second = co;
 if (int.TryParse(TxtSoakCoverCloseTimeout.Text, out var cc)) s.Cover_Close_Timeout_Second = cc;
 if (int.TryParse(TxtSoakActTimeLimit.Text, out var atl)) s.ActTime_Limit_Second = atl;
 if (float.TryParse(TxtSoakUltrasonicCurrentMaxLimit.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var ucm)) s.UltrasonicCurrentMaxLimit = ucm;

 _unitSettings.SoakingTank = s;
 _isDirty = true;
 }
 catch
 {
 // ignore
 }
 }

 // Helper: load soaking tank values into UI
 private void LoadSoakingToUi()
 {
 try
 {
 if (_unitSettings?.SoakingTank != null)
 {
 var s = _unitSettings.SoakingTank;
 TxtSoakMotorUnitTransfer.Text = s.MotorUnitTransfer.ToString(CultureInfo.InvariantCulture);
 TxtSoakCoverOpenTimeout.Text = s.Cover_Open_Timeout_Second.ToString();
 TxtSoakCoverCloseTimeout.Text = s.Cover_Close_Timeout_Second.ToString();
 TxtSoakActTimeLimit.Text = s.ActTime_Limit_Second.ToString();
 TxtSoakUltrasonicCurrentMaxLimit.Text = s.UltrasonicCurrentMaxLimit.ToString(CultureInfo.InvariantCulture);
 }
 else
 {
 TxtSoakMotorUnitTransfer.Text = string.Empty;
 TxtSoakCoverOpenTimeout.Text = string.Empty;
 TxtSoakCoverCloseTimeout.Text = string.Empty;
 TxtSoakActTimeLimit.Text = string.Empty;
 TxtSoakUltrasonicCurrentMaxLimit.Text = string.Empty;
 }
 }
 catch
 {
 // ignore
 }
 }

 // Helper: save shuttle UI controls into _unitSettings.Shuttle
 private void SaveShuttleUi()
 {
 try
 {
 if (_unitSettings == null) _unitSettings = new UnitSettings();
 if (_unitSettings.Shuttle == null) _unitSettings.Shuttle = new US_Shuttle();
 var sh = _unitSettings.Shuttle;

 if (int.TryParse(TxtShuttleClamperTimeout.Text, out var ct)) sh.Clamper_Timeout_Second = ct;

 _unitSettings.Shuttle = sh;
 _isDirty = true;
 }
 catch
 {
 // ignore
 }
 }

 // Helper: load shuttle UI
 private void LoadShuttleToUi()
 {
 try
 {
 if (_unitSettings?.Shuttle != null)
 {
 var sh = _unitSettings.Shuttle;
 TxtShuttleClamperTimeout.Text = sh.Clamper_Timeout_Second.ToString();
 }
 else
 {
 TxtShuttleClamperTimeout.Text = string.Empty;
 }
 }
 catch
 {
 // ignore
 }
 }

 // Helper: save motor X/Z UI into _unitSettings.Motors
 private void SaveMotorsUi()
 {
 try
 {
 if (_unitSettings == null) _unitSettings = new UnitSettings();
 var list = _unitSettings.Motors ?? new System.Collections.Generic.List<US_Motor>();
 // ensure at least2 motors
 while (list.Count <2) list.Add(new US_Motor());

 if (float.TryParse(TxtMotorXUnitTransfer.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var xut)) list[0].UnitTransfer = xut;
 if (float.TryParse(TxtMotorZUnitTransfer.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var zut)) list[1].UnitTransfer = zut;

 _unitSettings.Motors = list;
 _isDirty = true;
 }
 catch
 {
 // ignore
 }
 }

 // Helper: load motors into UI (X = index0, Z = index1)
 private void LoadMotorsToUi()
 {
 try
 {
 if (_unitSettings?.Motors != null && _unitSettings.Motors.Count >=1)
 {
 TxtMotorXUnitTransfer.Text = _unitSettings.Motors[0].UnitTransfer.ToString(CultureInfo.InvariantCulture);
 }
 else TxtMotorXUnitTransfer.Text = string.Empty;

 if (_unitSettings?.Motors != null && _unitSettings.Motors.Count >=2)
 {
 TxtMotorZUnitTransfer.Text = _unitSettings.Motors[1].UnitTransfer.ToString(CultureInfo.InvariantCulture);
 }
 else TxtMotorZUnitTransfer.Text = string.Empty;
 }
 catch
 {
 // ignore
 }
 }
 }
}
