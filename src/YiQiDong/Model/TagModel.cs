using Quick.LiteDB.Plus;
using System.ComponentModel.DataAnnotations.Schema;

namespace YiQiDong.Model
{
    [Table(nameof(TagModel))]
    public class TagModel : BaseModel
    {
        public TagModel() { }
        public TagModel(string id) { Id = id; }
    }
}
