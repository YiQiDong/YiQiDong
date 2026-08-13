using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using YiQiDong.Core;

namespace YiQiDong.Model
{
    public class CreateContainerModel : YqdContainerInfo, INotifyPropertyChanging, INotifyPropertyChanged
    {
        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanging([CallerMemberName] string propertyName = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        public void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [Required(ErrorMessage = "必须选择镜像编号")]
        public override string ImageId
        {
            get { return base.ImageId; }
            set
            {
                RaisePropertyChanging();
                base.ImageId = value;
                if (!string.IsNullOrEmpty(base.ImageId))
                {
                    var imageInfo = ImageManager.Instance.Get(base.ImageId);
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
        public override string Id { get => base.Id; set => base.Id = value; }

        [Required(ErrorMessage = "必须输入名称")]
        [StringLength(100, ErrorMessage = "名称太长")]
        public override string Name { get => base.Name; set => base.Name = value; }
        public override int TransportTimeout
        {
            get => base.TransportTimeout;
            set
            {
                if (value < 3000)
                    return;
                base.TransportTimeout = value;
            }
        }

        public string EnableRecordLogStr
        {
            get { return EnableRecordLog.ToString(); }
            set { EnableRecordLog = bool.Parse(value); }
        }
    }
}
