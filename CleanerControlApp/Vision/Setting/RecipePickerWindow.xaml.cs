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
 SelectedRecipe = name;
 DialogResult = true;
 Close();
 }
 }

 private void BtnOK_Click(object sender, RoutedEventArgs e)
 {
 if (LstRecipes.SelectedItem is string name)
 {
 SelectedRecipe = name;
 DialogResult = true;
 Close();
 }
 else
 {
 MessageBox.Show("請先選擇一個配方", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
 }
 }

 private void BtnCancel_Click(object sender, RoutedEventArgs e)
 {
 DialogResult = false;
 Close();
 }
 }
}