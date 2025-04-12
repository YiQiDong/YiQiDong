using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using Quick.Fields;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using YiQiDong.Core;
using YiQiDong.Core.Utils;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Components.Controls;

public partial class ContainerFunctionControl
{
    [Parameter]
    public ContainerContext Container { get; set; }
    [Parameter]
    public FunctionInfo Function { get; set; }

    private bool isExecuting = false;
    private ModalLoading modalLoading;
    private Quick.Blazor.Bootstrap.QuickFields.Controls controls;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
            executeFunction();
    }

    private void executeFunction(FieldForGet field = null, FieldForGet[] fieldForGetArray = null)
    {
        if (isExecuting)
            return;
        isExecuting = true;
        InvokeAsync(StateHasChanged);

        var fields = fieldForGetArray?.Select(t => t.ToPost()).ToArray();
        var message = "正在处理...";
        if (field != null)
        {
            message = $"正在处理: {field.Name}";
        }
        var executeFunctionTask = Task.Run(() =>
        {
            try
            {
                var ret = Container.ExecuteFunction(Function, field?.GetFullFieldIds(), fields);
                controls.SetFields(ret);
            }
            catch (Exception ex)
            {
                controls.SetFields(
                [
                    new FieldForGet()
                    {
                        Type = FieldType.Alert,
                        Name = "错误",
                        Theme = FieldTheme.Danger,
                        Description=ExceptionUtils.GetExceptionString(ex)
                    }
                ]);
            }
            InvokeAsync(StateHasChanged);
            isExecuting = false;
        });
        Task.Run(async () =>
        {
            var isLoadingShowed = false;
            var timeout = Function.ExecuteTimeout;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (isExecuting)
            {
                await Task.Delay(1000);
                if (!isExecuting)
                    break;
                var usedTimeSpan = stopwatch.Elapsed;
                var usedTime = Convert.ToInt32(usedTimeSpan.TotalMilliseconds);
                if (!isLoadingShowed)
                {
                    modalLoading.Show($"{Container.ContainerInfo.Name} - {Function.Name}", message, false, null);
                    isLoadingShowed = true;
                }
                usedTimeSpan = TimeSpan.FromSeconds(Convert.ToInt32(usedTimeSpan.TotalSeconds));
                modalLoading.UpdateProgress(usedTime * 100 / timeout, $"已用时间：{usedTimeSpan}");
            }
            stopwatch.Stop();
            if (isLoadingShowed)
                modalLoading.Close();
        });
    }

    private void OnFieldChanged(FieldForGet field, FieldForGet[] fields)
    {
        executeFunction(field, fields);
    }
}