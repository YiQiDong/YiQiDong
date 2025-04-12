using Quick.Fields;
using System;
using System.Collections.Generic;
using YiQiDong.Core;
using YiQiDong.Core.Utils;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Agent.AgentTypes.TextConfigs.Functions;

internal class EnvironmentConfigFunction : AbstractFunction
{
    public override string Name => "环境变量";
    private const string CUSTOM_ENVIRONMENT_LIST = nameof(CUSTOM_ENVIRONMENT_LIST);
    private const string CUSTOM_ENVIRONMENT_LIST_BTN_ADD = nameof(CUSTOM_ENVIRONMENT_LIST_BTN_ADD);
    private const string CUSTOM_ENVIRONMENT_LIST_BTN_EDIT = nameof(CUSTOM_ENVIRONMENT_LIST_BTN_EDIT);
    private const string CUSTOM_ENVIRONMENT_LIST_BTN_DELETE = nameof(CUSTOM_ENVIRONMENT_LIST_BTN_DELETE);
    private const string CUSTOM_ENVIRONMENT_LIST_FUNC_ADD = nameof(CUSTOM_ENVIRONMENT_LIST_FUNC_ADD);
    private const string CUSTOM_ENVIRONMENT_LIST_FUNC_EDIT = nameof(CUSTOM_ENVIRONMENT_LIST_FUNC_EDIT);
    private const string CUSTOM_ENVIRONMENT_LIST_FUNC_DELETE = nameof(CUSTOM_ENVIRONMENT_LIST_FUNC_DELETE);


    private AgentType agentType;
    private ContainerConfigModel configModel;
    private Action onChanged;

    public EnvironmentConfigFunction(AgentType agentType, Action onChanged)
    {
        this.agentType = agentType;
        this.onChanged = onChanged;
    }

    //获取容器环境变量
    private FieldForGet getContainerEnviromentVarsGroup(FunctionRequest request, bool isReadOnly)
    {
        var list = new List<FieldForGet>();
        //容器环境变量
        foreach (var item in agentType.processEnviromentsDictionary)
        {
            var evi = agentType.GetEnvironmentVariableInfo(item.Key);
            list.Add(new FieldForGet()
            {
                Name = evi?.Name ?? item.Key,
                Description = evi?.Description,
                Type = FieldType.InputText,
                Value = item.Value,
                Input_ReadOnly = true
            });
        }
        return new FieldForGet()
        {
            Type = FieldType.ContainerGroup,
            Name = "容器环境变量",
            Children = list.ToArray()
        };
    }

    private void saveCustomEnviroment()
    {
        ConfigFileProcessor.Default.Save(configModel, ContainerConfigModelSerializerContext.Default.ContainerConfigModel);
        agentType.configModel = configModel;
        onChanged?.Invoke();
    }

    public FieldForGet getCustomEnviromentGroup(FunctionRequest request, bool isReadOnly)
    {
        string preEnviromentKey = null;
        string enviromentKey = null;
        string enviromentValue = null;
        var isFirstEdit = true;

        var list = new List<FieldForGet>();
        //执行添加
        if (request != null && request.IsFieldIdsMatch(CUSTOM_ENVIRONMENT_LIST, CUSTOM_ENVIRONMENT_LIST_FUNC_ADD))
        {
            enviromentKey = request.GetFieldValue(CUSTOM_ENVIRONMENT_LIST, nameof(enviromentKey));
            enviromentValue = request.GetFieldValue(CUSTOM_ENVIRONMENT_LIST, nameof(enviromentValue));
            try
            {
                if (string.IsNullOrEmpty(enviromentKey))
                    throw new ArgumentException($"请输入环境变量名称！");
                configModel.Environment[enviromentKey] = enviromentValue;
                saveCustomEnviroment();
                list.Add(new()
                {
                    Name = "信息",
                    Description = $"添加环境变量[{enviromentKey}]成功！",
                    Type = FieldType.MessageBox,
                    Input_ReadOnly = true
                });
            }
            catch (Exception ex)
            {
                list.Add(new()
                {
                    Name = "错误",
                    Description = $"添加子进程函数时出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}",
                    Type = FieldType.MessageBox,
                    MessageBox_UsePreTag = true,
                    Input_ReadOnly = true
                });
                request.FieldIds = [CUSTOM_ENVIRONMENT_LIST, CUSTOM_ENVIRONMENT_LIST_BTN_ADD];
            }
        }
        //执行编辑
        else if (request != null && request.IsFieldIdsMatch(CUSTOM_ENVIRONMENT_LIST, CUSTOM_ENVIRONMENT_LIST_FUNC_EDIT))
        {
            preEnviromentKey = request.GetFieldValue(CUSTOM_ENVIRONMENT_LIST, nameof(preEnviromentKey));
            enviromentKey = request.GetFieldValue(CUSTOM_ENVIRONMENT_LIST, nameof(enviromentKey));
            enviromentValue = request.GetFieldValue(CUSTOM_ENVIRONMENT_LIST, nameof(enviromentValue));
            try
            {
                if (string.IsNullOrEmpty(enviromentKey))
                    throw new ArgumentException($"请输入环境变量名称！");

                if (preEnviromentKey != enviromentKey && configModel.Environment.ContainsKey(preEnviromentKey))
                    configModel.Environment.Remove(preEnviromentKey);
                configModel.Environment[enviromentKey] = enviromentValue;
                saveCustomEnviroment();
                list.Add(new()
                {
                    Name = "信息",
                    Description = $"编辑子进程环境变量[{enviromentKey}]成功！",
                    Type = FieldType.MessageBox,
                    Input_ReadOnly = true
                });
            }
            catch (Exception ex)
            {
                list.Add(new()
                {
                    Name = "错误",
                    Description = $"编辑子进程环境变量[{enviromentKey}]时出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}",
                    Type = FieldType.MessageBox,
                    MessageBox_UsePreTag = true,
                    Input_ReadOnly = true
                });
                isFirstEdit = false;
                request.FieldIds = [CUSTOM_ENVIRONMENT_LIST, preEnviromentKey, CUSTOM_ENVIRONMENT_LIST_BTN_EDIT];
            }
        }
        //点击“删除”按钮
        else if (request != null && request.IsFieldIdsMatch(CUSTOM_ENVIRONMENT_LIST, "*", CUSTOM_ENVIRONMENT_LIST_BTN_DELETE))
        {
            enviromentKey = request.FieldIds[1];
            list.Add(new()
            {
                Id = enviromentKey,
                Type = FieldType.ContainerRow,
                Children = [
                    new(){
                Id = CUSTOM_ENVIRONMENT_LIST_FUNC_DELETE,
                Name = "删除",
                Description = $"确定要删除子进程环境变量[{enviromentKey}]?",
                Type = FieldType.MessageBox,
                PostOnChanged = true,
                MessageBox_CanCancel =true
            }]
            }
            );
        }
        //执行删除
        else if (request != null && request.IsFieldIdsMatch(CUSTOM_ENVIRONMENT_LIST, "*", CUSTOM_ENVIRONMENT_LIST_FUNC_DELETE))
        {
            enviromentKey = request.FieldIds[1];
            var messageBoxResult = request.GetFieldValue(request.FieldIds);
            if (messageBoxResult == FieldForGet.MESSAGEBOX_VALUE_OK)
            {
                try
                {
                    if (configModel.Environment.ContainsKey(enviromentKey))
                        configModel.Environment.Remove(enviromentKey);
                    saveCustomEnviroment();
                    list.Add(new()
                    {
                        Name = "信息",
                        Description = $"删除子进程环境变量[{enviromentKey}]成功！",
                        Type = FieldType.MessageBox,
                        Input_ReadOnly = true
                    });
                }
                catch (Exception ex)
                {
                    list.Add(new()
                    {
                        Name = "错误",
                        Description = $"删除子进程环境变量[{enviromentKey}]时出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}",
                        Type = FieldType.MessageBox,
                        Input_ReadOnly = true
                    });
                }
            }
        }
        //点击“添加”按钮
        if (request != null && request.IsFieldIdsMatch(CUSTOM_ENVIRONMENT_LIST, CUSTOM_ENVIRONMENT_LIST_BTN_ADD))
        {
            list.Add(new()
            {
                Type = FieldType.ContainerGroup,
                Name = "添加子进程环境变量",
                Children = [
                    new()
                {
                    Id = nameof(enviromentKey),
                    Name = "环境变量名称",
                    Type = FieldType.InputText,
                    Value = enviromentKey
                },
                new()
                {
                    Id = nameof(enviromentValue),
                    Name = "环境变量值",
                    Type = FieldType.InputText,
                    Value = enviromentValue
                },
                new()
                {
                    Id = CUSTOM_ENVIRONMENT_LIST_FUNC_ADD,
                    Name = "确定",
                    Type = FieldType.Button,
                    Theme = FieldTheme.Primary
                },
                new ()
                {
                    Name = "取消",
                    Type = FieldType.Button,
                    MarginLeft = 1
                }
                ]
            });
        }
        //点击“编辑”按钮
        else if (request != null && request.IsFieldIdsMatch(CUSTOM_ENVIRONMENT_LIST, "*", CUSTOM_ENVIRONMENT_LIST_BTN_EDIT))
        {
            preEnviromentKey = request.FieldIds[1];
            try
            {
                if (isFirstEdit)
                {
                    enviromentKey = preEnviromentKey;
                    configModel.Environment.TryGetValue(preEnviromentKey, out enviromentValue);
                }
                list.Add(new()
                {
                    Type = FieldType.ContainerGroup,
                    Name = "编辑子进程环境变量",
                    Children = [
                        new()
                    {
                        Id = nameof(preEnviromentKey),
                        Type = FieldType.ContainerRow,
                        Value = preEnviromentKey
                    },
                    new()
                    {
                        Id = nameof(enviromentKey),
                        Name = "环境变量名称",
                        Type = FieldType.InputText,
                        Value = enviromentKey
                    },
                    new()
                    {
                        Id = nameof(enviromentValue),
                        Name = "环境变量值",
                        Type = FieldType.InputText,
                        Value = enviromentValue
                    },
                    new()
                    {
                        Id = CUSTOM_ENVIRONMENT_LIST_FUNC_EDIT,
                        Name = "确定",
                        Type = FieldType.Button,
                        Theme = FieldTheme.Primary
                    },
                    new ()
                    {
                        Name = "取消",
                        Type = FieldType.Button,
                        MarginLeft = 1
                    }
                    ]
                });
            }
            catch (Exception ex)
            {
                list.Add(new()
                {
                    Name = "错误",
                    Description = $"编辑子进程环境变量[{enviromentKey}]时出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}",
                    Type = FieldType.MessageBox,
                    Input_ReadOnly = true
                });
            }
        }
        else
        {
            if (!isReadOnly)
            {
                list.Add(new()
                {
                    Id = CUSTOM_ENVIRONMENT_LIST_BTN_ADD,
                    Name = "添加",
                    Type = FieldType.Button,
                    Theme = FieldTheme.Primary,
                    MarginBottom = 1
                });
            }
            if (configModel.Environment != null)
            {
                foreach (var item in configModel.Environment)
                {
                    list.Add(new()
                    {
                        Id = item.Key,
                        Name = item.Key,
                        Description = agentType.GetEnvironmentVariableInfo(item.Key)?.Description,
                        Type = FieldType.InputText,
                        Value = item.Value,
                        Input_ReadOnly = true,
                        Input_AppendChildren = isReadOnly ? null : [
                            new (){ Id = CUSTOM_ENVIRONMENT_LIST_BTN_EDIT, Type = FieldType.Button, Name = "编辑", Input_IsSmall = true},
                            new (){ Id = CUSTOM_ENVIRONMENT_LIST_BTN_DELETE, Type = FieldType.Button, Name = "删除", Input_IsSmall = true, Theme = FieldTheme.Danger}
                        ]
                    });
                }
            }
        }
        return new()
        {
            Id = CUSTOM_ENVIRONMENT_LIST,
            Name = "子进程环境变量",
            Type = FieldType.ContainerGroup,
            Children = list.ToArray()
        };
    }


    private FieldForGet[] innerGet(FunctionRequest request, bool isReadOnly)
    {
        return [
           new FieldForGet()
            {
                Type = FieldType.ContainerTab,
                Children = [
                    getContainerEnviromentVarsGroup(request,isReadOnly),
                    getCustomEnviromentGroup(request,isReadOnly)
                ]
            }
        ];
    }

    public override FieldForGet[] Get()
    {
        configModel = agentType.configModel.Clone();
        var isReadOnly = AgentContext.Container.AutoStart;
        return innerGet(null, isReadOnly);
    }

    public override FieldForGet[] Post(FunctionRequest request)
    {
        var isReadOnly = AgentContext.Container.AutoStart;
        return innerGet(request, isReadOnly);
    }
}
