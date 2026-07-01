using System;
using System.Linq;

namespace YiQiDong.Model;

public class NameAndVersion
{
    public string Name { get; set; }
    public string Version { get; set; }

    public static NameAndVersion Parse(string line)
    {
        var strs = line.Split('-', System.StringSplitOptions.RemoveEmptyEntries);
        if (strs.Length < 2)
            throw new FormatException($"[{line}]不是有效的名称与版本字符串");
        var version = strs[strs.Length - 1];
        var name = string.Join('-', strs.Take(strs.Length - 1).ToArray());
        return new NameAndVersion() { Name = name, Version = version };
    }
}
