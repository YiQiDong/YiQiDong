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
            "channel": {
                "architecture": "armhf", 
                "name": "beta", 
                "released-at": "2024-03-02T06:34:45.702519+00:00", 
                "risk": "beta", 
                "track": "latest"
            }, 
            "created-at": "2024-03-02T06:34:20.559773+00:00", 
            "download": {
                "deltas": [ ], 
                "sha3-384": "df1ccae931b36144f8b7cc55373ea8d64cf304ac3a0117b1422bdd1c0b4df6ad5f0041f0cd273a2adffcd88c26053675", 
                "size": 144633856, 
                "url": "https://api.snapcraft.io/api/v1/snaps/download/XKEcBqPM06H1Z7zGOdG5fbICuf8NWK5R_2778.snap"
            }, 
            "revision": 2778, 
            "type": "app", 
            "version": "123.0.6312.22"
        }
     */
    public class SnapChannelMap
    {
        public SnapChannelInfo channel { get; set; }
        [JsonPropertyName("created-at")]
        public DateTime created_at { get; set; }
        public SnapDownloadInfo download { get; set; }
        public int revision { get; set; }
        public string type { get; set; }
        public string version { get; set; }
    }
}
