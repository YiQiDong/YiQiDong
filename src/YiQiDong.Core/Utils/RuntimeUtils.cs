using Quick.Shell;
using Quick.Shell.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace YiQiDong.Core.Utils;

public class RuntimeUtils
{
    private static string currentRID;
    /// <summary>
    /// 获取当前运行库标识符
    /// </summary>
    /// <returns></returns>
    public static string GetCurrentRID()
    {
        if (string.IsNullOrEmpty(currentRID))
        {
            var os = "unknown";
            if (OperatingSystem.IsWindows())
                os = "win";
            else if (OperatingSystem.IsLinux())
                os = "linux";
            else if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
                os = "osx";
            else if (OperatingSystem.IsFreeBSD())
                os = "freebsd";
            else if (OperatingSystem.IsAndroid())
                os = "android";
            else if (OperatingSystem.IsIOS())
                os = "ios";
            var arch = RuntimeInformation.OSArchitecture.ToString().ToLower();
            currentRID = $"{os}-{arch}";
        }
        return currentRID;
    }

    public static bool IsMatchRID(string rid)
    {
        if (rid == "any")
            return true;
        if (rid == GetCurrentRID())
            return true;
        rid = rid.Replace("_", "-");
        return rid == GetCurrentRID();
    }

    public static string CombineEnviromentPath(string prePath, IEnumerable<string> pathList)
    {
        IEnumerable<string> pathEnumerable = null;
        //将原PATH添加到最后
        if (string.IsNullOrEmpty(prePath))
            pathEnumerable = pathList;
        else
            pathEnumerable = pathList.Union(new[] { prePath });
        //处理PATH
        var paths = string.Join(OperatingSystem.IsWindows() ? ";" : ":", pathEnumerable);
        return paths;
    }

    public static string CombineEnviromentPath(IEnumerable<string> pathList)
    {
        return CombineEnviromentPath(Environment.GetEnvironmentVariable("PATH"), pathList);
    }

    private static Process _StartProcess(ProcessStartInfo psi, IEnumerable<string> pathList)
    {
        var pathArray = pathList?.ToArray();
        if (pathArray == null || pathArray.Length == 0)
            return Process.Start(psi);

        lock (typeof(RuntimeUtils))
        {
            var prePath = Environment.GetEnvironmentVariable("PATH");
            var path = CombineEnviromentPath(prePath, pathList);
            try
            {
                Environment.SetEnvironmentVariable("PATH", path);
                psi.Environment["PATH"] = path;
                return Process.Start(psi);
            }
            catch
            {
                throw;
            }
            finally
            {
                //还原PATH变量
                Environment.SetEnvironmentVariable("PATH", prePath);
            }
        }
    }

    public static Process StartProcess(string filename, string[] args, IEnumerable<string> pathList)
    {
        var psi = ProcessUtils.CreateProcessStartInfo(filename, args);
        return _StartProcess(psi, pathList);
    }

    public static Process StartProcess(ProcessStartInfo psi, IEnumerable<string> pathList)
    {
        ProcessUtils.ProcessProcessStartInfo(psi);
        return _StartProcess(psi, pathList);
    }
}
