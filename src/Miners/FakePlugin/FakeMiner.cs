﻿using MinerPlugin;
using MinerPluginToolkitV1;
using NHM.Common.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FakePlugin
{
    /// <summary>
    /// ExampleMiner class inherits IMiner to implement basic actions for miner
    /// </summary>
    public class FakeMiner : MinerBase
    {
        #region members for simulation purposes
        string _devices;

        Random _rand { get; } = new Random();

        //private enum MinerState
        //{
        //    STOPPED,
        //    MINING
        //}

        //MinerState _state = MinerState.STOPPED;

        #endregion members for simulation purposes

        public FakeMiner(string uuid):base(uuid){}

        public override async Task<ApiData> GetMinerStatsDataAsync()
        {
            // simulate API delay
            await Task.Delay(150);

            var api = new ApiData();
            var perDeviceSpeedInfo = new Dictionary<string, IReadOnlyList<AlgorithmTypeSpeedPair>>();
            var perDevicePowerInfo = new Dictionary<string, int>();
            var totalSpeed = 0d;
            var totalPowerUsage = 0;

            foreach (var mp in _miningPairs)
            {
                var speedVariation = _rand.Next(-100, 100);
                var speed = 1000 + speedVariation;
                var powerVariation = _rand.Next(-10, 10);
                var power = 100 + powerVariation;
                totalSpeed += speed;
                totalPowerUsage += power;

                var deviceUUID = mp.Device.UUID;
                perDeviceSpeedInfo.Add(deviceUUID, new List<AlgorithmTypeSpeedPair>() { new AlgorithmTypeSpeedPair(_algorithmType, speed) });
                perDevicePowerInfo.Add(deviceUUID, 108);
            }

            api.AlgorithmSpeedsPerDevice = perDeviceSpeedInfo;
            api.AlgorithmSpeedsTotal = new List<AlgorithmTypeSpeedPair> { new AlgorithmTypeSpeedPair(_algorithmType, totalSpeed) };
            api.PowerUsagePerDevice = perDevicePowerInfo;
            api.PowerUsageTotal = totalPowerUsage;

            return api;
        }

        public override async Task<BenchmarkResult> StartBenchmark(CancellationToken stop, BenchmarkPerformanceType benchmarkType = BenchmarkPerformanceType.Standard)
        {
            await Task.Delay(15000, stop);
            var success = !stop.IsCancellationRequested;
            double speed = success ? 1000 : 0; // everything is same speed

            // and return our result
            return new BenchmarkResult
            {
                AlgorithmTypeSpeeds = new List<AlgorithmTypeSpeedPair> { new AlgorithmTypeSpeedPair(_algorithmType, speed) },
                Success = success,
                ErrorMessage = ""
            };
        }

        protected override void Init()
        {
            _devices = string.Join(",", _miningPairs.Select(p => $"{p.Device.Name}({p.Device.ID})"));
        }

        protected override string MiningCreateCommandLine()
        {
            return $"-devices={_devices} -algorithm={_algorithmType.ToString()} -miningLocaiton={_miningLocation} -username={_username} -UUID {_uuid}";
        }
    }
}
