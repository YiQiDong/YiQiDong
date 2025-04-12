using Quick.Blazor.Bootstrap;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using YiQiDong.Core.Utils;
using YiQiDong.Model;
using YiQiDong.Utils;

namespace YiQiDong.Components.Pages
{
    public partial class WebFileTransferManage
    {
        public const string DATA_FILE = "web_file_transfer_config.json";
        private WebFileTransferConfig config;
        private ModalAlert modalAlert;
        private ModalLoading modalLoading;

        [JsonSerializable(typeof(WebFileTransferConfig))]
        [JsonSourceGenerationOptions(WriteIndented = true)]
        internal partial class WebFileTransferConfigSerializerContext : JsonSerializerContext { }
        private static WebFileTransferConfig GetConfig()
        {
            var dataFile = FolderUtils.GetPathUnderDataDir(DATA_FILE);
            if (File.Exists(dataFile))
            {
                var content = File.ReadAllText(dataFile);
                return JsonSerializer.Deserialize(content, WebFileTransferConfigSerializerContext.Default.WebFileTransferConfig);
            }
            return new WebFileTransferConfig();
        }

        private static void SetConfig(WebFileTransferConfig config)
        {
            var dataFile = FolderUtils.GetPathUnderDataDir(DATA_FILE);
            File.WriteAllText(dataFile, JsonSerializer.Serialize(config, WebFileTransferConfigSerializerContext.Default.WebFileTransferConfig));
        }

        public static void Init()
        {
            var config = GetConfig();
            Quick.WebFileTransfer.Server.WebFileTransferController.Init(config.BaseFolder, config.Token);
        }

        protected override void OnInitialized()
        {
            config = GetConfig();
        }

        private void ModifyConfigModel()
        {
            try
            {
                modalLoading.Show("正在保存配置...", null, true);
                SetConfig(config);
                Init();
                modalAlert.Show("成功", "保存配置成功");
            }
            catch (Exception ex)
            {
                modalAlert.Show("失败", "保存配置失败，原因：" + ExceptionUtils.GetExceptionMessage(ex));
            }
            finally
            {
                modalLoading.Close();
            }
        }
    }
}
