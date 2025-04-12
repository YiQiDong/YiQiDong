namespace YiQiDong.Core;

public class AccessTokenManager
{
    public static AccessTokenManager Instance { get; } = new AccessTokenManager();
    private string innerAccessToken = null;

    /// <summary>
    /// 获取一个访问令牌
    /// </summary>
    /// <returns></returns>
    public string GetAccessToken()
    {
        innerAccessToken = Guid.NewGuid().ToString();
        return innerAccessToken;
    }

    /// <summary>
    /// 验证一个访问令牌
    /// </summary>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    public bool VerifyAccessToken(string accessToken)
    {
        if (string.IsNullOrEmpty(innerAccessToken))
            return false;
        var ret = innerAccessToken == accessToken;
        //如果验证成功，清空访问令牌
        if (ret)
            innerAccessToken = null;
        return ret;
    }
}
