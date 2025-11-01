using System.Text.Json.Serialization;

namespace YiQiDong.Protocol.V1.QpNotices;

[JsonSerializable(typeof(ContainerInitedNotice))]
[JsonSerializable(typeof(ContainerLogNotice))]
[JsonSerializable(typeof(ContainerStartedNotice))]
[JsonSerializable(typeof(ContainerStopedNotice))]
[JsonSerializable(typeof(FunctionListChangedNotice))]
[JsonSerializable(typeof(FunctionSessionChangedNotice))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class NoticesSerializerContext : JsonSerializerContext { }