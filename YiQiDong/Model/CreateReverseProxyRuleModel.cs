using System.ComponentModel.DataAnnotations;
using Quick.Blazor.Bootstrap;

namespace YiQiDong.Model
{
    public class CreateReverseProxyRuleModel : PropertyNotifyModel
    {
        private string _Name;
        [Required(ErrorMessage = "必须输入名称")]
        public string Name
        {
            get { return _Name; }
            set
            {
                RaisePropertyChanging();
                _Name = value;
                RaisePropertyChanged();
            }
        }

        private string _Path;
        [Required(ErrorMessage = "必须输入路径")]
        public string Path
        {
            get { return _Path; }
            set
            {
                RaisePropertyChanging();
                _Path = value?.Trim();
                RaisePropertyChanged();
            }
        }

        private string _Url;
        [Required(ErrorMessage = "必须输入URL")]
        public string Url
        {
            get { return _Url; }
            set
            {
                RaisePropertyChanging();
                _Url = value;
                RaisePropertyChanged();
            }
        }
    }
}
