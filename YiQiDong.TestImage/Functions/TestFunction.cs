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
                Description =@"1.此文本框是只读的
2.会显示当前时间

3.描述是多行的",
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