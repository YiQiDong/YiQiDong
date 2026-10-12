using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Quick.Blazor.Bootstrap;
using YiQiDong.Core;
using YiQiDong.Utils;

namespace YiQiDong.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase
    {
        [Inject]
        private AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        [Inject]
        public Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; }
    
        private bool IsLoading = true;
        public string Title => Program.Config.Title;

        public string ActiveKey => null;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                IsLoading = false;
                _ = InvokeAsync(StateHasChanged);
            }
        }

        private async Task Logout()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            LoginTokenManager.Instance.Logout(authState.User.Identity.Name);
            ((MyAuthenticationStateProvider)AuthenticationStateProvider).SignOut();
            await InvokeAsync(StateHasChanged);
        }

        private bool IsShowDataFolderWarning() => !DebugUtils.IsDebug() && UpdateUtils.IsDataFolderInDanger();
    }
}
