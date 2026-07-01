using Microsoft.AspNetCore.Components;
using System.Text;

namespace YiQiDong.Components.Controls
{
    public partial class FileManageControl
    {
        [Parameter]
        public string Dir { get; set; }
        [Parameter]
        public string SelectedPath { get; set; }
        [Parameter]
        public string FileFilter { get; set; }

        public static Dictionary<string, Encoding> EncodingDict { get; private set; }

        static FileManageControl()
        {
            EncodingDict = new Dictionary<string, Encoding>()
            {
                ["UTF-8"] = new UTF8Encoding(false),
                ["UTF-8 BOM"] = new UTF8Encoding(true),
                ["ASCII"] = Encoding.ASCII,
                ["GB18030"] = Encoding.GetEncoding("GB18030"),
                ["Unicode"] = Encoding.Unicode
            };
        }
    }
}
