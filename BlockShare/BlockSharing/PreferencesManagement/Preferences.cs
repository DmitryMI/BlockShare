using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockShare.BlockSharing.BlockShareTypes;
using BlockShare.BlockSharing.HashMapping;

namespace BlockShare.BlockSharing.PreferencesManagement
{
    public class Preferences
    {
        [PreferenceParameter(IsRequired = true)]
        [CommandLineAlias('m', "mode")]
        public ModeOfOperation Mode { get; set; }

        [PreferenceParameter(IsRequired = false)]
        [CommandLineAlias('g', "gui")]
        public bool EnableGui { get; set; } = false;

        [PreferenceParameter(IsRequired = false)]
        public int HashSize { get; set; } = 32;

        [PreferenceParameter(IsRequired = false)]
        public long BlockSize { get; set; } = 16 * 1024 * 1024;

        [PreferenceParameter(IsRequired = true)]
        [CommandLineAlias('i', "ip")]
        public string ServerIp { get; set; } = "127.0.0.1";

        [PreferenceParameter(IsRequired = false)]
        [CommandLineAlias('p', "port")]
        public int ServerPort { get; set; } = 9652;

        [PreferenceParameter(IsRequired = true)]
        [CommandLineAlias('s', "storage")]
        public string ServerStoragePath { get; set; } = "ServerStorage";

        [PreferenceParameter(IsRequired = true)]
        [CommandLineAlias('s', "storage")]
        public string ClientStoragePath { get; set; } = "ClientStorage";

        [PreferenceParameter(IsRequired = false)]
        [CommandLineAlias("startup")]
        public string ClientStartupPath { get; set; } = ".\\";

        [PreferenceParameter(IsRequired = false)]
        public bool CreateMissingStorageDirectories { get; set; } = true;

        [PreferenceParameter(IsRequired = false)]
        public bool ClientBlockVerificationEnabled { get; set; } = false;

        [PreferenceParameter(IsRequired = false)]
        public bool UseHashLists { get; set; } = false;

#if ENSURE_SECURITY
        [PreferenceParameter(IsRequired = true)]
#else
        [PreferenceParameter(IsRequired = false)]
#endif
        public SecurityPreferences SecurityPreferences { get; set; } = new SecurityPreferences(SecurityMethod.Tls)
        {
            CertificateAuthorityPath = ".security/ca.crt",
            ServerCertificatePath = ".security/server.crt",
            ClientCertificatePath = ".security/client.crt",
            AcceptedCertificatesDirectoryPath = ".security/accepted-certificates"
        };

#if DEBUG
        [PreferenceParameter(IsRequired = false)]
        public int Verbosity { get; set; } = 3;
#else
        [PreferenceParameter(IsRequired = false)]
        public int Verbosity { get; set; } = 0;
#endif

        [PreferenceParameter(IsRequired = false)]
        public int BrowserRecursionLevel { get; set; } = Int32.MaxValue;

        [PreferenceParameter(IsRequired = false)]
        public static string HashlistExtension { get; set; } = ".hashlist";
        [PreferenceParameter(IsRequired = false)]
        public static string HashpartExtension { get; set; } = ".hashpart";

        [PreferenceParameter(IsRequired = false)]
        public HashMapper HashMapper { get; set; } = new ShaHashMapper(".hashparts", ".hashlists");

        [PreferenceParameter(IsRequired = false)]
        public string StorageMappingFile { get; set; } = null;

        [PreferenceParameter(IsRequired = false)]
        public bool UseDigestCache { get; set; } = false;

    }
}
