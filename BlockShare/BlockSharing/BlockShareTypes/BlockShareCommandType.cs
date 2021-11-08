using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes
{
    public enum BlockShareCommandType : byte
    {
        // Client to Server    
        GetEntryType = 0xA0,
        GetDirectoryDigest,
        GetHashList,
        GetBlockRange,
        Disconnect,
        OpenFile,
        GetFileDigest,

        // Server to Client
        Ok,
        InvalidOperation,
        SetDirectoryDigest,
        SetHashlist,
        SetEntryType,
        SetBlock,
        SetFileDigest,
    }
}
