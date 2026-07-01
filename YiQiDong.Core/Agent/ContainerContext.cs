using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Core.Agent
{
    public class ContainerContext : ContainerInfo
    {
        public string ContainerFolder { get; internal set; }
        public string ImageFolder { get; internal set; }

        public ContainerContext(ContainerInfo containerInfo)
        {
            AutoStart = containerInfo.AutoStart;
            Description = containerInfo.Description;
            Id = containerInfo.Id;
            Image = containerInfo.Image;
            ImageId = containerInfo.ImageId;
            LogIgnoreList = containerInfo.LogIgnoreList;
            LogLevel = containerInfo.LogLevel;
            Name = containerInfo.Name;
            RuntimeIds = containerInfo.RuntimeIds;
            Tags = containerInfo.Tags;
        }
    }
}
