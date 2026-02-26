using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.Admin.Utils;
using Quick.Shell.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using YiQiDong.Core.Utils;
using YiQiDong.Core.Utils.Unix;

namespace YiQiDong.Utils
{
    public class SystemInfoContext : IDisposable
    {
        public class DisplayDriverInfo
        {
            public string DisplayName { get; set; }
            public string Name { get; set; }
            public string DriveFormat { get; set; }
            public string DriveType { get; set; }
            public long Total { get; set; }
            public long Used { get; set; }
            public string TotalStr => storageUnitStringConverting.GetString(Total, 2, false) + "B";
            public string UsedStr => storageUnitStringConverting.GetString(Used, 2, false) + "B";
            public int UsedPercent => Total == 0 ? 0 : Convert.ToInt32(Used * 100 / Total);
            public BackgroundTheme Background
            {
                get
                {
                    if (UsedPercent >= 90)
                        return BackgroundTheme.danger;
                    if (UsedPercent >= 80)
                        return BackgroundTheme.warning;
                    return BackgroundTheme.success;
                }
            }
        }

        public class DisplayNetworkInterfaceInfo
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string MacAddress { get; set; }
            public string[] IpAddressArray { get; set; }
            public string OperationalStatus { get; set; }
            public string NetworkInterfaceType { get; set; }
        }

        private static UnitStringConverting storageUnitStringConverting = UnitStringConverting.StorageUnitStringConverting;
        private static string[] ignoreNetworkInterfaceDescriptionPrefixs = new[]
        {
            "docker",
            "veth"
        };
        private static string[] ignoreNetworkInterfaceDescriptionSuffixs = new[]
        {
            "Filter-0000",
            "Scheduler-0000",
            "Driver-0000"
        };

        private const int chartsMaxDataCount = 100;
        private SystemInfoWatcher systemInfoWatcher;
        private Timer shortTimer, longTimer;

        public double?[][] CpuChartsData = new double?[chartsMaxDataCount][];
        public double?[][] MemoryUsedChartsData = new double?[chartsMaxDataCount][];

        private Queue<double?[]> CpuChartsDataQueue = new Queue<double?[]>();
        private Queue<double?[]> MemoryUsedChartsDataQueue = new Queue<double?[]>();

        public DisplayDriverInfo[] DriverInfos;
        public DisplayDriverInfo CpuInfo;
        public DisplayDriverInfo MemoryInfo { get; set; }
        public DisplayNetworkInterfaceInfo[] NetworkInterfaceInfos;
        public double MemoryTotalInUnit { get; set; }
        /// <summary>
        /// 内存单位
        /// </summary>
        public string MemoryUnit { get; set; }
        /// <summary>
        /// 操作系统名称
        /// </summary>
        public string OperateSystemName { get; set; }
        /// <summary>
        /// 内核名称
        /// </summary>
        public string KernelName { get; set; }
        /// <summary>
        /// CPU名称
        /// </summary>
        public string CpuName { get; set; }
        /// <summary>
        /// 进程架构
        /// </summary>
        public string ProcessArchitecture { get; set; }
        /// <summary>
        /// 机器名
        /// </summary>
        public string MachineName { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 当前时区
        /// </summary>
        public string TimeZone { get; set; }
        /// <summary>
        /// 语言区域
        /// </summary>
        public string Locale { get; set; }
        /// <summary>
        /// 版本版本
        /// </summary>
        public string SoftwareVersion { get; set; }
        public int ProcessId { get; set; }
        public string ProcessStartTime { get; set; }
        public string RuntimeFrameworkDescription { get; set; }
        public string RunMethod { get; set; }
        public string ByteOrder { get; set; }
        public string SoftwareArch { get; set; }
        public string WorkingDir { get; set; }
        public string DataDir { get; set; }
        public string CurrentTime { get; set; }
        public string ProcessUsedMemory { get; set; }
        public event EventHandler DataChanged;
        public SystemInfoContext()
        {
            var nowTime = DateTime.Now;
            for (var i = 0; i < chartsMaxDataCount; i++)
            {
                CpuChartsDataQueue.Enqueue(new double?[] { new Epoch.net.LongEpochTime(nowTime.AddSeconds(i - chartsMaxDataCount)).Epoch, null });
                MemoryUsedChartsDataQueue.Enqueue(new double?[] { new Epoch.net.LongEpochTime(nowTime.AddSeconds(i - chartsMaxDataCount)).Epoch, null });
            }

            Task.Run(() =>
            {
                CpuInfo = new DisplayDriverInfo();
                CpuInfo.Total = 100;
                MemoryInfo = new DisplayDriverInfo();

                try
                {
                    systemInfoWatcher = new SystemInfoWatcher();
                }
                catch { }
                //数据改变事件是否在处理中
                var isDataChangedEventHandling = false;
                shortTimer = new System.Threading.Timer(_ =>
                {
                    try
                    {
                        if (isDataChangedEventHandling)
                            return;
                        isDataChangedEventHandling = true;
                        shortTimeRefresh();
                        DataChanged?.Invoke(this, EventArgs.Empty);
                    }
                    finally
                    {
                        isDataChangedEventHandling = false;
                    }
                }, null, 1000, 1000);
                longTimer = new System.Threading.Timer(_ =>
                {
                    longTimeRefresh();
                }, null, 1000, 10000);
                oneTimeRefresh();
            });
        }

        //只刷新一次
        private void oneTimeRefresh()
        {
            try
            {
                var cultureInfo = System.Globalization.CultureInfo.CurrentCulture;
                OperateSystemName = systemInfoWatcher?.GetOperateSystemName();
                KernelName = systemInfoWatcher?.GetKernelName();
                ProcessArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
                CpuName = systemInfoWatcher?.GetCpuName();
                MachineName = Environment.MachineName;
                try
                {
                    UserName = ProcessUtils.ExecuteShell("whoami").Output.Split("\\").LastOrDefault();
                }
                catch
                {
                    UserName = Environment.UserName;
                }
                TimeZone = TimeZoneInfo.Local.StandardName;
                Locale = $"{cultureInfo.NativeName}/{cultureInfo.Name}";
                SoftwareVersion = Consts.Version;
                SoftwareArch = Consts.ARCH;

                var process = System.Diagnostics.Process.GetCurrentProcess();
                process.Refresh();

                ProcessId = process.Id;
                ProcessStartTime = process.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                RuntimeFrameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
                RunMethod = "Host";
                if (!OperatingSystem.IsWindows())
                {
                    if (UnixUtils.IsRuningInChroot())
                        RunMethod = "chroot";
                    else if (UnixUtils.IsRuningInDocker())
                        RunMethod = "Docker";
                }
                ByteOrder = BitConverter.IsLittleEndian ? "Little Endian" : "Big Endian";
                WorkingDir = Environment.CurrentDirectory;
                DataDir = Program.Config.DataFolder;
            }
            catch (Exception ex)
            {
                DataDir = ExceptionUtils.GetExceptionString(ex);
            }
        }

        //短时间刷新一次
        private void shortTimeRefresh()
        {
            CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (systemInfoWatcher != null)
            {
                UseCpuAndMemoryQueue(() =>
                {
                    try
                    {
                        CpuInfo.Used = systemInfoWatcher.GetCpuUsagePercent();
                        pushData(CpuChartsDataQueue, CpuInfo.Used, CpuChartsData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("获取CPU信息时出错，原因：" + ExceptionUtils.GetExceptionString(ex));
                        CpuInfo.Used = 0;
                    }
                    try
                    {
                        var rawMemoryInfo = systemInfoWatcher.GetMemoryInfo();
                        if (rawMemoryInfo == null)
                            throw new NullReferenceException("systemInfoWatcher.GetMemoryInfo() return null.");
                        MemoryInfo.Total = rawMemoryInfo.Total;
                        MemoryInfo.Used = rawMemoryInfo.Used;
                        MemoryUnit = storageUnitStringConverting.GetFitUnitString(MemoryInfo.Total);

                        MemoryTotalInUnit = double.Parse(storageUnitStringConverting.GetUnits(MemoryInfo.Total, MemoryUnit).ToString("N1"));
                        var memoryUsedInUnit = double.Parse(storageUnitStringConverting.GetUnits(MemoryInfo.Used, MemoryUnit).ToString("N1"));
                        pushData(MemoryUsedChartsDataQueue, memoryUsedInUnit, MemoryUsedChartsData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("获取内存信息时出错，原因：" + ExceptionUtils.GetExceptionString(ex));
                        MemoryInfo.Total = 0;
                        MemoryInfo.Used = 0;
                    }
                });
            }
        }

        //长时间刷新一次
        private void longTimeRefresh()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            process.Refresh();
            ProcessUsedMemory = storageUnitStringConverting.GetString(process.WorkingSet64, 2, true) + "B";
            try
            {
                DriverInfos = DriveInfo.GetDrives()?
                    .Where(t => t.IsReady
                                && t.Name != "/tmp"
                                && t.Name != "/var/tmp"
                                && !t.Name.StartsWith("/etc/")
                                && !t.Name.StartsWith("/run/")
                                && t.TotalSize > 0
                                && !string.IsNullOrEmpty(t.DriveFormat)
                                && t.DriveFormat != "tmpfs"
                                && t.DriveFormat != "squashfs"
                                && t.DriveFormat != "overlay"
                                && t.DriveType != DriveType.Ram)?
                    .Select(t =>
                    {
                        var model = new DisplayDriverInfo()
                        {
                            DisplayName = (string.IsNullOrEmpty(t.VolumeLabel) || t.Name == t.VolumeLabel) ? t.Name : $"{t.VolumeLabel}({t.Name})",
                            Name = t.Name,
                            DriveFormat = t.DriveFormat.ToString(),
                            DriveType = t.DriveType.ToString(),
                            Total = t.TotalSize,
                            Used = t.TotalSize - t.TotalFreeSpace
                        };
                        return model;
                    })?.ToArray();
            }
            catch
            {
                DriverInfos = null;
            }
            try
            {
                NetworkInterfaceInfos = NetworkInterface.GetAllNetworkInterfaces()?
                    .Where(t => t.OperationalStatus == OperationalStatus.Up
                            && t.NetworkInterfaceType != NetworkInterfaceType.Loopback
                            && t.NetworkInterfaceType != NetworkInterfaceType.Ppp
                            && t.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                            && !ignoreNetworkInterfaceDescriptionPrefixs.Any(i => t.Description.StartsWith(i))
                            && !ignoreNetworkInterfaceDescriptionSuffixs.Any(i => t.Description.EndsWith(i)))?
                    .Select(t =>
                    {
                        return new DisplayNetworkInterfaceInfo()
                        {
                            Name = t.Name,
                            Description = t.Description,
                            MacAddress = t.GetPhysicalAddress().ToString(),
                            IpAddressArray = t.GetIPProperties().UnicastAddresses
                            .Select(t => t.Address)
                            .Where(t => !t.IsIPv6LinkLocal && !t.IsIPv6Teredo)
                            .Select(t => t.ToString()).ToArray(),
                            OperationalStatus = t.OperationalStatus.ToString(),
                            NetworkInterfaceType = t.NetworkInterfaceType.ToString()
                        };
                    })?
                    .Where(t => !string.IsNullOrEmpty(t.MacAddress))?
                    .ToArray();
            }
            catch
            {
                NetworkInterfaceInfos = null;
            }
        }

        private void pushData(Queue<double?[]> queue, double value, double?[][] buffer)
        {
            queue.Enqueue(new double?[] { Epoch.net.LongEpochTime.Now.Epoch, value });
            if (queue.Count > chartsMaxDataCount)
                queue.Dequeue();
            queue.CopyTo(buffer, 0);
        }

        public void UseCpuAndMemoryQueue(Action action)
        {
            lock (this)
                action?.Invoke();
        }

        public void Dispose()
        {
            longTimer?.Dispose();
            shortTimer?.Dispose();
        }
    }
}
