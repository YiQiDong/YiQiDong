using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Quick.Blazor.Bootstrap;
using YiQiDong.Utils;

namespace YiQiDong.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase, IPageNavigater
    {
        private NavMenu navMenu;
        public bool IsLogin { get; private set; } = false;
        public string Title => Program.Config.Title;
        public string Message { get; private set; }
        private string CorrectPassword => Program.Config.Password;

        //[BindProperty]
        public string Password { get; set; }

        public string ActiveKey => null;

#if DEBUG
        protected override void OnInitialized()
        {
            base.OnInitialized();
            Password = CorrectPassword;
        }
#endif

        public void OnPost()
        {
            if (!IsLogin && CorrectPassword != Password)
            {
                Message = "密码不正确！";
                return;
            }
            IsLogin = true;
            StateHasChanged();
        }

        private void onPasswordKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
                OnPost();
        }
        
        private void Logout()
        {
            IsLogin = false;
        }
        
        public void Navigate(string activeKey, Type componentType, Dictionary<string, object> parameterDict)
        {
            //更改菜单选中状态
            if (!string.IsNullOrEmpty(activeKey))
                navMenu.ChangeActiveKey(activeKey);
            //跳转到页面
            parameterDict[nameof(IPageNavigater)] = this;
            Body = Quick.Blazor.Bootstrap.Utils.BlazorUtils.GetRenderFragment(componentType, parameterDict);
            StateHasChanged();
        }

        private void Show<T>()
        {
            Body = Quick.Blazor.Bootstrap.Utils.BlazorUtils.GetRenderFragment<T>(new Dictionary<string, object>()
            {
                [nameof(IPageNavigater)] = this
            });
        }

        private bool IsShowDataFolderWarning() => !DebugUtils.IsDebug() && UpdateUtils.IsDataFolderInDanger();
    }
}
