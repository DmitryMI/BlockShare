using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.DirectoryDigesting
{
    public abstract class EntryDigest
    {
        public long Size { get; protected set; }
        public string RelativePath { get; protected set; }
        public string Name { get; protected set; }
        public DateTime UpdateTime { get; protected set; }

        public abstract bool IsDirectory { get; }
    }
}
