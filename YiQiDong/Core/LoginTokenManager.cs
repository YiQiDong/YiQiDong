namespace YiQiDong.Core;

public class LoginTokenManager
{
    public static LoginTokenManager Instance { get; } = new LoginTokenManager();
    private class TokenContext
    {
        public string Token { get; set; }
        public int UsingPages { get; set; }
        public DateTime LastChangeTime { get; set; }
    }
    private TimeSpan tokenTimeout = TimeSpan.FromMinutes(10);
    private TimeSpan maxTokenTimeout = TimeSpan.FromDays(1);
    private Dictionary<string, TokenContext> tokenContextDict;
    private LoginTokenManager()
    {
        tokenContextDict = new Dictionary<string, TokenContext>();
        _ = checkAsync();
    }

    public void UsingToken(string loginToken)
    {
        lock (tokenContextDict)
        {
            if (!tokenContextDict.TryGetValue(loginToken, out var tokenContext))
            {
                tokenContext = new TokenContext() { Token = loginToken };
                tokenContextDict[loginToken] = tokenContext;
            }
            tokenContext.UsingPages++;
            tokenContext.LastChangeTime = DateTime.Now;
        }
    }

    public void UnusingToken(string loginToken)
    {
        lock (tokenContextDict)
        {
            if (!tokenContextDict.TryGetValue(loginToken, out var tokenContext))
                return;
            tokenContext.UsingPages--;
            tokenContext.LastChangeTime = DateTime.Now;
        }
    }

    private async Task checkAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            lock (tokenContextDict)
            {
                var tokenContexts = tokenContextDict.Values.ToArray();
                foreach (var tokenContext in tokenContexts)
                {
                    var time = DateTime.Now - tokenContext.LastChangeTime;
                    //如果超过超时时间，则移除
                    if (time > maxTokenTimeout || (tokenContext.UsingPages <= 0 && time > tokenTimeout))
                        tokenContextDict.Remove(tokenContext.Token);
                }
            }
        }
    }

    /// <summary>
    /// 验证Token有效性
    /// </summary>
    /// <param name="loginToken"></param>
    /// <returns></returns>
    public bool Verify(string loginToken)
    {
        if (string.IsNullOrEmpty(loginToken))
            return false;
        lock (tokenContextDict)
            return tokenContextDict.ContainsKey(loginToken);
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    /// <param name="loginToken"></param>
    public void Logout(string loginToken)
    {
        if (string.IsNullOrEmpty(loginToken))
            return;
        lock (tokenContextDict)
        {
            if (tokenContextDict.ContainsKey(loginToken))
                tokenContextDict.Remove(loginToken);
        }
    }
}
