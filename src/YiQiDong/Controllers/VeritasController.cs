using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;

namespace YiQiDong.Controllers
{
    [DisplayName("Veritas相关接口")]
    public class VeritasController
    {
        public void Use(WebApplication app)
        {
            var routeGroup = app.MapGroup("/api/veritas");
            routeGroup.MapPost("/{containerId}/start", StartContainer);
            routeGroup.MapPost("/{containerId}/stop", StopContainer);
            routeGroup.MapPost("/{containerId}/restart", RestartContainer);
            routeGroup.MapPost("/{containerId}/status", GetContainerStatus);
        }

        /// <summary>
        /// 是否有权限
        /// </summary>
        private bool HasPermission(HttpContext httpContext)
        {
            //仅允许本地环回地址访问
            return IPAddress.IsLoopback(httpContext.Connection.RemoteIpAddress);
        }

        /// <summary>
        /// 成功
        /// </summary>
        /// <returns></returns>
        private IResult Success()
        {
            return Results.Ok("110");
        }

        private IResult Failed(string message)
        {
            //Response.Headers.Add("Yqd-Message", message);
            return Results.BadRequest("100");
        }

        /// <summary>
        /// 不允许访问
        /// </summary>
        /// <returns></returns>
        private IResult NotAllowed()
        {
            return Failed("You have no permission to access this resource.");
        }

        private IResult NotFoundContainer(string containerId)
        {
            return Failed($"Can't found container: {containerId}");
        }

        /// <summary>
        /// 启动容器
        /// </summary>
        /// <param name="containerId">容器编号</param>
        /// <returns></returns>
        public async Task<IResult> StartContainer(HttpContext httpContext, string containerId)
        {
            if (!HasPermission(httpContext))
                return NotAllowed();
            var container = Core.ContainerManager.Instance.Get(containerId);
            if (container == null)
                return NotFoundContainer(containerId);
            var containerInfo = container.ContainerInfo;
            if (containerInfo.AutoStart)
                return Failed($"{containerInfo.Name} is already started.");
            await container.Start();
            return Success();
        }

        /// <summary>
        /// 停止容器
        /// </summary>
        /// <param name="containerId">容器编号</param>
        /// <returns></returns>
        public async Task<IResult> StopContainer(HttpContext httpContext, string containerId)
        {
            if (!HasPermission(httpContext))
                return NotAllowed();
            var container = Core.ContainerManager.Instance.Get(containerId);
            if (container == null)
                return NotFoundContainer(containerId);
            var containerInfo = container.ContainerInfo;
            if (!containerInfo.AutoStart)
                return Failed($"{containerInfo.Name} is already stoped.");
            await container.Stop();
            return Success();
        }

        /// <summary>
        /// 重启容器
        /// </summary>
        /// <param name="containerId">容器编号</param>
        /// <returns></returns>
        public async Task<IResult> RestartContainer(HttpContext httpContext, string containerId)
        {
            if (!HasPermission(httpContext))
                return NotAllowed();
            var container = Core.ContainerManager.Instance.Get(containerId);
            if (container == null)
                return NotFoundContainer(containerId);
            await container.Stop();
            await Task.Delay(1000);
            await container.Start();
            return Success();
        }

        /// <summary>
        /// 获取容器状态
        /// </summary>
        /// <param name="containerId">容器编号</param>
        /// <returns>容器进程编号，如果没有容器进程，编号为-1</returns>
        public IResult GetContainerStatus(HttpContext httpContext, string containerId)
        {
            if (!HasPermission(httpContext))
                return NotAllowed();
            var container = Core.ContainerManager.Instance.Get(containerId);
            if (container == null)
                return NotFoundContainer(containerId);
            if (!container.ContainerInfo.AutoStart)
                return Failed($"Container[{containerId}] not start.");
            return Success();
        }
    }
}
