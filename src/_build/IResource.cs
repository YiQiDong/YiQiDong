using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _build
{
    public interface IResource
    {
        public const string RUNTIME_META_FILE = "YiQiDong.Runtime.json";
        public const string IMAGE_META_FILE = "YiQiDong.Image.json";
        string Id { get; }
        string Name { get; }
        void Invoke();
    }
}
