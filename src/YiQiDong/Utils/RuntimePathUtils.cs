using System.IO;

namespace YiQiDong.Utils
{
    public static class RuntimePathUtils
    {   
        public static string GetRuntimeFolder()
        {
            return Path.Combine(FolderUtils.GetDataDir(), Consts.RUNTIMES_FOLDER);
        }

        public static string GetRuntimeFolder(string runtimeId)
        {
            return Path.Combine(GetRuntimeFolder(), runtimeId);
        }
    }
}