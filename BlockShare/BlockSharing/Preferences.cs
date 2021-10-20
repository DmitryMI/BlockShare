using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockShare.BlockSharing.HashMapping;

namespace BlockShare.BlockSharing
{
    public class Preferences
    {
        public const int HashSize = 32;

        public int GetHashSize() => HashSize;
        public long BlockSize { get; set; } = 16 * 1024 * 1024;

        public string ServerStoragePath { get; set; } = "";
        public string ClientStoragePath { get; set; } = "";

        public bool ClientBlockVerificationEnabled { get; set; } = true;

        public int Verbosity { get; set; } = 0;

        public static string HashlistExtension { get; set; } = ".hashlist";
        public static string HashpartExtension { get; set; } = ".hashpart";

        public HashMapper HashMapper { get; set; } = new ExtensionHashMapper();
    }
}
