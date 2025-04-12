using Quick.Blazor.Bootstrap;
using Quick.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YiQiDong.Core;

namespace YiQiDong.Components.Pages
{
    public partial class TagManage
    {
        private ModalAlert modalAlert;
        private ModalPrompt modalPrompt;

        private void CreateTag()
        {
            modalPrompt.Show("创建标签", null, t =>
              {
                  var newGroupName = t?.Trim();
                  if (string.IsNullOrEmpty(newGroupName))
                  {
                      modalAlert.Show("创建标签失败", "未输入标签名称！");
                      return;
                  }
                  if (TagManager.Instance.Contains(newGroupName))
                  {
                      modalAlert.Show("创建标签失败", "输入的标签名已经存在！");
                      return;
                  }
                  try
                  {
                      TagManager.Instance.Add(newGroupName);
                  }
                  catch (Exception ex)
                  {
                      modalAlert.Show("创建标签时出错", ExceptionUtils.GetExceptionMessage(ex));
                  }
                  InvokeAsync(StateHasChanged);
              }, null);
        }

        private void DeleteTag(string tag)
        {
            modalAlert.Show("删除确认", $"确定要删除标签[{tag}]？", () =>
              {
                  try
                  {
                      TagManager.Instance.Delete(tag);
                      InvokeAsync(StateHasChanged);
                  }
                  catch (Exception ex)
                  {
                      Task.Delay(100).ContinueWith(t =>
                      {
                          modalAlert.Show("删除标签时出错", ExceptionUtils.GetExceptionMessage(ex));
                      });
                  }
              });
        }
    }
}
