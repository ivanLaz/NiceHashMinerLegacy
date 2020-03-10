﻿using NHM.Common.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MinerPlugin
{

    // TODO when we update to C#7 use tuple values or System.ValueTuple for .NET version that don't support C#7

    /// <summary>
    /// IMiner is the mandatory interface for all miners containing bare minimum functionalities
	/// It is used as miner process instance created by IMinerPlugin
    /// </summary>
    public interface IMiner
    {
        /// <summary>
        /// Sets mining pairs (<see cref="MiningPair"/>)
        /// </summary>
        void InitMiningPairs(IEnumerable<MiningPair> miningPairs);

        /// <summary>
        /// Sets Mining location and username; password is optional
        /// </summary>
        void InitMiningLocationAndUsername(string miningLocation, string username, string password = "x");


        /// <summary>
        /// Obsolete use StartMiningTask (<see cref="IMinerAsyncExtensions"/>)
        /// </summary>
        [Obsolete("Obsolete use IMinerAsyncExtensions.StartMiningTask", true)]
        void StartMining();
        /// <summary>
        /// Obsolete use StopMiningTask (<see cref="IMinerAsyncExtensions"/>)
        /// </summary>
        [Obsolete("Obsolete use IMinerAsyncExtensions.StopMiningTask", true)]
        void StopMining();

        /// <summary>
        /// Returns Benchmark result (<see cref="BenchmarkResult"/>)
        /// </summary>
        Task<BenchmarkResult> StartBenchmark(CancellationToken stop, BenchmarkPerformanceType benchmarkType = BenchmarkPerformanceType.Standard); // IBenchmarker

        /// <summary>
        /// Returns a task that retrives mining 
        /// </summary>
        Task<ApiData> GetMinerStatsDataAsync();
    }
}
