using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _build.Core.Snapcraft
{
    /*
     {
        "deltas": [ ], 
        "sha3-384": "df1ccae931b36144f8b7cc55373ea8d64cf304ac3a0117b1422bdd1c0b4df6ad5f0041f0cd273a2adffcd88c26053675", 
        "size": 144633856, 
        "url": "https://api.snapcraft.io/api/v1/snaps/download/XKEcBqPM06H1Z7zGOdG5fbICuf8NWK5R_2778.snap"
    }
     */
    public class SnapDownloadInfo
    {
        public string url { get; set; }
        public long size { get; set; }
    }
}
