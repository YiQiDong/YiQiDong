using System.Text.Json.Serialization;

namespace YiQiDong.Cluster.Protocol.QpCommands;

[JsonSerializable(typeof(CreateCluster.Request))]
[JsonSerializable(typeof(CreateCluster.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class CreateClusterCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(DeleteCluster.Request))]
[JsonSerializable(typeof(DeleteCluster.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class DeleteClusterCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(GetContainerList.Request))]
[JsonSerializable(typeof(GetContainerList.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class GetContainerListCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(StartCluster.Request))]
[JsonSerializable(typeof(StartCluster.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class StartClusterCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(StopCluster.Request))]
[JsonSerializable(typeof(StopCluster.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class StopClusterCommandSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(UpdateConfig.Request))]
[JsonSerializable(typeof(UpdateConfig.Response))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class UpdateConfigCommandSerializerContext : JsonSerializerContext { }