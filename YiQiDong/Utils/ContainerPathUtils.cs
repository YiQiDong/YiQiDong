using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace YiQiDong.Utils
{
    public static class ContainerPathUtils
    {
        public static string GetContainerFolder()
        {
            return Path.Combine(FolderUtils.GetDataDir(), Core.Consts.CONTAINERS_FOLDER);
        }

        public static string GetContainerFolder(string containerId)
        {
            return Path.Combine(GetContainerFolder(), containerId);
        }
    }
}
