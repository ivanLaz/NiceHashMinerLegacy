﻿using NHM.Common;
using NHMCore.Mining;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHM.DeviceMonitoring.TDP;

namespace NiceHashMiner.ViewModels.Models
{
    public class DeviceDataTDP : NotifyChangedBase
    {
        public ComputeDevice Dev { get; }

        private ITDP _tdpMon;

        public bool HasTPDSettings { get; }


        public string SelectedTPDSettingType { get; private set; } = "N/A";
        public string LastTPDSettingsSuccess { get; set; } = "N/A";
        public string TPDSimpleValue { get; private set; } = "N/A";
        public string TPDPercentageValue { get; private set; } = "N/A";
        public string TPDRawValue { get; private set; } = "N/A";


        public void SetSimple(TDPSimpleType type)
        {
            TDPSet(_tdpMon.SetTDPSimple(type));
        }
        public void SetPercentage(double value)
        {
            var perc = value / 100.0;
            TDPSet(_tdpMon.SetTDPPercentage(perc));
        }
        public void SetRaw(double value)
        {
            TDPSet(_tdpMon.SetTDPRaw(value));
        }

        public DeviceDataTDP(ComputeDevice dev)
        {
            Dev = dev;
            _tdpMon = dev.DeviceMonitor as ITDP;
            HasTPDSettings = _tdpMon != null;
            if (HasTPDSettings)
            {
                UpdateValues();
            }
        }

        private void TDPSet(bool success)
        {
            LastTPDSettingsSuccess = success.ToString();
            OnPropertyChanged(nameof(LastTPDSettingsSuccess));
            if (success) UpdateValues();
        }

        private void UpdateValues()
        {
            TPDSimpleValue = _tdpMon.TDPSimple.ToString();
            TPDPercentageValue = $"{_tdpMon.TDPPercentage * 100}%";
            TPDRawValue = _tdpMon.TDPRaw.ToString();
            SelectedTPDSettingType = _tdpMon.SettingType.ToString();
            OnPropertyChanged(nameof(TPDSimpleValue));
            OnPropertyChanged(nameof(TPDPercentageValue));
            OnPropertyChanged(nameof(TPDRawValue));
            OnPropertyChanged(nameof(SelectedTPDSettingType));
        }

    }
}
