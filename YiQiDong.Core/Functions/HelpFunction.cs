using Quick.Fields;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Core.Functions;

public class HelpFunction : AbstractFunction
{
    public override string Name => "帮助";
    private Dictionary<string, string> helpDict;

    public HelpFunction(Dictionary<string, string> helpDict)
    {
        this.helpDict = helpDict;
    }

    public override FieldForGet[] Execute(FunctionRequest request)
    {
        return
        [
            new()
            {
                Type =  FieldType.ContainerTab,
                Children = helpDict.Select(t=>new FieldForGet()
                {
                    Name=t.Key,
                    Type = FieldType.ContainerGroup,
                    Children = t.Value
                        .Split([Environment.NewLine], StringSplitOptions.None)
                        .Select(u=>
                        {
                            var p = u.Trim();
                            if(string.IsNullOrEmpty(p))
                                return new FieldForGet(){ Type = FieldType.ContainerRow,MarginBottom = 3};
                            return new FieldForGet(){ Type = FieldType.HtmlParagraph,Value = p};
                        }).ToArray()
                }).ToArray()
            }
        ];
    }
}
