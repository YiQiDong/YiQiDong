using Quick.Fields;

namespace YiQiDong.Protocol.V1.Model
{
    public class FunctionRequest : FieldsForPostContainer
    {
        /// <summary>
        /// 功能编号
        /// </summary>
        public string FunctionId { get; set; }
        /// <summary>
        /// Session编号
        /// </summary>
        public string SessionId { get; set; }
    }
}
