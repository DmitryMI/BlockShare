using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.RemoteFileSystem
{
    public class RemoteFileInfo : RemoteFileSystemEntryInfo
    {
        public RemoteFileInfo(string fullPath, string name, RemoteDirectoryInfo parent) : base(fullPath, name, parent)
        {
        }
    }
}
