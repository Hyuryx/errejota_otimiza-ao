using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace ErrejotaMatrixOptimizer
{
    public class HardwareSpecs
    {
        public string CpuName { get; set; } = "Identificando...";
        public int CpuCores { get; set; }
        public int CpuThreads { get; set; }
        public bool IsMultiCcdAmd { get; set; }

        public string GpuName { get; set; } = "Identificando...";
        public string GpuDriver { get; set; } = "";
        public string ScreenResolution { get; set; } = "";
        public string RefreshRate { get; set; } = "240";

        public double RamTotalGb { get; set; }
        public string RamSpeed { get; set; } = "";
        public bool IsRamCompressionActive { get; set; }
        public long SvcHostSplitKb { get; set; }

        public string StorageSummary { get; set; } = "";
        public string NetworkCardName { get; set; } = "";
        public string NetworkLinkSpeed { get; set; } = "1 Gbps";
        public bool IsNetworkLsoActive { get; set; } = true;
        public bool IsGreenEthernetDisabled { get; set; } = true;
        public string NetworkCongestionProvider { get; set; } = "cubic";
        public int NetworkMtu { get; set; } = 1500;
        public string PrimaryDns { get; set; } = "";
        public string WindowsVersion { get; set; } = "";

        // Status dos gargalos atuais
        public bool IsMpoDisabled { get; set; }
        public bool IsBcdDynamicTickDisabled { get; set; }
        public bool IsWin32PriorityOptimized { get; set; }
        public bool IsGpuMsiActive { get; set; }
        public bool IsFivemInstalled { get; set; }
        public bool IsFivemCcd0Active { get; set; }
        public bool IsNetworkBuffersOptimized { get; set; }
        public bool IsNetworkThrottlingDisabled { get; set; }

        public string GtaSettingsPath { get; set; } = "";
        public int CurrentGtaSamplingMode { get; set; } = 0;
        public int GtaScreenWidth { get; set; } = 1920;
        public int GtaScreenHeight { get; set; } = 1080;
        public int GtaRefreshRate { get; set; } = 239;
        public string GtaGrassQuality { get; set; } = "0";
        public string GtaCityDensity { get; set; } = "0.2";
        public string GtaTextureQuality { get; set; } = "2";

        public int HealthScore { get; set; } = 85;
    }

    public static class HardwareScanner
    {
        public static HardwareSpecs Scan()
        {
            var specs = new HardwareSpecs();

            // 1. CPU
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
                foreach (ManagementObject mo in searcher.Get())
                {
                    specs.CpuName = mo["Name"]?.ToString()?.Trim() ?? "Desconhecido";
                    specs.CpuCores = Convert.ToInt32(mo["NumberOfCores"] ?? 0);
                    specs.CpuThreads = Convert.ToInt32(mo["NumberOfLogicalProcessors"] ?? 0);
                    break;
                }

                if (specs.CpuName.Contains("Ryzen", StringComparison.OrdinalIgnoreCase) &&
                    (specs.CpuName.Contains("9 ") || specs.CpuCores >= 12))
                {
                    specs.IsMultiCcdAmd = true;
                }
            }
            catch { }

            // 2. GPU & Telas
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, DriverVersion, VideoModeDescription, CurrentRefreshRate FROM Win32_VideoController");
                foreach (ManagementObject mo in searcher.Get())
                {
                    string name = mo["Name"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(name))
                    {
                        specs.GpuName = name;
                        specs.GpuDriver = mo["DriverVersion"]?.ToString() ?? "";
                        specs.ScreenResolution = mo["VideoModeDescription"]?.ToString() ?? "";
                        specs.RefreshRate = mo["CurrentRefreshRate"]?.ToString() ?? "240";
                        break;
                    }
                }
            }
            catch { }

            // 3. Memória RAM
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Capacity, Speed FROM Win32_PhysicalMemory");
                long totalBytes = 0;
                string speed = "";
                foreach (ManagementObject mo in searcher.Get())
                {
                    totalBytes += Convert.ToInt64(mo["Capacity"] ?? 0);
                    if (string.IsNullOrEmpty(speed) && mo["Speed"] != null)
                    {
                        speed = mo["Speed"].ToString() + " MHz";
                    }
                }
                specs.RamTotalGb = Math.Round((double)totalBytes / (1024 * 1024 * 1024), 1);
                specs.RamSpeed = speed;
            }
            catch { }

            // 4. SO & Windows
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Caption, BuildNumber FROM Win32_OperatingSystem");
                foreach (ManagementObject mo in searcher.Get())
                {
                    specs.WindowsVersion = $"{mo["Caption"]} (Build {mo["BuildNumber"]})";
                    break;
                }
            }
            catch { }

            // 5. Discos
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Model, Size FROM Win32_DiskDrive");
                var disks = new List<string>();
                foreach (ManagementObject mo in searcher.Get())
                {
                    string model = mo["Model"]?.ToString() ?? "";
                    long size = Convert.ToInt64(mo["Size"] ?? 0);
                    int sizeGb = (int)(size / (1024 * 1024 * 1024));
                    disks.Add($"{model} ({sizeGb} GB)");
                }
                specs.StorageSummary = string.Join(" | ", disks.Take(3));
            }
            catch { }

            // 6. Rede
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, Speed FROM Win32_NetworkAdapter WHERE NetConnectionStatus = 2");
                foreach (ManagementObject mo in searcher.Get())
                {
                    specs.NetworkCardName = mo["Name"]?.ToString() ?? "Ethernet";
                    var spd = mo["Speed"];
                    if (spd != null && long.TryParse(spd.ToString(), out long spdBps))
                    {
                        if (spdBps >= 2_000_000_000) specs.NetworkLinkSpeed = "2.5 Gbps";
                        else if (spdBps >= 1_000_000_000) specs.NetworkLinkSpeed = "1 Gbps";
                        else if (spdBps >= 100_000_000) specs.NetworkLinkSpeed = "100 Mbps";
                    }
                    break;
                }
            }
            catch { }

            // 7. Status do Registro (MPO, Win32PrioritySeparation, SvcHost, IFEO, Rede)
            try
            {
                // MPO
                using var dwmKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Dwm");
                if (dwmKey != null)
                {
                    var val = dwmKey.GetValue("OverlayTestMode");
                    if (val != null && Convert.ToInt32(val) == 5)
                    {
                        specs.IsMpoDisabled = true;
                    }
                }

                // Win32PrioritySeparation
                using var prioKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\PriorityControl");
                if (prioKey != null)
                {
                    var val = prioKey.GetValue("Win32PrioritySeparation");
                    if (val != null && Convert.ToInt32(val) == 0x28) // 40 dec
                    {
                        specs.IsWin32PriorityOptimized = true;
                    }
                }

                // SvcHost Split Threshold
                using var ctrlKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control");
                if (ctrlKey != null)
                {
                    var val = ctrlKey.GetValue("SvcHostSplitThresholdInKB");
                    if (val != null)
                    {
                        specs.SvcHostSplitKb = Convert.ToInt64(val);
                    }
                }

                // FiveM IFEO CCD0
                using var ifeoKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\FiveM.exe\PerfOptions");
                if (ifeoKey != null)
                {
                    var val = ifeoKey.GetValue("CpuAffinityMask");
                    if (val != null && Convert.ToInt64(val) == 65535)
                    {
                        specs.IsFivemCcd0Active = true;
                    }
                }

                // Multimedia Network Throttling
                using var mmKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile");
                if (mmKey != null)
                {
                    var val = mmKey.GetValue("NetworkThrottlingIndex");
                    if (val != null && unchecked((uint)Convert.ToInt32(val)) == 0xFFFFFFFF)
                    {
                        specs.IsNetworkThrottlingDisabled = true;
                    }
                }

                // FiveM instalado
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string fivemExe = Path.Combine(localAppData, @"FiveM\FiveM.exe");
                specs.IsFivemInstalled = File.Exists(fivemExe);

                // GTA V settings.xml
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string gtaXml = Path.Combine(docs, @"Rockstar Games\GTA V\settings.xml");
                if (File.Exists(gtaXml))
                {
                    specs.GtaSettingsPath = gtaXml;
                    string xmlContent = File.ReadAllText(gtaXml);

                    var mSampling = Regex.Match(xmlContent, @"<SamplingMode\s+value=""(\d+)""");
                    if (mSampling.Success && int.TryParse(mSampling.Groups[1].Value, out int sMode))
                    {
                        specs.CurrentGtaSamplingMode = sMode;
                    }

                    var mW = Regex.Match(xmlContent, @"<ScreenWidth\s+value=""(\d+)""");
                    if (mW.Success && int.TryParse(mW.Groups[1].Value, out int w)) specs.GtaScreenWidth = w;

                    var mH = Regex.Match(xmlContent, @"<ScreenHeight\s+value=""(\d+)""");
                    if (mH.Success && int.TryParse(mH.Groups[1].Value, out int h)) specs.GtaScreenHeight = h;

                    var mHz = Regex.Match(xmlContent, @"<RefreshRate\s+value=""(\d+)""");
                    if (mHz.Success && int.TryParse(mHz.Groups[1].Value, out int hz)) specs.GtaRefreshRate = hz;

                    var mGrass = Regex.Match(xmlContent, @"<GrassQuality\s+value=""([^""]+)""");
                    if (mGrass.Success) specs.GtaGrassQuality = mGrass.Groups[1].Value;

                    var mCity = Regex.Match(xmlContent, @"<CityDensity\s+value=""([^""]+)""");
                    if (mCity.Success) specs.GtaCityDensity = mCity.Groups[1].Value;

                    var mTex = Regex.Match(xmlContent, @"<TextureQuality\s+value=""([^""]+)""");
                    if (mTex.Success) specs.GtaTextureQuality = mTex.Groups[1].Value;
                }

                // DNS
                using var tcpipKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters");
                if (tcpipKey != null)
                {
                    specs.PrimaryDns = tcpipKey.GetValue("DhcpNameServer")?.ToString() ?? tcpipKey.GetValue("NameServer")?.ToString() ?? "";
                }
            }
            catch { }

            // Cálculo do Índice de Saúde / Otimização (Score 0-100)
            int score = 40;
            if (specs.IsMpoDisabled) score += 12;
            if (specs.IsWin32PriorityOptimized) score += 12;
            if (specs.IsFivemCcd0Active) score += 12;
            if (specs.IsNetworkThrottlingDisabled) score += 12;
            if (specs.SvcHostSplitKb > 0) score += 12;
            specs.HealthScore = Math.Min(100, score);

            return specs;
        }
    }
}
