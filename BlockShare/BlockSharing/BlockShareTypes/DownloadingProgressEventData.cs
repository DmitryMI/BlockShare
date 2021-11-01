using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes
{
    public class DownloadingProgressEventData
    {
        public string FileName { get; set; }
        public FileHashList RemoteHashList { get; set; }
        public FileHashList LocalHashList { get; set; }
        public int BlocksCount { get; set; }
        public int DownloadedBlockIndex { get; set; }
    }
}
