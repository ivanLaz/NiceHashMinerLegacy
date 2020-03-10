﻿using MinerPlugin;
using MinerPluginToolkitV1.Interfaces;
using Newtonsoft.Json;
using NHM.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MinerPluginToolkitV1.Configs
{
    [Serializable]
    public class MinerCustomActionSettings : IInternalSetting
    {
        [JsonProperty("use_user_settings")]
        public bool UseUserSettings { get; set; } = false;

        public class MinerCustomActionEntry
        {
            [JsonProperty("start")]
            public string StartExePath { get; set; }
            [JsonProperty("wait_start_exec")]
            public bool StartExePathWaitExec { get; set; }
            [JsonProperty("stop")]
            public string StopExePath { get; set; }
            [JsonProperty("wait_stop_exec")]
            public bool StopExePathWaitExec { get; set; }
        }


        [JsonProperty("algorithm_custom_actions")]
        public Dictionary<string, MinerCustomActionEntry> AlgorithmCustomActions { get; set; } = null;
    }
}
