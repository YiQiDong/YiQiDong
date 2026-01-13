using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace YiQiDong.Utils
{
    public static class ImagePathUtils
    {
        public static string GetImageFolder()
        {
            return Path.Combine(FolderUtils.GetDataDir(), Core.Consts.IMAGES_FOLDER);
        }

        public static string GetImageFolder(string imageId)
        {
            return Path.Combine(GetImageFolder(), imageId);
        }
    }
}
