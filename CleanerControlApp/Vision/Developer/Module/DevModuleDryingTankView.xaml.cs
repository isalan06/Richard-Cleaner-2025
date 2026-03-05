using System.Windows.Controls;
using System;
using System.Diagnostics;
using System.Windows;
using CleanerControlApp.Hardwares.DryingTank.Interfacaes;
using Microsoft.Extensions.DependencyInjection;

namespace CleanerControlApp.Vision.Developer.Module
{
    public partial class DevModuleDryingTankView : UserControl
    {
        public DevModuleDryingTankView()
        {
            InitializeComponent();
        }

        private void InitializeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >0)
                {
                    dryingTanks[0].ModuleReset();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InitializeButton_Click exception: {ex}");
            }
        }

        private void AutoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >0)
                {
                    dryingTanks[0].AutoStart();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AutoButton_Click exception: {ex}");
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >0)
                {
                    dryingTanks[0].AutoStop();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StopButton_Click exception: {ex}");
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >0)
                {
                    dryingTanks[0].AutoPause();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PauseButton_Click exception: {ex}");
            }
        }

        private void AlarmStopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >0)
                {
                    dryingTanks[0].AlarmStop();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AlarmStopButton_Click exception: {ex}");
            }
        }

        // #2 handlers
        private void Initialize2Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >1)
                {
                    dryingTanks[1].ModuleReset();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Initialize2Button_Click exception: {ex}");
            }
        }

        private void Auto2Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >1)
                {
                    dryingTanks[1].AutoStart();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Auto2Button_Click exception: {ex}");
            }
        }

        private void Stop2Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >1)
                {
                    dryingTanks[1].AutoStop();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Stop2Button_Click exception: {ex}");
            }
        }

        private void Pause2Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >1)
                {
                    dryingTanks[1].AutoPause();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Pause2Button_Click exception: {ex}");
            }
        }

        private void AlarmStop2Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >1)
                {
                    dryingTanks[1].AlarmStop();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AlarmStop2Button_Click exception: {ex}");
            }
        }

        // Tank1 additional controls
        private void OpenDryingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >0)
                {
                    dryingTanks[0].SimHiTemperature(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenDryingButton_Click exception: {ex}");
            }
        }

        private void CloseDryingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >0)
                {
                    dryingTanks[0].SimHiTemperature(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CloseDryingButton_Click exception: {ex}");
            }
        }

        private void PickButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >0)
                {
                    dryingTanks[0].HS_ClamperPickFinished = !dryingTanks[0].HS_ClamperPickFinished;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PickButton_Click exception: {ex}");
            }
        }

        private void PlaceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >0)
                {
                    dryingTanks[0].HS_ClamperPlaceFinished = !dryingTanks[0].HS_ClamperPlaceFinished;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PlaceButton_Click exception: {ex}");
            }
        }

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >0)
                {
                    dryingTanks[0].HS_ClamperMoving = !dryingTanks[0].HS_ClamperMoving;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MoveButton_Click exception: {ex}");
            }
        }

        // Tank2 additional controls
        private void OpenDrying2Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >1)
                {
                    dryingTanks[1].SimHiTemperature(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenDrying2Button_Click exception: {ex}");
            }
        }

        private void CloseDrying2Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >1)
                {
                    dryingTanks[1].SimHiTemperature(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CloseDrying2Button_Click exception: {ex}");
            }
        }

        private void Pick2Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >1)
                {
                    dryingTanks[1].HS_ClamperPickFinished = !dryingTanks[1].HS_ClamperPickFinished;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Pick2Button_Click exception: {ex}");
            }
        }

        private void Place2Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >1)
                {
                    dryingTanks[1].HS_ClamperPlaceFinished = !dryingTanks[1].HS_ClamperPlaceFinished;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Place2Button_Click exception: {ex}");
            }
        }

        private void Move2Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dryingTanks = App.AppHost?.Services.GetService<IDryingTank[]>();
                if (dryingTanks != null && dryingTanks.Length >1)
                {
                    dryingTanks[1].HS_ClamperMoving = !dryingTanks[1].HS_ClamperMoving;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Move2Button_Click exception: {ex}");
            }
        }
    }
}