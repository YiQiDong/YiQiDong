using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YiQiDong.Core.Utils
{
    public static class IniUtils
    {
        public static Dictionary<string, string> Load(string content)
        {
            if (content == null)
                return null;
            return Load(content.Split(new char[] { '\r', '\n' }));
        }

        public static Dictionary<string, string> Load(string[] lines)
        {
            var properties = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                if (line.StartsWith("#"))
                    continue;
                var spIndex = line.IndexOf('=');
                if (spIndex <= 0)
                    continue;
                var key = line.Substring(0, spIndex);
                var value = line.Substring(spIndex + 1).Trim();
                properties[key] = value;
            }
            return properties;
        }
    }
}
