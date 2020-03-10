﻿using MinerPluginToolkitV1.Configs;
using MinerPluginToolkitV1.ExtraLaunchParameters;
using System;
using System.Collections.Generic;

namespace GMinerPlugin
{
    internal static class PluginInternalSettings
    {
        internal static TimeSpan DefaultTimeout = new TimeSpan(1, 15, 0);

        internal static MinerApiMaxTimeoutSetting GetApiMaxTimeoutConfig = new MinerApiMaxTimeoutSetting
        {
            GeneralTimeout = DefaultTimeout,
        };

        internal static MinerOptionsPackage MinerOptionsPackage = new MinerOptionsPackage
        {
            GeneralOptions = new List<MinerOption>
            {
                /// <summary>
                /// personalization string for equihash algorithm (for example: 'BgoldPoW', 'BitcoinZ', 'Safecoin')
                /// </summary>
                new MinerOption
                {
                    Type = MinerOptionType.OptionWithSingleParameter,
                    ID = "gminer_pers",
                    LongName = "--pers",
                },
                /// <summary>
                /// enable/disable power efficiency calculator. Power efficiency calculator display of energy efficiency statistics of GPU in S/w, higher CPU load. Default value is '1' ('0' - off or '1' - on)
                /// </summary>
                new MinerOption
                {
                    Type = MinerOptionType.OptionWithSingleParameter,
                    ID = "gminer_pec",
                    LongName = "--pec=",
                    DefaultValue = "1"
                },
                /// <summary>
                /// enable/disable NVML
                /// </summary>
                new MinerOption
                {
                    Type = MinerOptionType.OptionWithSingleParameter,
                    ID = "gminer_nvml",
                    LongName = "--nvml=",
                    DefaultValue = "1"
                },
                /// <summary>
                /// enable/disable CUDA platform
                /// </summary>
                new MinerOption
                {
                    Type = MinerOptionType.OptionWithSingleParameter,
                    ID = "gminer_cuda",
                    LongName = "--cuda=",
                },
                /// <summary>
                /// enable/disable OpenCL platform
                /// </summary>
                new MinerOption
                {
                    Type = MinerOptionType.OptionWithSingleParameter,
                    ID = "gminer_opencl",
                    LongName = "--opencl=",
                },
                /// <summary>
                /// pass cost of electricity in USD per kWh, miner will report $ spent to mining
                /// </summary>
                new MinerOption
                {
                    Type = MinerOptionType.OptionWithSingleParameter,
                    ID = "gminer_electricity",
                    LongName = "--electricity_cost="
                },
                /// <summary>
                /// option to control GPU intensity (--intensity, 1-100)
                /// </summary>
                new MinerOption
                {
                    Type = MinerOptionType.OptionWithMultipleParameters,
                    ID = "gminer_intensity",
                    ShortName = "-i",
                    LongName = "--intensity",
                    Delimiter = " "
                },
                /// <summary>
                /// log filename
                /// </summary>
                new MinerOption
                {
                    Type = MinerOptionType.OptionWithSingleParameter,
                    ID = "gminer_logfile",
                    ShortName = "-l",
                    LongName = "--logfile"
                },
                /// <summary>
                /// enable/disable color output
                /// </summary>
                new MinerOption
                {
                    Type = MinerOptionType.OptionWithSingleParameter,
                    ID = "gminer_color",
                    ShortName = "-c",
                    LongName = "--color="
                },
                /// <summary>
                /// space-separated list of OC modes for each device
                /// </summary>
                new MinerOption
                {
                    Type = MinerOptionType.OptionWithMultipleParameters,
                    ID = "gminer_oc",
                    LongName = "--oc",
                    Delimiter = " "
                },
                /// <summary>
                /// enable OC1 for all devices
                /// </summary>
                new MinerOption
                {
                    Type = MinerOptionType.OptionIsParameter,
                    ID = "gminer_oc1",
                    LongName = "--oc1"
                },
                /// <summary>
                /// space-separated list of intensities for secondary algorithm in dual mining mode (0-10)
                /// </summary>
                new MinerOption
                {
                    Type = MinerOptionType.OptionWithMultipleParameters,
                    ID = "gminer_dual_intensity",
                    LongName = "--dual_intensity"
                }
            },
            TemperatureOptions = new List<MinerOption>{
                /// <summary>
                /// space-separated list of temperature limits, upon reaching the limit, the GPU stops mining until it cools down, can be empty (for example: '85 80 75')
                /// </summary>
                new MinerOption
                {
                    Type = MinerOptionType.OptionWithMultipleParameters,
                    ID = "gminer_templimit",
                    ShortName = "-t",
                    LongName = "--templimit",
                    DefaultValue = "90",
                    Delimiter = " "
                }
            }
        };
    }
}
