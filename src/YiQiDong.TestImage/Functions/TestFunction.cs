using Quick.Fields;
using YiQiDong.Core;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.TestImage.Functions;

public class TestFunction : AbstractFunction
{
    public override string Name => "测试功能";

    public override FieldForGet[] Execute(FunctionRequest request)
    {
        return [
            new ()
            {
                Id="txtCurrentTime",
                Name = "当前时间",
                Type = FieldType.InputText,
                Input_ReadOnly = true,
                Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            },
            new ()
            {
                Id="btnRefresh",
                Name = "刷新",
                Type = FieldType.Button
            }
        ];
    }
}