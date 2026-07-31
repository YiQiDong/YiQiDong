using System.Text.Json.Serialization;

namespace YiQiDong.Protocol.V1.QpCommands;

[JsonSerializable(typeof(AddReverseProxyRule.Request))]
[JsonSerializable(typeof(AddReverseProxyRule.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class AddReverseProxyRuleCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(ExecuteFunction.Request))]
[JsonSerializable(typeof(ExecuteFunction.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class ExecuteFunctionCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(Exit.Request))]
[JsonSerializable(typeof(Exit.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class ExitCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(GetFunctionList.Request))]
[JsonSerializable(typeof(GetFunctionList.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class GetFunctionListCommandSerializerContext : JsonSerializerContext { }


[JsonSerializable(typeof(GetConfigFileList.Request))]
[JsonSerializable(typeof(GetConfigFileList.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class GetConfigFileListCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(Register.Request))]
[JsonSerializable(typeof(Register.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class RegisterCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(Start.Request))]
[JsonSerializable(typeof(Start.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class StartCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(Stop.Request))]
[JsonSerializable(typeof(Stop.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class StopCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(GetThreadList.Request))]
[JsonSerializable(typeof(GetThreadList.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class UsingCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(OpenFunctionSession.Request))]
[JsonSerializable(typeof(OpenFunctionSession.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class OpenFunctionSessionSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(CloseFunctionSession.Request))]
[JsonSerializable(typeof(CloseFunctionSession.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class CloseFunctionSessionSerializerContext : JsonSerializerContext { }