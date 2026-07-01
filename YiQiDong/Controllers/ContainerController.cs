using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;

namespace YiQiDong.Controllers
{
    [DisplayName("容器相关")]
    public class ContainerController
    {
        public void Use(WebApplication app)
        {
            var routeGroup = app.MapGroup("/api/container");
            routeGroup.MapGet("/", GetContainerList);
            routeGroup.MapPost("/{containerId}/enable", EnableContainer);
            routeGroup.MapPost("/{containerId}/disable", DisableContainer);
            routeGroup.MapPost("/{containerId}/start", StartContainer);
            routeGroup.MapPost("/{containerId}/stop", StopContainer);
            routeGroup.MapPost("/{containerId}/restart", RestartContainer);
            routeGroup.MapPost("/{containerId}/info", GetContainerInfo);
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
        /// 不允许访问
        /// </summary>
        /// <returns></returns>
        private IResult NotAllowed()
        {
            return Results.BadRequest("没有权限访问此资源"); ;
        }

        /// <summary>
        /// 查询全部容器列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IResult GetContainerList(HttpContext httpContext)
        {
            if (!HasPermission(httpContext))
                return NotAllowed();
            return Results.Ok(Core.ContainerManager.Instance.GetAll().Select(t => t.ContainerInfo).ToArray());
        }

        /// <summary>
        /// 启用容器
        /// </summary>
        /// <param name="containerId">容器编号</param>
        /// <returns></returns>
        public IResult EnableContainer(HttpContext httpContext, string containerId)
        {
            if (!HasPermission(httpContext))
                return NotAllowed();
            var container = Core.ContainerManager.Instance.Get(containerId);
            if (container == null)
                return Results.NotFound();
            if (!container.ContainerInfo.Enable)
                container.Enable();
            return Results.Ok();
        }

        /// <summary>
        /// 禁用容器
        /// </summary>
        /// <param name="containerId">容器编号</param>
        /// <returns></returns>
        public IResult DisableContainer(HttpContext httpContext, string containerId)
        {
            if (!HasPermission(httpContext))
                return NotAllowed();
            var container = Core.ContainerManager.Instance.Get(containerId);
            if (container == null)
                return Results.NotFound();
            var containerInfo = container.ContainerInfo;
            if (containerInfo.Enable)
            {
                if (containerInfo.AutoStart)
                    return Results.BadRequest($"容器[{containerInfo.Name}]正在运行，先停止容器才能禁用容器！");
                container.Disable();
            }
            return Results.Ok();
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
                return Results.NotFound();
            var containerInfo = container.ContainerInfo;
            if (containerInfo.AutoStart)
                return Results.BadRequest($"容器[{containerInfo.Name}]已经处于启动状态！");
            await container.Start();
            return Results.Ok();
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
                return Results.NotFound();
            var containerInfo = container.ContainerInfo;
            if (!containerInfo.AutoStart)
                return Results.BadRequest($"容器[{containerInfo.Name}]已经处于停止状态！");
            await container.Stop();
            return Results.Ok();
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
                return Results.NotFound();
            await container.Stop();
            await Task.Delay(1000);
            await container.Start();
            return Results.Ok();
        }

        /// <summary>
        /// 获取容器信息
        /// </summary>
        /// <param name="containerId">容器编号</param>
        /// <returns>容器信息</returns>
        public IResult GetContainerInfo(HttpContext httpContext, string containerId)
        {
            if (!HasPermission(httpContext))
                return NotAllowed();
            var container = Core.ContainerManager.Instance.Get(containerId);
            if (container == null)
                return Results.NotFound();
            return Results.Ok(container.ContainerInfo);
        }
    }
}
