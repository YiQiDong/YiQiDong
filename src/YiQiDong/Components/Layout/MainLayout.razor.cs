using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Quick.Blazor.Bootstrap;
using YiQiDong.Core;
using YiQiDong.Utils;

namespace YiQiDong.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase, IPageNavigater, IDisposable
    {
        private NavMenu navMenu;
        private bool IsLoading = true;
        public bool IsLogin { get; private set; } = false;
        public string Title => Program.Config.Title;
        public string Message { get; private set; }
        private string CorrectPassword => Program.Config.Password;

        public string Password { get; set; }

        public string ActiveKey => null;

        [Inject]
        public Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; }
        [Inject]
        public NavigationManager NavigationManager { get; set; }

        private Lazy<string> LoginTokenKey;

#if DEBUG
        protected override void OnInitialized()
        {
            base.OnInitialized();
            Password = CorrectPassword;
            LoginTokenKey = new Lazy<string>(() => NavigationManager.Uri + "_token");
        }
#endif

        private string loginToken;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                loginToken = await LocalStorage.GetItemAsStringAsync(LoginTokenKey.Value);
                if (!string.IsNullOrEmpty(loginToken))
                {
                    IsLogin = LoginTokenManager.Instance.Verify(loginToken);
                    if (IsLogin)
                        LoginTokenManager.Instance.UsingToken(loginToken);
                    else
                        await LocalStorage.RemoveItemAsync(LoginTokenKey.Value);
                }
                IsLoading = false;
                _ = InvokeAsync(StateHasChanged);
            }
        }

        public void OnPost()
        {
            if (!IsLogin && CorrectPassword != Password)
            {
                Message = "密码不正确！";
                return;
            }
            IsLogin = true;
            var token = Guid.NewGuid().ToString("N");
            LoginTokenManager.Instance.UsingToken(token);
            LocalStorage.SetItemAsStringAsync(LoginTokenKey.Value, token);
            StateHasChanged();
        }

        private void onPasswordKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
                OnPost();
        }

        private void Logout()
        {
            LocalStorage.RemoveItemAsync(LoginTokenKey.Value);
            LoginTokenManager.Instance.Logout(loginToken);
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

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(loginToken))
                LoginTokenManager.Instance.UnusingToken(loginToken);
        }
    }
}
