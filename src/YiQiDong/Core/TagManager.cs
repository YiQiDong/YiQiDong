using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Quick.LiteDB.Plus;

namespace YiQiDong.Core
{
    public class TagManager
    {
        public static TagManager Instance { get; } = new TagManager();

        public string[] GetTags() => ConfigDbContext.CacheContext
            .Query<Model.TagModel>()
            .Select(t => t.Id)
            .OrderBy(t => t)
            .ToArray();

        public bool Contains(string tag)
        {
            return ConfigDbContext.CacheContext.Find(new Model.TagModel(tag)) != null;
        }

        public void Add(string tag)
        {
            ConfigDbContext.CacheContext.Add(new Model.TagModel(tag));
        }

        public void Delete(string tag)
        {
            var images = ImageManager.Instance.Query(tag, null);
            if (images.Length > 0)
                throw new ApplicationException($"镜像[{images[0].Name} {images[0].Version}]正在使用此标签");
            var containers = ContainerManager.Instance.Query(tag, null);
            if (containers.Length > 0)
                throw new ApplicationException($"容器[{containers[0].ContainerInfo.Name}]正在使用此标签");
            var model = ConfigDbContext.CacheContext.Find(new Model.TagModel(tag));
            if (model != null)
                ConfigDbContext.CacheContext.Remove(model);
        }
    }
}
