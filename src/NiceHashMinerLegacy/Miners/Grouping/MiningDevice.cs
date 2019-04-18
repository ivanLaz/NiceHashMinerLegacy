﻿using NiceHashMiner.Devices;
using System;
using System.Collections.Generic;
using NiceHashMiner.Algorithms;
using NiceHashMiner.Switching;
using NiceHashMinerLegacy.Common.Enums;

namespace NiceHashMiner.Miners.Grouping
{
    public class MiningDevice
    {
        // switch testing quick and dirty, runtime versions 
#if (SWITCH_TESTING)
        static List<AlgorithmType> testingAlgos = new List<AlgorithmType>() {
            //AlgorithmType.X13,
            //AlgorithmType.Keccak,
            //AlgorithmType.X15,
            //AlgorithmType.Nist5,
            //AlgorithmType.NeoScrypt,
            AlgorithmType.Lyra2RE,
            //AlgorithmType.WhirlpoolX,
            //AlgorithmType.Qubit,
            //AlgorithmType.Quark,
            //AlgorithmType.Lyra2REv2,
            //AlgorithmType.Blake256r8,
            //AlgorithmType.Blake256r14,
            //AlgorithmType.Blake256r8vnl,
            AlgorithmType.Hodl,
            //AlgorithmType.DaggerHashimoto,
            //AlgorithmType.Decred,
            AlgorithmType.CryptoNight,
            //AlgorithmType.Lbry,
            AlgorithmType.Equihash
        };
        static int next = -1;
        public static void SetNextTest() {
            ++next;
            if (next >= testingAlgos.Count) next = 0;
            var mostProfitKeyName = AlgorithmNiceHashNames.GetName(testingAlgos[next]);
            Helpers.ConsolePrint("SWITCH_TESTING", String.Format("Setting most MostProfitKey to {0}", mostProfitKeyName));
        }

        static bool ForceNone = false;
        // globals testing variables
        static int seconds = 20;
        public static int SMAMinerCheckInterval = seconds * 1000; // 30s
        public static bool ForcePerCardMiners = false;
#endif

        public MiningDevice(ComputeDevice device)
        {
            Device = device;
            foreach (var algo in Device.AlgorithmSettings)
            {
                var isAlgoMiningCapable = GroupSetupUtils.IsAlgoMiningCapable(algo);
                var isValidMinerPath = true; // old crap remove
                if (isAlgoMiningCapable && isValidMinerPath)
                {
                    Algorithms.Add(algo);
                }
                else if (isAlgoMiningCapable && algo is PluginAlgorithm)
                {
                    Algorithms.Add(algo);
                }
            }

            MostProfitableAlgorithmType = AlgorithmType.NONE;
            MostProfitableMinerUUID = "NONE";
        }

        public ComputeDevice Device { get; }
        public List<Algorithm> Algorithms = new List<Algorithm>();

        public string GetMostProfitableString()
        {
            return
                MostProfitableMinerUUID
                + "_"
                + Enum.GetName(typeof(AlgorithmType), MostProfitableAlgorithmType);
        }

        public AlgorithmType MostProfitableAlgorithmType { get; private set; }

        public string MostProfitableMinerUUID { get; private set; }

        // prev state
        public AlgorithmType PrevProfitableAlgorithmType { get; private set; }

        public string PrevProfitableMinerBaseType { get; private set; }

        private int GetMostProfitableIndex()
        {
            return Algorithms.FindIndex((a) =>
                a.AlgorithmUUID == MostProfitableAlgorithmType && a.MinerUUID == MostProfitableMinerUUID);
        }

        private int GetPrevProfitableIndex()
        {
            return Algorithms.FindIndex((a) =>
                a.AlgorithmUUID == PrevProfitableAlgorithmType && a.MinerUUID == PrevProfitableMinerBaseType);
        }

        public double GetCurrentMostProfitValue
        {
            get
            {
                var mostProfitableIndex = GetMostProfitableIndex();
                if (mostProfitableIndex > -1)
                {
                    return Algorithms[mostProfitableIndex].CurrentProfit;
                }

                return 0;
            }
        }

        public double GetPrevMostProfitValue
        {
            get
            {
                var mostProfitableIndex = GetPrevProfitableIndex();
                if (mostProfitableIndex > -1)
                {
                    return Algorithms[mostProfitableIndex].CurrentProfit;
                }

                return 0;
            }
        }

        public MiningPair GetMostProfitablePair()
        {
            return new MiningPair(Device, Algorithms[GetMostProfitableIndex()]);
        }

        public bool HasProfitableAlgo()
        {
            return GetMostProfitableIndex() > -1;
        }

        public void RestoreOldProfitsState()
        {
            // restore last state
            MostProfitableAlgorithmType = PrevProfitableAlgorithmType;
            MostProfitableMinerUUID = PrevProfitableMinerBaseType;
        }

        public void SetNotMining()
        {
            // device isn't mining (e.g. below profit threshold) so set state to none
            PrevProfitableAlgorithmType = AlgorithmType.NONE;
            PrevProfitableMinerBaseType = "NONE";
            MostProfitableAlgorithmType = AlgorithmType.NONE;
            MostProfitableMinerUUID = "NONE";
        }

        public void CalculateProfits(Dictionary<AlgorithmType, double> profits)
        {
            // save last state
            PrevProfitableAlgorithmType = MostProfitableAlgorithmType;
            PrevProfitableMinerBaseType = MostProfitableMinerUUID;
            // assume none is profitable
            MostProfitableAlgorithmType = AlgorithmType.NONE;
            MostProfitableMinerUUID = "NONE";
            // calculate new profits
            foreach (var algo in Algorithms)
            {
                algo.UpdateCurProfit(profits);
            }

            // find max paying value and save key
            double maxProfit = 0;
            foreach (var algo in Algorithms)
            {
                if (maxProfit < algo.CurrentProfit)
                {
                    maxProfit = algo.CurrentProfit;
                    MostProfitableAlgorithmType = algo.AlgorithmUUID;
                    MostProfitableMinerUUID = algo.MinerUUID;
                }
            }
#if (SWITCH_TESTING)
            var devName = Device.GetFullName();
            // set new most profit
            if (Algorithms.ContainsKey(testingAlgos[next])) {
                MostProfitableKey = testingAlgos[next];
            } else if(ForceNone) {
                MostProfitableKey = AlgorithmType.NONE;
            }
            var mostProfitKeyName = AlgorithmNiceHashNames.GetName(MostProfitableKey);
            Helpers.ConsolePrint("SWITCH_TESTING", String.Format("Setting device {0} to {1}", devName, mostProfitKeyName));
#endif
        }
    }
}
