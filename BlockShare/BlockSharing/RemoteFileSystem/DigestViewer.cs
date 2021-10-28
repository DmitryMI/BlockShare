using BlockShare.BlockSharing.DirectoryDigesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.RemoteFileSystem
{
    public class DigestViewer
    {
        private DirectoryDigest root;
        
        public DirectoryDigest Current { get; private set; }

        public DigestViewer(DirectoryDigest root, BlockShareClient client)
        {
            this.root = root;
        }
    }
}
