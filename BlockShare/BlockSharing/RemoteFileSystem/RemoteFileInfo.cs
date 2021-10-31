using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockShare.BlockSharing.DirectoryDigesting;

namespace BlockShare.BlockSharing.RemoteFileSystem
{
    [Obsolete]
    public class RemoteFileInfo : RemoteFileSystemEntryInfo
    {
        private readonly FileDigest fileDigest;
        public long Size => fileDigest != null ? fileDigest.Size : 0;
        public RemoteFileInfo(string fullPath, string name, RemoteDirectoryInfo parent, FileDigest fileDigest) : base(fullPath, name, parent)
        {
            this.fileDigest = fileDigest;
        }
    }
}
