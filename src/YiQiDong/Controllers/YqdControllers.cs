using YiQiDong.Controllers;

namespace Microsoft.AspNetCore.Builder;

public static class YqdControllers
{
    public static void UseYiQiDongControllers(this WebApplication app)
    {
        new FileController().Use(app);
        new ContainerController().Use(app);
        new VeritasController().Use(app);
    }
}