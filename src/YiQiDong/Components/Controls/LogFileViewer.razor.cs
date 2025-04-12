using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YiQiDong.Core.Utils;
using YiQiDong.Utils;

namespace YiQiDong.Components.Controls
{
    public partial class LogFileViewer : IDisposable
    {
        [Parameter]
        public string LogFile { get; set; }

        private string[] LogFileLines;

        private int LogWindowHeight = 500;
        private int _PageSize = 10;
        
        public int PageSize
        {
            get => _PageSize;
            set
            {
                _PageSize = value;
                Offset = 0;
            }
        }
        private int _Offset = 0;
        public int Offset
        {
            get => _Offset;
            set
            {
                _Offset = value;
                _ = search();
            }
        }
        private ModalLoading modalLoading;
        private ModalAlert modalAlert;

        private string searchKeywords;
        private string[] displayLines;
        private int totalCount;
        private bool isLogFileLoaded = false;
        private static UnitStringConverting storageUSC = UnitStringConverting.StorageUnitStringConverting;

        private void btnLoad_Clicked()
        {
            if (string.IsNullOrEmpty(LogFile))
                return;
            var fileInfo = new FileInfo(LogFile);
            modalAlert.Show("确认加载日志文件", $"日志文件[{Path.GetFileName(LogFile)}]大小为[{storageUSC.GetString(fileInfo.Length, 1, true)}B]，确认将日志文件加载到内存中？", async () =>
            {
                var cts = new CancellationTokenSource();
                modalLoading.Show("加载日志文件", "正在打开日志文件...", true, () => cts.Cancel());
                try
                {
                    List<string> lineList = new List<string>();
                    using (var fs = File.Open(LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var fileSize = fs.Length;
                        using (var reader = new StreamReader(fs, true))
                        {
                            while (!cts.IsCancellationRequested && !reader.EndOfStream)
                            {
                                var line = await reader.ReadLineAsync();
                                var position = fs.Position;
                                modalLoading.UpdateProgress(Convert.ToInt32(position * 100 / fileSize), $"读取中[{position}/{fileSize}]...");
                                lineList.Add(line);
                            }
                        }
                    }
                    LogFileLines = lineList.ToArray();
                    modalLoading.Close();
                    isLogFileLoaded = true;
                    await search();
                }
                catch (Exception ex)
                {
                    modalAlert.Show("错误", $"加载日志文件[{Path.GetFileName(LogFile)}]时失败，原因：{ExceptionUtils.GetExceptionString(ex)}");
                    modalLoading.Close();
                }
            });
        }

        private void btnSearch_Clicked()
        {
            Offset = 0;
        }

        private async Task search()
        {
            var cts = new CancellationTokenSource();
            modalLoading.Show("查询", "正在查询日志文件...", true, () => cts.Cancel());

            List<string> lineList = new List<string>();
            var currentOffset = 0;
            var currentIndex = 0;

            for (var i = 0; i < LogFileLines.Length; i++)
            {
                if (cts.IsCancellationRequested)
                    break;
                var line = LogFileLines[i];

                modalLoading.UpdateProgress(Convert.ToInt32(i * 100 / LogFileLines.Length), $"查询中[行：{i * 100}/{LogFileLines.Length}]...");
                var isMatch = string.IsNullOrEmpty(searchKeywords) || (line != null && line.Contains(searchKeywords));
                if (!isMatch)
                    continue;
                if (currentOffset < Offset)
                {
                    currentOffset++;
                }
                else if (lineList.Count < PageSize)
                {
                    lineList.Add(line);
                }
                currentIndex++;
            }
            totalCount = currentIndex;
            displayLines = lineList.ToArray();
            modalLoading.Close();
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            LogFileLines = null;
            displayLines = null;
            //做一次垃圾回收
            GC.Collect();
        }
    }
}
