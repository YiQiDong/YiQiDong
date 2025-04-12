using System.ComponentModel;

namespace YiQiDong.Controllers
{
    public class FileController
    {
        public void Use(WebApplication app)
        {
            var routeGroup = app.MapGroup("/api/file");
            routeGroup.MapGet("/Download", DownloadFile);
        }

        private static string DownloadPath;
        public static void SetDownloadPath(string path)
        {
            DownloadPath = path;
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="Path">路径</param>
        public IResult DownloadFile(HttpContext httpContext)
        {
            var path = DownloadPath;
            DownloadPath = null;
            if (string.IsNullOrEmpty(path))
                return Results.NotFound();
            if (!System.IO.File.Exists(path))
                return Results.NotFound();
            var rep = httpContext.Response;
            rep.Headers["Expires"] = "0";
            rep.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, post-check=0, pre-check=0";
            rep.Headers["Pragma"] = "no-cache";
            return Results.File(path, "application/octet-stream", System.IO.Path.GetFileName(path));
        }
    }
}
