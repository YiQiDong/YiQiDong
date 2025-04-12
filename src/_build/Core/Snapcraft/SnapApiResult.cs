using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace _build.Core.Snapcraft
{
    public class SnapApiResult
    {
        [JsonPropertyName("channel-map")]
        public SnapChannelMap[] channel_map { get; set; }
    }
}
