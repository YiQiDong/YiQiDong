using System.Text.Json.Serialization;

namespace YiQiDong.Protocol.V1.QpCommands;

[JsonSerializable(typeof(AddReverseProxyRule.Request))]
[JsonSerializable(typeof(AddReverseProxyRule.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class AddReverseProxyRuleCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(ExecuteFunction.Request))]
[JsonSerializable(typeof(ExecuteFunction.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class ExecuteFunctionCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(Exit.Request))]
[JsonSerializable(typeof(Exit.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class ExitCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(GetFunctionList.Request))]
[JsonSerializable(typeof(GetFunctionList.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class GetFunctionListCommandSerializerContext : JsonSerializerContext { }


[JsonSerializable(typeof(GetConfigFileList.Request))]
[JsonSerializable(typeof(GetConfigFileList.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class GetConfigFileListCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(Register.Request))]
[JsonSerializable(typeof(Register.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class RegisterCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(Start.Request))]
[JsonSerializable(typeof(Start.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class StartCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(Stop.Request))]
[JsonSerializable(typeof(Stop.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class StopCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(Using.Request))]
[JsonSerializable(typeof(Using.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class UsingCommandSerializerContext : JsonSerializerContext { }