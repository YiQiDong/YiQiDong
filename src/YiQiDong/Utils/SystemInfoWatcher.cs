using Quick.Shell.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using System.Runtime.InteropServices.ComTypes;
using Quick.Shell.WinCmd;
using Windows.Win32.System.SystemInformation;
using System.ComponentModel;

namespace YiQiDong.Utils
{
    public class SystemInfoWatcher
    {
        private string osName;
        private string kernelName;
        private string cpuName;
        private long memoryTotalSize;
        private int pageSize_macos;
        private long preCpuIdleTime = 0;
        private long preCpuTotalTime = 0;

        private static Dictionary<string, string> GetSystemInfo(string command, string sp = ":")
        {
            var ret = ProcessUtils.ExecuteShell(command);
            return ConsoleUtils.ConsoleOutputParse(ret.Output, sp);
        }

        public SystemInfoWatcher()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    //计算机\HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0\ProcessorNameString
                    using (var subKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                    {
                        cpuName = subKey.GetValue("ProcessorNameString").ToString().Trim();
                        subKey.Dispose();
                    }
                    //获取操作系统名称
                    {
                        //优先用wmic从WMI获取操作系统名称。Win11以上操作系统默认不再启用wmic功能。
                        var ret = ProcessUtils.ExecuteShell("wmic os get caption");
                        if (ret.ExitCode == 0)
                        {
                            osName = ret.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                        }
                        //其次用Get-CimInstance从WMI中获取操作系统名称。
                        if (string.IsNullOrEmpty(osName))
                        {
                            using(var context = new Quick.Shell.PowerShell.PowerShellCommandContext())
                            {
                                context.Open();
                                //优先使用Get-CimInstance查询
                                var cmdRet = context.ExecuteCommand("Get-CimInstance -Class Win32_OperatingSystem | Select-Object -Property Caption",true);
                                //其次使用Get-WmiObject查询
                                if (cmdRet.ExitCode != 0)
                                    cmdRet = context.ExecuteCommand("Get-WmiObject -Class Win32_OperatingSystem | Select-Object -Property Caption",true);
                                if (cmdRet.ExitCode == 0)
                                    osName = cmdRet.Output.LastOrDefault();
                            }
                        }
                        //其次从注册表中获取操作系统名称
                        if (string.IsNullOrEmpty(osName))
                        {
                            //计算机\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProductName
                            using (var subKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                            {
                                osName = subKey.GetValue("ProductName").ToString().Trim();
                                subKey.Dispose();
                            }
                        }
                    }

                    //获取内核信息
                    {
                        var ret = ProcessUtils.ExecuteShell("ver");
                        if (ret.ExitCode == 0)
                            kernelName = ret.Output.Trim();
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    //获取操作系统名称
                    GetSystemInfo("system_profiler SPSoftwareDataType | grep 'System Version'").TryGetValue("System Version", out osName);
                    //获取内核名称
                    {
                        var ret = ProcessUtils.ExecuteShell("uname -sr");
                        if (ret.ExitCode == 0)
                            kernelName = ret.Output.Trim();
                    }
                    var hardwareInfoDict= GetSystemInfo("system_profiler SPHardwareDataType");
                    //获取CPU名称
                    hardwareInfoDict.TryGetValue("Processor Name", out cpuName);
                    if (string.IsNullOrEmpty(cpuName))
                        hardwareInfoDict.TryGetValue("Chip", out cpuName);
                    if (string.IsNullOrEmpty(cpuName))
                        cpuName = ProcessUtils.ExecuteShell("sysctl -n machdep.cpu.brand_string").Output?.Trim();
                    //获取内存信息
                    var memDict = GetSystemInfo("sysctl -a | grep hw.memsize");
                    string memoryTotalSizeStr;
                    if (memDict.TryGetValue("hw.memsize_usable", out memoryTotalSizeStr))
                        memoryTotalSize = long.Parse(memoryTotalSizeStr);
                    else if (memDict.TryGetValue("hw.memsize", out memoryTotalSizeStr))
                        memoryTotalSize = long.Parse(memoryTotalSizeStr);
                    var pageSizeRet = ProcessUtils.ExecuteShell("pagesize");
                    pageSize_macos = int.Parse(pageSizeRet.Output);
                }
                else
                {
                    //获取操作系统名称
                    {
                        var dict = GetSystemInfo("cat /etc/os-release", "=");
                        if (dict.ContainsKey("PRETTY_NAME"))
                            osName = dict["PRETTY_NAME"];
                        else
                            osName = dict["NAME"] + " " + dict["VERSION"];
                    }
                    //获取内核名称
                    {
                        var ret = ProcessUtils.ExecuteShell("uname -sr");
                        if (ret.ExitCode == 0)
                            kernelName = ret.Output.Trim();
                    }
                    //获取CPU名称
                    {
                        if (!GetSystemInfo("lscpu").TryGetValue("Model name", out cpuName))
                            GetSystemInfo("cat /proc/cpuinfo").TryGetValue("model name", out cpuName);
                    }
                }
            }
            catch { }

            if (string.IsNullOrEmpty(osName))
                osName = RuntimeInformation.OSDescription;
            if (string.IsNullOrEmpty(cpuName))
                cpuName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
        }

        public string GetOperateSystemName() => osName;

        public string GetKernelName() => kernelName;

        public string GetCpuName() => cpuName;

        /// <summary>
        /// 获取CPU使用率百分比值
        /// </summary>
        /// <returns></returns>
        public int GetCpuUsagePercent()
        {
            if (OperatingSystem.IsWindows())
            {
                long lIdleTime = default;
                long lKernelTime = default;
                long lUserTime = default;

                long currentCpuIdleTime = 0;
                long currentCpuTotalTime = 0;
                int idlePercent = 0;
                //如果不是第一次采集，则正常计算
                if (preCpuIdleTime > 0)
                {
                    if (Win32Utils.GetSystemTimes(ref lIdleTime, ref lKernelTime, ref lUserTime))
                    {
                        currentCpuIdleTime = lIdleTime;
                        currentCpuTotalTime = lKernelTime + lUserTime;
                    }
                    var fenMu = currentCpuTotalTime - preCpuTotalTime;
                    if (fenMu == 0)
                        idlePercent = 100;
                    else
                        idlePercent = Convert.ToInt32((currentCpuIdleTime - preCpuIdleTime) * 100 / fenMu);
                    preCpuIdleTime = currentCpuIdleTime;
                    preCpuTotalTime = currentCpuTotalTime;
                }
                //否则是第一次采集，设定为100%空闲
                else
                {
                    if (Win32Utils.GetSystemTimes(ref lIdleTime, ref lKernelTime, ref lUserTime))
                    {
                        preCpuIdleTime = lIdleTime;
                        preCpuTotalTime = lKernelTime + lUserTime;
                    }
                    idlePercent = 100;
                }
                return 100 - idlePercent;
            }
            else if (OperatingSystem.IsMacOS())
            {
                var ret = ProcessUtils.ExecuteShell("top -l  2 | grep -E \"^CPU\" | tail -1 | awk '{ print $3 + $5 }'");
                if (ret.ExitCode == 0)
                    return Convert.ToInt32(double.Parse(ret.Output));
                return 0;
            }
            else
            {
                using (var fs = File.OpenRead("/proc/stat"))
                using (var reader = new StreamReader(fs))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (line.StartsWith("cpu "))
                        {
                            var strs = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                            long currentCpuTotalTime = 0;
                            long currentCpuIdleTime = 0;

                            //user
                            if (strs.Length > 1)
                                currentCpuTotalTime += long.Parse(strs[1]);
                            //nice
                            if (strs.Length > 2)
                                currentCpuTotalTime += long.Parse(strs[2]);
                            //system
                            if (strs.Length > 3)
                                currentCpuTotalTime += long.Parse(strs[3]);
                            //idle
                            if (strs.Length > 4)
                            {
                                currentCpuIdleTime = long.Parse(strs[4]);
                                currentCpuTotalTime += currentCpuIdleTime;
                            }
                            //iowait
                            if (strs.Length > 5)
                                currentCpuTotalTime += long.Parse(strs[5]);
                            //irq
                            if (strs.Length > 6)
                                currentCpuTotalTime += long.Parse(strs[6]);
                            //softirq
                            if (strs.Length > 7)
                                currentCpuTotalTime += long.Parse(strs[7]);
                            //steal
                            if (strs.Length > 8)
                                currentCpuTotalTime += long.Parse(strs[8]);
                            //guest
                            if (strs.Length > 9)
                                currentCpuTotalTime += long.Parse(strs[9]);
                            //guest_nice
                            if (strs.Length > 10)
                                currentCpuTotalTime += long.Parse(strs[10]);

                            int idlePercent = 0;
                            //如果不是第一次采集，则正常计算
                            if (preCpuIdleTime > 0)
                            {
                                var fenMu = currentCpuTotalTime - preCpuTotalTime;
                                if (fenMu == 0)
                                    idlePercent = 100;
                                else
                                    idlePercent = Convert.ToInt32((currentCpuIdleTime - preCpuIdleTime) * 100 / fenMu);
                            }
                            //否则是第一次采集，设定为100%空闲
                            else
                            {
                                idlePercent = 100;
                            }
                            preCpuIdleTime = currentCpuIdleTime;
                            preCpuTotalTime = currentCpuTotalTime;
                            return 100 - idlePercent;
                        }
                    }
                    throw new IOException("Can't read cpu usage info from /proc/stat");
                }
            }
        }

        public class MemoryInfo
        {
            public long Total { get; set; }
            public long Free { get; set; }
            public long Used { get; set; }
        }

        public MemoryInfo GetMemoryInfo()
        {
            if (OperatingSystem.IsWindows())
            {
#pragma warning disable CA1416 // 验证平台兼容性
                MEMORYSTATUSEX memoryStatus = new();
                memoryStatus.dwLength=(uint)Marshal.SizeOf(memoryStatus);
                PInvoke.GlobalMemoryStatusEx(ref memoryStatus);
#pragma warning restore CA1416 // 验证平台兼容性
                var free = (long)memoryStatus.ullAvailPhys;
                var total = (long)memoryStatus.ullTotalPhys;
                var used = total - free;
                return new MemoryInfo()
                {
                    Free = free,
                    Used = used,
                    Total = total
                };
            }
            else if (OperatingSystem.IsMacOS())
            {
                var dict = GetSystemInfo("memory_pressure");
                var freePercentage = int.Parse(dict["System-wide memory free percentage"].Replace("%", string.Empty));

                var total = memoryTotalSize;
                var free = total * freePercentage / 100;
                var used = total - free;
                return new MemoryInfo()
                {
                    Free = free,
                    Used = used,
                    Total = total
                };
            }
            else
            {
                using (var fs = File.OpenRead("/proc/meminfo"))
                using (var reader = new StreamReader(fs))
                {
                    long? total = null;
                    long? free = null;

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var index = line.IndexOf(":");
                        if (index <= 0)
                            continue;
                        var key = line.Substring(0, index);
                        var str = line.Substring(index + 1).Trim();
                        var strs = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        switch (key)
                        {
                            case "MemTotal":
                            case "MemFree":
                            case "MemAvailable":
                                var value = long.Parse(strs[0]);
                                if (strs.Length >= 2)
                                {
                                    var unit = strs[1];
                                    switch (unit)
                                    {
                                        case "kB":
                                            value *= 1024;
                                            break;
                                    }
                                }
                                switch (key)
                                {
                                    case "MemTotal":
                                        total = value;
                                        break;
                                    case "MemFree":
                                    case "MemAvailable":
                                        free = value;
                                        break;
                                }
                                break;
                            default:
                                continue;
                        }
                    }
                    if (free.HasValue && total.HasValue)
                    {
                        return new MemoryInfo()
                        {
                            Free = free.Value,
                            Used = total.Value - free.Value,
                            Total = total.Value
                        };
                    }
                    throw new IOException("Can't read meminfo from /proc/meminfo");
                }
            }
        }
    }
}
