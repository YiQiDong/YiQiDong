using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YiQiDong.Core
{
    public class LoginMiddleware
    {
        public static LoginMiddleware Instance { get; private set; }
        private RequestDelegate _next;
        public string AccessToken { get; private set; }

        public string NewAccessToken()
        {
            AccessToken = Guid.NewGuid().ToString("N");
            return AccessToken;
        }

        public bool ValidateAccessToken(string accessToken)
        {
            if (string.IsNullOrEmpty(AccessToken))
                return false;
            if (AccessToken == accessToken)
            {
                AccessToken = null;
                return true;
            }
            return false;
        }

        public bool ValidateAccessToken(HttpContext httpContext)
        {
            return ValidateAccessToken(httpContext.Request.Query[nameof(AccessToken)]);
        }

        public LoginMiddleware(RequestDelegate next = null)
        {
            Instance = this;
            _next = next;
            Quick.Blazor.Bootstrap.ReverseProxy.ReverseProxyManager.Instance.RuleAdded += ReverseProxyManager_RuleAdded;
            Quick.Blazor.Bootstrap.ReverseProxy.ReverseProxyManager.Instance.RuleRemoved += ReverseProxyManager_RuleRemoved;
            if (Program.IsStartSuccess)
                foreach (var path in Quick.Blazor.Bootstrap.ReverseProxy.ReverseProxyManager.Instance.GetRules(null).Select(t => t.Path))
                    ReverseProxyManager_RuleAdded(Quick.Blazor.Bootstrap.ReverseProxy.ReverseProxyManager.Instance, path);
        }

        private static List<string> whiteUrlList = new List<string>(new[]
            {
                "/_blazor",
                "/north",
                "/cluster",
                "/glash",
                "/Login",
                "/api/container",
                "/api/veritas"
            }
        );

        private static List<string> whiteUrlPrefixList = new List<string>(new[]
            {
                "/ws/",
                "/_blazor/",
                "/_framework/",
                "/Quick.WebFileTransfer/",
                "/api/container/",
                "/api/veritas/"
            }
        );
        private static List<string> proxyUrlList = new List<string>();
        private static List<string> proxyUrlPrefixList = new List<string>();

        private void ReverseProxyManager_RuleAdded(object sender, string e)
        {
            if (e.EndsWith("/"))
                proxyUrlPrefixList.Add(e);
            else
                proxyUrlList.Add(e);
        }

        private void ReverseProxyManager_RuleRemoved(object sender, string e)
        {
            if (e.EndsWith("/"))
                proxyUrlPrefixList.Remove(e);
            else
                proxyUrlList.Remove(e);
        }

        public static bool IsPathInWhiteList(string path)
        {
            if (whiteUrlPrefixList.Any(t => path.StartsWith(t))
                || proxyUrlPrefixList.Any(t => path.StartsWith(t)))
                return true;
            if (whiteUrlList.Contains(path)
                || proxyUrlList.Contains(path))
                return true;
            return false;
        }

        private const string SESSION_IS_LOGIN = nameof(SESSION_IS_LOGIN);
        public async Task Invoke(HttpContext context)
        {
            var req = context.Request;
            var rep = context.Response;
            var path = req.Path.Value;
            if (path == "/")
            {
                var html = Program.Config.DefaultHtml;
                if (!string.IsNullOrEmpty(html))
                {
                    await rep.WriteAsync(html);
                    return;
                }
            }
            var isMainPageVisit = path == "/" || path == "/Index";

            //允许跨域访问
            rep.Headers.AccessControlAllowOrigin = new Microsoft.Extensions.Primitives.StringValues("*");
            rep.Headers.AccessControlAllowMethods = new Microsoft.Extensions.Primitives.StringValues("*");
            rep.Headers.AccessControlAllowHeaders = new Microsoft.Extensions.Primitives.StringValues("*");
            rep.Headers.AccessControlMaxAge = new Microsoft.Extensions.Primitives.StringValues("86400");

            //如果在白名单中，则放行
            if (IsPathInWhiteList(path))
            {
                await _next.Invoke(context);
                return;
            }
            //验证Session中是否已经登录，如果已经登录，则放行
            if (!string.IsNullOrEmpty(context.Session.GetString(SESSION_IS_LOGIN)))
            {
                await _next.Invoke(context);
                return;
            }
            //验证AccessToken
            if (ValidateAccessToken(context))
            {
                context.Session.SetString(SESSION_IS_LOGIN, true.ToString());
                await _next.Invoke(context);
                return;
            }
            //如果是API访问
            if (!isMainPageVisit)
            {
                rep.StatusCode = 401;
                rep.ContentType = "text/plain;charset=utf-8";
                await rep.WriteAsync($"没有权限查看[{path}]", Encoding.UTF8);
                return;
            }
            //否则跳转到登录页面
            rep.Redirect("./Login");
        }
    }
}
