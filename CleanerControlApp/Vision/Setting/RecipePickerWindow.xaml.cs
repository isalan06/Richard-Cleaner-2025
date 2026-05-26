using CleanerControlApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CleanerControlApp.Vision.SettingViews
{
 public partial class RecipePickerWindow : Window
 {
 private List<string> _all = new List<string>();
 private bool _sortAsc = true;

 public string? SelectedRecipe { get; private set; }

 public RecipePickerWindow()
 {
 InitializeComponent();

 try { ConfigLoader.Load(); } catch { }
 LoadRecipes();

 TxtFilter.TextChanged += (s, e) => ApplyFilter();
 }

 private void LoadRecipes()
 {
 try
 {
 _all = ConfigLoader.ListRecipeNames() ?? new List<string>();
 }
 catch { _all = new List<string>(); }
 ApplyFilter();
 }

 private void ApplyFilter()
 {
 var filter = TxtFilter.Text ?? string.Empty;
 IEnumerable<string> items = _all;
 if (!string.IsNullOrWhiteSpace(filter)) items = items.Where(x => x != null && x.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >=0);
 items = _sortAsc ? items.OrderBy(x => x) : items.OrderByDescending(x => x);
 LstRecipes.ItemsSource = items.ToList();
 }

 private void BtnSort_Click(object sender, RoutedEventArgs e)
 {
 _sortAsc = !_sortAsc;
 BtnSort.Content = _sortAsc ? "名稱↑" : "名稱↓";
 ApplyFilter();
 }

 private void LstRecipes_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
 {
 if (LstRecipes.SelectedItem is string name)
 {
 // Apply the recipe to ModuleSettings and persist (same behavior as SetSystemView)
 try
 {
 if (ApplyAndSaveRecipe(name))
 {
 SelectedRecipe = name;
 DialogResult = true;
 Close();
 }
 }
 catch { }
 }
 }

 private void BtnOK_Click(object sender, RoutedEventArgs e)
 {
 if (LstRecipes.SelectedItem is string name)
 {
 try
 {
 if (ApplyAndSaveRecipe(name))
 {
 SelectedRecipe = name;
 DialogResult = true;
 Close();
 }
 }
 catch { }
 }
 else
 {
 MessageBox.Show("請先選擇一個配方", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
 }
 }

 private bool ApplyAndSaveRecipe(string name)
 {
 try
 {
 ModuleSettings? moduleSettings = null;
 // Prefer DI instance if available
 try
 {
 if (App.AppHost != null)
 {
 moduleSettings = App.AppHost.Services.GetService(typeof(ModuleSettings)) as ModuleSettings;
 }
 }
 catch { }

 if (moduleSettings == null)
 {
 try { moduleSettings = ConfigLoader.GetModuleSettings(); } catch { }
 }

 if (moduleSettings == null)
 {
 MessageBox.Show("無法取得模組設定以載入配方", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
 return false;
 }

 var ok = ConfigLoader.LoadRecipeToModuleSettings(moduleSettings, name);
 if (ok)
 {
 // Persist the selection and recipe file
 ConfigLoader.SetModuleSettingsAndSave(moduleSettings);

 // Do not show extra MessageBox here; caller (UI) will update via ModuleSettingsUpdated event
 return true;
 }
 else
 {
 MessageBox.Show($"載入配方失敗: {name}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
 return false;
 }
 }
 catch (Exception ex)
 {
 MessageBox.Show($"載入配方失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
 return false;
 }
 }

 private void BtnCancel_Click(object sender, RoutedEventArgs e)
 {
 DialogResult = false;
 Close();
 }
 }
}