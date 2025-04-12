using System.Collections.Generic;

namespace YiQiDong.Utils
{
    public class YmgFileUtils
    {
        public const int YMG_MAGIC_HEAD_LENGTH = 2;
        private static Dictionary<string, string> YMG_MAGIC_HEAD_DICT = new Dictionary<string, string>()
        {
            ["7z"] = "7z",
            ["yi"] = "7z",
            ["yz"] = "PK",
            ["PK"] = "PK"
        };

        /// <summary>
        /// 是否是YMG头
        /// </summary>
        /// <param name="head"></param>
        /// <returns></returns>
        public static bool IsYmgHead(string head) => YMG_MAGIC_HEAD_DICT.ContainsKey(head);

        public static string GetSrcFileHead(string head)
        {
            if (!IsYmgHead(head))
                return null;
            return YMG_MAGIC_HEAD_DICT[head];
        }
    }
}
