using CleanerControlApp.Utilities;
using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace CleanerControlApp.Vision.SettingViews
{
    public partial class SetSystemView : UserControl
    {
        private ModuleSettings _moduleSettings;
        private List<string> _allRecipes = new List<string>();
        private bool _sortAscending = true;

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

            // initialize recipe list
            LoadRecipeList();

            // attach key handler for delete
            try
            {
                if (this.FindName("Lst_RecipeNames") is ListBox lb)
                {
                    lb.KeyDown += Lst_RecipeNames_KeyDown;
                }
            }
            catch { }
        }

        private void Lst_RecipeNames_KeyDown(object? sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Delete)
                {
                    if (Lst_RecipeNames.SelectedItem is string name)
                    {
                        // protect Default
                        if (string.Equals(name, "Default", StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show("無法刪除 'Default' 配方。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            e.Handled = true;
                            return;
                        }

                        var current = _moduleSettings?.RecipeName ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(current) && string.Equals(name, current, StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show("無法刪除目前正在使用的配方。請先切換到其他配方後再刪除。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            e.Handled = true;
                            return;
                        }

                        var res = MessageBox.Show($"確定要刪除配方 '{name}' 嗎？", "確認刪除", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (res == MessageBoxResult.Yes)
                        {
                            try
                            {
                                var ok = ConfigLoader.DeleteRecipe(name);
                                if (ok)
                                {
                                    MessageBox.Show($"已刪除配方: {name}", "訊息", MessageBoxButton.OK, MessageBoxImage.Information);
                                    LoadRecipeList();
                                }
                                else
                                {
                                    MessageBox.Show($"刪除配方失敗: {name}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"刪除配方失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }

                    e.Handled = true;
                }
            }
            catch { }
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

            // Update current recipe name display
            try
            {
                if (this.FindName("Txt_CurrentRecipeName") is TextBlock tb)
                {
                    tb.Text = string.IsNullOrWhiteSpace(_moduleSettings.RecipeName) ? "(未設定)" : _moduleSettings.RecipeName;
                }
            }
            catch { }
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
            MessageBox.Show("讀取完成", "訊息", MessageBoxButton.OK, MessageBoxImage.Information);

            // reload recipes in case external changes occurred
            LoadRecipeList();
        }

        private void BtnWrite_Click(object sender, RoutedEventArgs e)
        {
            if (_moduleSettings == null)
            {
                if ( App.AppHost != null)
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

            int sink =0, soak =0, d1 =0, d2 =0, write =0;

            bool pSink = tbSink != null && int.TryParse(tbSink.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out sink);
            bool pSoak = tbSoak != null && int.TryParse(tbSoak.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out soak);
            bool pD1 = tbDry1 != null && int.TryParse(tbDry1.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out d1);
            bool pD2 = tbDry2 != null && int.TryParse(tbDry2.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out d2);
            bool pWrite = tbWrite != null && int.TryParse(tbWrite.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out write);

            if (!pSink) errors.AppendLine("Sink 值格式錯誤");
            if (!pSoak) errors.AppendLine("浸泡槽 值格式錯誤");
            if (!pD1) errors.AppendLine("烘乾1 值格式錯誤");
            if (!pD2) errors.AppendLine("烘乾2 值格式錯誤");
            if (!pWrite) errors.AppendLine("初始寫入馬達參數 值格式錯誤");

            if (errors.Length >0)
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
                // Persist ModuleSettings (and linked recipe file if RecipeName set)
                ConfigLoader.SetModuleSettingsAndSave(_moduleSettings);

                // Do NOT update DI ModuleSettings instance here — other views should refresh via their own Read action

                // Update recipe name display in case it changed
                try
                {
                    if (this.FindName("Txt_CurrentRecipeName") is TextBlock tb)
                    {
                        tb.Text = string.IsNullOrWhiteSpace(_moduleSettings.RecipeName) ? "(未設定)" : _moduleSettings.RecipeName;
                    }
                }
                catch { }

                MessageBox.Show("寫入完成", "訊息", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"寫入失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddRecipe_Click(object sender, RoutedEventArgs e)
        {
            if (_moduleSettings == null)
            {
                try { ConfigLoader.Load(); _moduleSettings = ConfigLoader.GetModuleSettings(); } catch { }
            }

            if (_moduleSettings == null)
            {
                MessageBox.Show("無法取得模組設定以建立配方", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Show input dialog to get recipe name
            var defaultName = "Recipe_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var dlg = new InputDialog(defaultName) { Owner = Window.GetWindow(this) };
            var ok = dlg.ShowDialog();
            if (ok != true)
                return;

            var name = dlg.Result;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("配方名稱不可為空", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check for existing file
            var exists = false;
            try
            {
                exists = System.IO.File.Exists(System.IO.Path.Combine(AppContext.BaseDirectory, "Recipes", name + ".json"));
            }
            catch { }

            if (exists)
            {
                var res = MessageBox.Show($"配方 '{name}' 已存在，是否覆蓋?", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;
            }

            try
            {
                // Prefer the DI-registered ModuleSettings instance (if available) so recipe captures current values from other views
                ModuleSettings toSave = _moduleSettings;
                try
                {
                    if (App.AppHost != null)
                    {
                        var diModule = App.AppHost.Services.GetService(typeof(ModuleSettings)) as ModuleSettings;
                        if (diModule != null)
                        {
                            toSave = diModule; // use DI instance which may have other module parameters updated by other views
                            _moduleSettings = diModule;
                        }
                    }
                }
                catch { }

                // Save recipe file and update ModuleSettings.RecipeName and persist
                ConfigLoader.SaveRecipeForModuleSettings(toSave, name);
                ConfigLoader.SetModuleSettingsAndSave(toSave);

                // Update display
                if (this.FindName("Txt_CurrentRecipeName") is TextBlock tb)
                {
                    tb.Text = _moduleSettings.RecipeName;
                }

                MessageBox.Show($"已建立配方: {name}", "訊息", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh list so new recipe appears
                LoadRecipeList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"建立配方失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Recipe list handling ---
        private void LoadRecipeList()
        {
            try
            {
                _allRecipes = ConfigLoader.ListRecipeNames() ?? new List<string>();
            }
            catch
            {
                _allRecipes = new List<string>();
            }

            ApplyFilterAndSort();
        }

        private void ApplyFilterAndSort()
        {
            try
            {
                var filter = string.Empty;
                try { if (this.FindName("Txt_RecipeFilter") is TextBox tb) filter = tb.Text ?? string.Empty; } catch { }

                IEnumerable<string> items = _allRecipes;
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    var lower = filter.ToLowerInvariant();
                    items = items.Where(x => x != null && x.ToLowerInvariant().Contains(lower));
                }

                items = _sortAscending ? items.OrderBy(x => x) : items.OrderByDescending(x => x);

                if (this.FindName("Lst_RecipeNames") is ListBox lb)
                {
                    lb.ItemsSource = items.Take(1000).ToList(); // defensive
                }
            }
            catch { }
        }

        private void Txt_RecipeFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilterAndSort();
        }

        private void BtnSortRecipes_Click(object sender, RoutedEventArgs e)
        {
            _sortAscending = !_sortAscending;
            try
            {
                if (BtnSortRecipes != null)
                {
                    BtnSortRecipes.Content = _sortAscending ? "排序：名稱↑" : "排序：名稱↓";
                }
            }
            catch { }
            ApplyFilterAndSort();
        }

        private void Lst_RecipeNames_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (Lst_RecipeNames.SelectedItem is string name)
                {
                    if (_moduleSettings == null) _moduleSettings = ConfigLoader.GetModuleSettings();
                    if (_moduleSettings != null)
                    {
                        var ok = ConfigLoader.LoadRecipeToModuleSettings(_moduleSettings, name);
                        if (ok)
                        {
                            // Persist the selection and recipe file
                            ConfigLoader.SetModuleSettingsAndSave(_moduleSettings);

                            // Do NOT update DI ModuleSettings instance here — other views should refresh via their own Read action

                            // Refresh UI to reflect loaded recipe parameters (only for this view)
                            try { LoadToUI(); } catch { }

                            if (this.FindName("Txt_CurrentRecipeName") is TextBlock tb)
                                tb.Text = _moduleSettings.RecipeName;

                            MessageBox.Show($"已載入配方: {name}", "訊息", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show($"載入配方失敗: {name}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch { }
        }

        private void Lst_RecipeNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Intentionally do NOT change the "現在使用配方" display when user selects an item.
            // Selection is treated as a preview only; user must double-click (confirm) to apply.
        }
    }
}
