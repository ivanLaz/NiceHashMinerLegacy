using Microsoft.Win32;
using NHM.Common;
using NHM.Common.Enums;
using NHM.DeviceDetection;
using NHM.DeviceMonitoring;
using NHMCore.ApplicationState;
using NHMCore.Configs;
using NHMCore.Mining;
using NHMCore.Mining.Plugins;
using NHMCore.Nhmws;
using NHMCore.Notifications;
using NHMCore.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static NHMCore.Translations;

namespace NHMCore
{
    static partial class ApplicationStateManager
    {
        private static bool isInitFinished = false;

        private class LoaderConverter : IStartupLoader
        {
            public IProgress<(string, int)> PrimaryProgress { get; }
            public IProgress<(string, int)> SecondaryProgress { get; }
            public string PrimaryTitle { get; set; }
            public string SecondaryTitle { get; set; }
            public bool SecondaryVisible { get; set; }

            public LoaderConverter(IProgress<(string, int)> prog, IProgress<(string, int)> prog2)
            {
                PrimaryProgress = prog;
                SecondaryProgress = prog2;
            }
        }

        public static async Task InitializeManagersAndMiners(IStartupLoader loader)
        {
            try
            {
                var allSteps = 15;
                var currentStep = 0;
                var nextProgPerc = new Func<int>(() =>
                {
                    ++currentStep;
                    var perc = (int)(((double)currentStep / allSteps) * 100);
                    if (perc > 100) return 100;
                    return perc;
                });
                // STEP
                // Checking System Memory
                loader.PrimaryProgress?.Report((Tr("Checking System Specs"), nextProgPerc()));
                await Task.Run(() => SystemSpecs.QueryWin32_OperatingSystemDataAndLog());
                await Task.Run(() => FilterOSSpecific.GetWindowsVersion());
                await WindowsUptimeCheck.DelayUptime();

                // TODO extract in function
                #region Device Detection

                // STEP +3
                Func<DeviceDetectionStep, string> detectionStepMessage = (DeviceDetectionStep step) =>
                {
                    switch (step)
                    {
                        case DeviceDetectionStep.CPU:
                            return Tr("Checking CPU Info");
                        case DeviceDetectionStep.NVIDIA_CUDA:
                            return Tr("Querying CUDA devices");
                        case DeviceDetectionStep.AMD_OpenCL:
                            return Tr("Checking AMD OpenCL GPUs");
                        default: //DeviceDetectionStep.WMIWMIVideoControllers
                            return Tr("Checking Windows Video Controllers");
                    }
                };
                var devDetectionProgress = new Progress<DeviceDetectionStep>(step => {
                    var msg = detectionStepMessage(step);
                    loader.PrimaryProgress?.Report((msg, nextProgPerc()));
                });
                await DeviceDetection.DetectDevices(devDetectionProgress);

                if (DeviceDetection.DetectionResult.IsOpenClFallback)
                {
                    AvailableNotifications.CreateOpenClFallbackInfo();
                }
                if (DeviceDetection.DetectionResult.IsDCHDriver) 
                {
                    AvailableNotifications.CreateWarningNVIDIADCHInfo();
                }
                if (DeviceDetection.DetectionResult.IsDCHDriver && !DeviceDetection.DetectionResult.IsNvmlFallback)
                {
                    AvailableNotifications.CreateNVMLFallbackFailInfo();
                }
                if (MiscSettings.Instance.UseEthlargement && !Helpers.IsElevated)
                {
                    AvailableNotifications.CreateEthlargementElevateInfo();
                }

                // add devices
                var detectionResult = DeviceDetection.DetectionResult;
                var index = 0;
                var cpuCount = 0;
                var cudaCount = 0;
                var amdCount = 0;
                foreach (var cDev in DeviceDetection.GetDetectedDevices())
                {
                    var nameCount = "";
                    if (cDev.DeviceType == DeviceType.CPU)
                    {
                        cpuCount++;
                        nameCount = $"CPU#{cpuCount}";
                    }
                    if (cDev.DeviceType == DeviceType.AMD)
                    {
                        amdCount++;
                        nameCount = $"AMD#{amdCount}";
                    }
                    if (cDev.DeviceType == DeviceType.NVIDIA)
                    {
                        cudaCount++;
                        nameCount = $"GPU#{cudaCount}";
                    }
                    AvailableDevices.AddDevice(new ComputeDevice(cDev, index++, nameCount));
                }
                AvailableDevices.UncheckCpuIfGpu();
                var ramCheckOK = SystemSpecs.CheckRam(AvailableDevices.AvailGpus, AvailableDevices.AvailNvidiaGpuRam, AvailableDevices.AvailAmdGpuRam);
                if (!ramCheckOK)
                {
                    AvailableNotifications.CreateIncreaseVirtualMemoryInfo();
                }
                // no compatible devices? exit
                if (AvailableDevices.Devices.Count == 0)
                {
                    NoDeviceAction?.Invoke();
                    return;
                }

                // STEP
                loader.PrimaryProgress?.Report((Tr("Initializing device monitoring"), nextProgPerc()));
                var monitors = await DeviceMonitorManager.GetDeviceMonitors(AvailableDevices.Devices.Select(d => d.BaseDevice), detectionResult.IsDCHDriver);
                foreach (var monitor in monitors)
                {
                    var dev = AvailableDevices.GetDeviceWithUuid(monitor.UUID);
                    dev.SetDeviceMonitor(monitor);
                }
                // now init device settings
                ConfigManager.InitDeviceSettings();

                if (!Helpers.IsElevated && !GlobalDeviceSettings.Instance.DisableDevicePowerModeSettings && AvailableDevices.HasNvidia)
                {
                    AvailableNotifications.CreateDeviceMonitoringNvidiaElevateInfo();
                }
                //// TODO add check and only show if not enabled
                //if (AvailableDevices.HasCpu)
                //{
                //    AvailableNotifications.CreateEnableLargePagesInfo();
                //}
                // TODO add check and only show if not enabled
                if (AvailableDevices.HasAmd)
                {
                    AvailableNotifications.CreateEnableComputeModeAMDInfo();
                }

                #endregion Device Detection

                // TODO ADD STEP AND MESSAGE
                await MinerPluginsManager.CheckAndSwapInstalledExternalPlugins();
                MinerPluginsManager.CheckAndDeleteNewVersion3Bins();

                // STEP
                // load plugins
                loader.PrimaryProgress?.Report((Tr("Loading miner plugins..."), nextProgPerc()));
                // Plugin Loading
                await MinerPluginsManager.LoadAndInitMinerPlugins();
                MinerPluginsManager.StartLoops(ExitApplication.Token);
                // commit again benchmarks after loading plugins
                ConfigManager.CommitBenchmarks();
                /////////////////////////////////////////////
                /////// from here on we have our devices and Miners initialized
                MiningState.Instance.CalculateDevicesStateChange();

                // STEP
                // connect to nhmws
                loader.PrimaryProgress?.Report((Tr("Connecting to nhmws..."), nextProgPerc()));
                // Init ws connection
                var (btc, worker, group) = CredentialsSettings.Instance.GetCredentials();
                NHWebSocket.SetCredentials(btc, worker, group);
                NHWebSocket.StartLoop(NHM.Common.Nhmws.NhmSocketAddress, ExitApplication.Token);
                

                // STEP
                // disable windows error reporting
                loader.PrimaryProgress?.Report((Tr("Setting Windows error reporting..."), nextProgPerc()));
                Helpers.DisableWindowsErrorReporting(WarningSettings.Instance.DisableWindowsErrorReporting);

                // STEP
                // Nvidia p0
                loader.PrimaryProgress?.Report((Tr("Changing all supported NVIDIA GPUs to P0 state..."), nextProgPerc()));
                if (MiningSettings.Instance.NVIDIAP0State && AvailableDevices.HasNvidia)
                {
                    Helpers.SetNvidiaP0State();
                }

                // STEP
                // Downloading integrated plugins bins, TODO put this in some internals settings
                var hasMissingMinerBins = MinerPluginsManager.GetMissingMiners().Count > 0;
                if (hasMissingMinerBins)
                {
                    loader.SecondaryTitle = Tr("Downloading Miner Binaries");
                    loader.SecondaryVisible = true;

                    loader.PrimaryProgress?.Report((Tr("Downloading Miner Binaries..."), nextProgPerc()));
                    await MinerPluginsManager.DownloadMissingMinersBins(loader.SecondaryProgress, ExitApplication.Token);
                    //await MinersDownloader.MinersDownloadManager.DownloadAndExtractOpenSourceMinersWithMyDownloaderAsync(progressDownload, ExitApplication.Token);
                    loader.SecondaryVisible = false;
                    if (ExitApplication.IsCancellationRequested) return;
                }

                // STEP
                // Update miner plugin binaries
                var hasPluginMinerUpdate = MinerPluginsManager.HasMinerUpdates();
                if (hasPluginMinerUpdate)
                {
                    loader.SecondaryTitle = Tr("Updating Miner Binaries");
                    loader.SecondaryVisible = true;

                    loader.PrimaryProgress?.Report((Tr("Updating Miner Binaries..."), nextProgPerc()));
                    await MinerPluginsManager.UpdateMinersBins(loader.SecondaryProgress, ExitApplication.Token);
                    loader.SecondaryVisible = false;
                    if (ExitApplication.IsCancellationRequested) return;
                }

                //var shouldAutoIncreaseVRAM = Registry.CurrentUser.GetValue(@"Software\" + APP_GUID.GUID + @"\AutoIncreaseVRAM", false);
                //if (shouldAutoIncreaseVRAM == null)
                //{
                //    AvailableNotifications.CreateIncreaseVirtualMemoryInfo();
                //} else
                //{
                //    if ((bool)shouldAutoIncreaseVRAM == true)
                //    {
                //        var vramSum = AvailableDevices.AvailNvidiaGpuRam + AvailableDevices.AvailAmdGpuRam;
                //    }
                //}

                if (!WindowsDefender.IsAlreadySet())
                {
                    AvailableNotifications.CreateAddWindowsDefenderExceptionInfo();
                }

                // re-check after download we should have all miner files
                var missingMinerBins = MinerPluginsManager.GetMissingMiners().Count > 0;
                if (missingMinerBins)
                {
                    AvailableNotifications.CreateMissingMinersInfo();
                }
                // fire up mining manager loop
                MiningManager.StartLoops(ExitApplication.Token, GetUsername());

                // STEP
                // VC_REDIST check
                loader.PrimaryProgress?.Report((Tr("Checking VC_REDIST..."), nextProgPerc()));
                VC_REDIST_x64_2015_2019_DEPENDENCY_PLUGIN.Instance.InstallVcRedist();

                // STEP
                if (FirewallRules.RunFirewallRulesOnStartup)
                {
                    loader.PrimaryProgress?.Report((Tr("Checking Firewall Rules..."), nextProgPerc()));
                    if (FirewallRules.IsFirewallRulesOutdated())
                    {
                        // requires UAC
                        // TODO show message box
                        FirewallRules.UpdateFirewallRules();
                    }
                }
                else
                {
                    loader.PrimaryProgress?.Report((Tr("Skipping Firewall Rules..."), nextProgPerc()));
                }

                // STEP
                // Cross reference plugin indexes
                loader.PrimaryProgress?.Report((Tr("Cross referencing miner device IDs..."), nextProgPerc()));
                // Detected devices cross reference with miner indexes
                await MinerPluginsManager.DevicesCrossReferenceIDsWithMinerIndexes(loader);
            }
            catch (Exception e)
            {
                Logger.Error("ApplicationStateManager.Init", $"Exception: {e.Message}");
            }
            finally
            {
                isInitFinished = true;
                NHWebSocket.NotifyStateChanged();

                // start update checker
                // updater loops after we finish
                UpdateHelpers.StartLoops(ExitApplication.Token);
                // restore last mining states
                await RestoreMiningState();
            }
        }
    }
}
