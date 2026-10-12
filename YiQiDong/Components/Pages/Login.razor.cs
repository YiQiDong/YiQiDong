using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using YiQiDong.Core;

namespace YiQiDong.Components.Pages;

public partial class Login
{
    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; }
    [Inject]
    public Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; }
    [Inject]
    public NavigationManager NavigationManager { get; set; }
    private string LoginTokenKey;
    private string loginToken;

    private string CorrectPassword => Program.Config.Password;
    public string Password { get; set; }
    public string Message { get; private set; }


    protected override void OnInitialized()
    {
        base.OnInitialized();
#if DEBUG
        Password = CorrectPassword;
#endif
        LoginTokenKey = NavigationManager.Uri + "_token";
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            loginToken = await LocalStorage.GetItemAsStringAsync(LoginTokenKey);
            if (!string.IsNullOrEmpty(loginToken))
            {
                if(LoginTokenManager.Instance.Verify(loginToken))
                {
                    LoginTokenManager.Instance.UsingToken(loginToken);
                    ((MyAuthenticationStateProvider)AuthenticationStateProvider).SignIn(loginToken);
                }
                else
                {
                    await LocalStorage.RemoveItemAsync(LoginTokenKey);
                    ((MyAuthenticationStateProvider)AuthenticationStateProvider).SignOut();
                }
            }
            _ = InvokeAsync(StateHasChanged);
        }
    }

    public void OnPost()
    {
        if (CorrectPassword != Password)
        {
            Message = "密码不正确！";
            return;
        }
        var token = Guid.NewGuid().ToString("N");
        LoginTokenManager.Instance.UsingToken(token);
        LocalStorage.SetItemAsStringAsync(LoginTokenKey, token);
        ((MyAuthenticationStateProvider)AuthenticationStateProvider).SignIn(token);
    }

    private void onPasswordKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            OnPost();
    }

}
