using System.ComponentModel.DataAnnotations;
using YiQiDong.Protocol.V1.Model;
using Quick.Blazor.Bootstrap;
using YiQiDong.Core;
using LogLevel = YiQiDong.Protocol.V1.Model.LogLevel;

namespace YiQiDong.Model
{
    public class CreateContainerModel : PropertyNotifyModel
    {
        private string _ImageId;
        [Required(ErrorMessage = "必须选择镜像编号")]
        public string ImageId
        {
            get { return _ImageId; }
            set
            {
                RaisePropertyChanging();
                _ImageId = value;
                if (!string.IsNullOrEmpty(_ImageId))
                {
                    var imageInfo = Core.ImageManager.Instance.Get(_ImageId);
                    if (imageInfo != null)
                    {
                        var idAndName = ContainerManager.Instance.GenerateNewContainerIdAndName(imageInfo.DefaultId ?? imageInfo.Id, imageInfo.Name);
                        Id = idAndName.Item1;
                        Name = idAndName.Item2;
                    }
                }
                RaisePropertyChanged();
            }
        }
        [Required(ErrorMessage = "必须输入编号")]
        [StringLength(100, ErrorMessage = "编号太长")]
        public string Id { get; set; }
        [Required(ErrorMessage = "必须输入名称")]
        [StringLength(100, ErrorMessage = "名称太长")]
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Tags { get; set; }
        public string[] RuntimeIds { get; set; }
        public string StartScript { get; set; }
        public string StartWarning { get; set; }
        public string StopScript { get; set; }
        public string StopWarning { get; set; }
        public bool EnableRecordLog { get; set; }
        public string EnableRecordLogStr
        {
            get { return EnableRecordLog.ToString(); }
            set { EnableRecordLog = bool.Parse(value); }
        }
        public string LogIgnoreList { get; set; }
        public LogLevel LogLevel { get; set; }
        public int LogSaveDays { get; set; }
        public string StartCron { get; set; }
        public string StopCron { get; set; }
        public string RestartCron { get; set; }
        public string EnvironmentVariables { get; set; }
    }
}
