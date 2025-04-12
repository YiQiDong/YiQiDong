using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace _build.Core.Snapcraft
{
    public class SnapcraftClient
    {
        public HttpClient GetHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("YiQiDong", "1.0"));
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
            httpClient.DefaultRequestHeaders.ExpectContinue = false;
            httpClient.DefaultRequestHeaders.Add("Snap-Device-Series", "16");
            return httpClient;
        }

        public async Task<SnapApiResult> GetSnapInfoAsync(string name)
        {
            using (var httpClient = GetHttpClient())
            {
                return await httpClient.GetFromJsonAsync<SnapApiResult>(
                    $"http://api.snapcraft.io/v2/snaps/info/{name}");
            }
        }
    }
}
