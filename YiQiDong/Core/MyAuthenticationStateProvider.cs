using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace YiQiDong.Core;

public class MyAuthenticationStateProvider : ServerAuthenticationStateProvider, IHostEnvironmentAuthenticationStateProvider
{
    private class TempIdentity : System.Security.Principal.IIdentity
    {
        public string AuthenticationType { get; set; }

        public bool IsAuthenticated { get; set; }

        public string Name { get; set; }
    }

    public void SignIn(string loginToken)
    {
        SetAuthenticationState(
            Task.FromResult(
                new AuthenticationState(
                    new ClaimsPrincipal(
                        new TempIdentity() { Name = loginToken, AuthenticationType = "root", IsAuthenticated = true }))));
    }

    public void SignOut()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var anonymousState = new AuthenticationState(anonymousUser);
        SetAuthenticationState(Task.FromResult(anonymousState));
    }
}
