namespace YiQiDong.Core
{
    public class TagManager
    {
        public static TagManager Instance { get; } = new TagManager();
        private string[] tags = null;

        private TagManager()
        {
            Reload();
        }

        public string[] GetTags() => tags;

        public bool Contains(string tag)
        {
            return Array.IndexOf(tags, tag) >= 0;
        }

        public void Reload()
        {
            var tagHashSet = new HashSet<string>();
            foreach (var imageInfo in ImageManager.Instance.Query(null, null))
                if (imageInfo.Tags != null)
                    foreach (var tag in imageInfo.Tags)
                        if (!tagHashSet.Contains(tag))
                            tagHashSet.Add(tag);
            tags = tagHashSet.OrderBy(t => t).ToArray();
        }
    }
}
