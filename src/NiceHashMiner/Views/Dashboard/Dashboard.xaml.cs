﻿using NiceHashMiner.ViewModels;
using NiceHashMiner.Views.Common;
using NHMCore;
using NHMCore.ApplicationState;
using NHMCore.Configs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NiceHashMiner.Views.Dashboard
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : UserControl, IThemeSetter
    {
        private MainVM _vm;

        public Dashboard()
        {
            InitializeComponent();

            DataContextChanged += Dashboard_DataContextChanged;
            ThemeSetterManager.AddThemeSetter(this);
        }

        private void Dashboard_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is MainVM mainVM)
            {
                _vm = mainVM;
                return;
            }
            throw new Exception("Dashboard_DataContextChanged e.NewValue must be of type MainVM");
        }

        void IThemeSetter.SetTheme(bool isLight)
        {
            var style = isLight ? Application.Current.FindResource("StartStopButtonLight") : Application.Current.FindResource("StartStopButtonDark");
            StartStopToggleButton.Style = style as Style;
        }

        private async void ToggleButtonStartStop_Click(object sender, RoutedEventArgs e)
        {
            await ToggleButtonStartStop_ClickTask();
        }

        private async Task ToggleButtonStartStop_ClickTask()
        {
            // IF ANY MINING execute STOP
            if (MiningState.Instance.AnyDeviceRunning)
            {
                await _vm.StopMining();
            }
            else
            {
                await _vm.StartMining();
            }
        }

        
        private static readonly EnterWalletDialog _enterBTCAddress = new EnterWalletDialog();
        private void EnterBTCWallet_Button_Click(object sender, RoutedEventArgs e)
        {
            CustomDialogManager.ShowDialog(_enterBTCAddress);
        }
    }
}
