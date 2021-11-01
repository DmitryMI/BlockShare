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

        public string ServerIp { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 9652;        

        public string ServerStoragePath { get; set; } = "";
        public string ClientStoragePath { get; set; } = "";

        public bool CreateMissingStorageDirectories { get; set; } = true;

        public bool ClientBlockVerificationEnabled { get; set; } = false;

#if DEBUG
        public int Verbosity { get; set; } = 3;
#else
        public int Verbosity { get; set; } = 0;
#endif

        public int BrowserRecursionLevel { get; set; } = 1;

        public static string HashlistExtension { get; set; } = ".hashlist";
        public static string HashpartExtension { get; set; } = ".hashpart";

        public HashMapper HashMapper { get; set; } = new ExtensionHashMapper();
        
    }
}
