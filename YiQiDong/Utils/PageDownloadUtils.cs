using System.Net.Http.Headers;
using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.Admin.Core;
using Quick.Utils;

namespace YiQiDong.Utils
{
    public static class PageDownloadUtils
    {
        public static async Task<string> DownloadAsync(string url, ModalLoading modalLoading, ModalAlert modalAlert, CancellationTokenSource operateCts)
        {
            modalLoading?.Show("下载", "正在下载文件...", true, operateCts.Cancel);

            var tmpFile = Path.GetTempFileName();
            try
            {
                var handler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    }
                };
                using (var httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(nameof(YiQiDong), Consts.Version));
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                    httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                    httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                    httpClient.DefaultRequestHeaders.ExpectContinue = false;
                    using (var rep = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, operateCts.Token))
                    using (var fileStream = File.OpenWrite(tmpFile))
                    {
                        var contentLength = rep.Content.Headers.ContentLength;
                        using (var stream = await rep.Content.ReadAsStreamAsync())
                        {
                            if (contentLength == null)
                            {
                                await stream.CopyToAsync(fileStream);
                            }
                            else
                            {
                                using (var commonTransferContext = new CommonTransferContext(progressInfo =>
                                {
                                    modalLoading.UpdateProgress(progressInfo.Percent, progressInfo.Message);
                                }, contentLength.Value))
                                {
                                    modalLoading.UpdateContent(url);
                                    await commonTransferContext.TransferAsync(stream, fileStream, operateCts.Token);
                                }
                            }
                        }
                    }
                }
                return tmpFile;
            }
            catch (TaskCanceledException)
            {
                modalAlert?.Show("下载已取消", $"已取消下载文件: {url}");
                if (File.Exists(tmpFile))
                    File.Delete(tmpFile);
                return null;
            }
            catch (Exception ex)
            {
                modalAlert?.Show("下载失败", ExceptionUtils.GetExceptionString(ex));
                if (File.Exists(tmpFile))
                    File.Delete(tmpFile);
                return null;
            }
            finally
            {
                modalLoading?.Close();
            }
        }
    }
}
