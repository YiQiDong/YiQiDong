using Quick.Fields;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Core.Functions;

public abstract class AbstractPagesFunction : AbstractFunction
{
    public abstract class AbstractPage
    {
        public string PageId { get; set; }
        public AbstractPage()
        {
            PageId = GetType().FullName;
        }

        /// <summary>
        /// 导航到指定页面
        /// </summary>
        /// <param name="response"></param>
        /// <param name="pageId"></param>
        public static void Navigate(List<FieldForGet> response, string pageId)
        {
            response[0].Value = pageId;
        }

        /// <summary>
        /// 导航到指定页面
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        public static void Navigate<T>(List<FieldForGet> response)
        {
            Navigate(response, typeof(T).FullName);
        }

        protected string GetDictValue(Dictionary<string, string> variableDict, string key)
        {
            if (variableDict.TryGetValue(key, out var value))
                return value;
            return null;
        }

        public abstract void Execute(FunctionRequest request, List<FieldForGet> response, Dictionary<string, string> variableDict);

        public abstract void Render(FunctionRequest request, List<FieldForGet> response, Dictionary<string, string> variableDict);
    }


    public const string YIQIDONG_CORE_ABSTRACT_PAGES_FUNCTION_CURRENT_PAGE = nameof(YIQIDONG_CORE_ABSTRACT_PAGES_FUNCTION_CURRENT_PAGE);

    private string defaultPage;
    private Dictionary<string, AbstractPage> pageDict;
    public AbstractPagesFunction(AbstractPage[] pages)
    {
        pageDict = pages.ToDictionary(t => t.PageId, t => t);
        defaultPage = pages.FirstOrDefault()?.PageId;
    }

    public override FieldForGet[] Execute(FunctionRequest request)
    {
        var response = new List<FieldForGet>();
        var variableDict = new Dictionary<string, string>();
        var currentPage = request?.GetFieldValue(YIQIDONG_CORE_ABSTRACT_PAGES_FUNCTION_CURRENT_PAGE);
        if (currentPage == null)
            currentPage = defaultPage;
        var currentPageField = new FieldForGet()
        {
            Id = YIQIDONG_CORE_ABSTRACT_PAGES_FUNCTION_CURRENT_PAGE,
            Type = FieldType.InputHidden,
            Value = currentPage
        };
        response.Add(currentPageField);

        if (request != null)
        {
            if (pageDict.TryGetValue(currentPage, out var page))
                page.Execute(request, response, variableDict);
        }
        currentPage = currentPageField.Value;
        if (currentPage == null)
            currentPage = defaultPage;
        {
            if (pageDict.TryGetValue(currentPage, out var page))
                page.Render(request, response, variableDict);
        }
        return response.ToArray();
    }
}
