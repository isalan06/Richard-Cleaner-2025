using System;
using System.Windows;
using System.Windows.Controls;
using CleanerControlApp.Hardwares.SoakingTank.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CleanerControlApp.Vision.Template
{
 /// <summary>
 /// Template_SoakingTank.xaml 的互動邏輯
 /// </summary>
 public partial class Template_SoakingTank : UserControl
 {
 private readonly ISoakingTank? _soakingTank;

 public Template_SoakingTank()
 {
 InitializeComponent();

 try
 {
 _soakingTank = App.AppHost?.Services.GetService<ISoakingTank>();
 }
 catch
 {
 _soakingTank = null;
 }
 }

 private void OpenCover_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualCoverClose(false); } catch { }
 }

 private void CloseCover_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualCoverClose(true); } catch { }
 }

 private void OpenAir_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualAirOP(true); } catch { }
 }

 private void CloseAir_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualAirOP(false); } catch { }
 }

 private void OpenWaterIn_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualWaterInOP(true); } catch { }
 }

 private void CloseWaterIn_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualWaterInOP(false); } catch { }
 }

 private void OpenUltrasonic_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualUltrasonicOP(true); } catch { }
 }

 private void CloseUltrasonic_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualUltrasonicOP(false); } catch { }
 }

 private void OpenWaterOut_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualWaterOutputOP(true); } catch { }
 }

 private void CloseWaterOut_Click(object sender, RoutedEventArgs e)
 {
 try { _soakingTank?.ManualWaterOutputOP(false); } catch { }
 }
 }
}
