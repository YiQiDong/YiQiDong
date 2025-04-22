using Blazor.ECharts.Options;
using Blazor.ECharts.Options.Enum;
using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using YiQiDong.Utils;

namespace YiQiDong.Components.Pages
{
    public partial class Home : IDisposable
    {
        private ModalWindow modalWindow;
        private ModalLoading modalLoading;
        private ModalAlert modalAlert;
        private SystemInfoContext systemInfoContext;
        private EChartsOption<Blazor.ECharts.Options.Series.Line.Line> cpuChartsOption;
        private EChartsOption<Blazor.ECharts.Options.Series.Line.Line> memoryChartsOption;

        [Parameter]
        public IPageNavigater IPageNavigater { get; set; }

        private void processEChartsOption(EChartsOption<Blazor.ECharts.Options.Series.Line.Line> options)
        {
            options.Animation = false;
            options.Grid = new List<Grid>() { new Grid() { Left = "60", Right = "10", Top = "20", Bottom = "20" } };
            options.Tooltip = new Tooltip()
            {
                Trigger = TooltipTrigger.Axis,
                AxisPointer = new TooltipAxisPointer() { Type = AxisPointerType.Shadow }
            };
            options.Toolbox = new Toolbox() { Show = true };
            options.XAxis = new List<XAxis>() { new XAxis() { Type = AxisType.Time } };
        }

        private Blazor.ECharts.Options.Series.Line.Line createSeries(string name, object data)
        {
            return new Blazor.ECharts.Options.Series.Line.Line()
            {
                Name = name,
                Data = data,
                AreaStyle = new AreaStyle(),
                ShowSymbol = false
            };
        }

        private SeriesBase cpuSeries;
        private SeriesBase memorySeries;

        protected override void OnInitialized()
        {
            systemInfoContext = Program.SystemInfoContext;
            cpuSeries=createSeries("CPU利用率(%)", systemInfoContext.CpuChartsData);
            cpuChartsOption = new EChartsOption<Blazor.ECharts.Options.Series.Line.Line>()
            {
                YAxis = new List<YAxis>()
                {
                    new YAxis()
                    {
                        Type = AxisType.Value,
                        SplitNumber = 1,
                        AxisLabel= new AxisLabel(){ Formatter="{value} %" },
                    }
                },
                Series = new List<object>() { cpuSeries }
            };
            processEChartsOption(cpuChartsOption);

            memorySeries = createSeries($"已使用({systemInfoContext.MemoryUnit}B)", systemInfoContext.MemoryUsedChartsData);
            memoryChartsOption = new EChartsOption<Blazor.ECharts.Options.Series.Line.Line>()
            {
                YAxis = new List<YAxis>()
                            {
                                new YAxis()
                                {
                                    Type = AxisType.Value,
                                    SplitNumber = 1,
                                    AxisLabel= new AxisLabel(){ Formatter="{value} " + systemInfoContext.MemoryUnit+"B" },
                                    Max = systemInfoContext.MemoryTotalInUnit
                                }
                            },
                Series = new List<object>() { memorySeries }
            };
            processEChartsOption(memoryChartsOption);
            systemInfoContext.DataChanged += SystemInfoContext_DataChanged;
        }

        private void SystemInfoContext_DataChanged(object sender, EventArgs e)
        {
            InvokeAsync(StateHasChanged).Wait();
        }

        private void showFolder(string path)
        {
            modalWindow.Show<Controls.FileManageControl>("文件管理", new Dictionary<string, object>()
            {
                [nameof(Controls.FileManageControl.Dir)] = path
            });
        }

        private void showProcessView(int pid)
        {
            modalWindow.Show<Quick.Blazor.Bootstrap.Admin.ProcessViewControl>(
                $"进程[{pid}]",
                Quick.Blazor.Bootstrap.Admin.ProcessViewControl.PrepareParameters(pid, null));
        }

        private void showProcessManage()
        {
            modalWindow.Show<Quick.Blazor.Bootstrap.Admin.ProcessManageControl>("进程管理");

        }

        public void Dispose()
        {
            systemInfoContext.DataChanged -= SystemInfoContext_DataChanged;
        }
    }
}
