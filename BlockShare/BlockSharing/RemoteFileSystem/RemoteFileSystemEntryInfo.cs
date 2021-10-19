using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.RemoteFileSystem
{
    public class RemoteFileSystemEntryInfo
    {
        public string RemoteFullPath { get; private set; }
        public string Name { get; private set; }
        public RemoteDirectoryInfo Parent { get; private set; }

        public RemoteFileSystemEntryInfo(string fullPath, string name, RemoteDirectoryInfo parent)
        {
            RemoteFullPath = fullPath;
            Name = name;
            Parent = parent;
        }
    }
}
