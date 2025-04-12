using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace _build.Core.Snapcraft
{
    /*
    {
        "architecture": "armhf", 
        "name": "beta", 
        "released-at": "2024-03-02T06:34:45.702519+00:00", 
        "risk": "beta", 
        "track": "latest"
    }
    */
    public class SnapChannelInfo
    {
        public string architecture { get; set; }
        public string name { get; set; }
        [JsonPropertyName("released-at")]
        public string released_at { get; set; }
        public string risk { get; set; }
        public string track { get; set; }
    }
}
