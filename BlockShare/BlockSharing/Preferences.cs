using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing
{
    public class Preferences
    {
        public const int HashSize = 32;

        public long BlockSize { get; set; } = 16 * 1024 * 1024;

        public string ServerStoragePath { get; set; } = "";
        public string ClientStoragePath { get; set; } = "";

        public bool ClientBlockVerificationEnabled { get; set; } = false;
    }
}
