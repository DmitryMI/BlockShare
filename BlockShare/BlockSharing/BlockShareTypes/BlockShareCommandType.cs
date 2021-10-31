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
        GetEntryType,
        GetDirectoryDigest,
        GetHashList,
        GetBlockRange,
        Disconnect,
        OpenFile,

        // Server to Client
        Ok,
        InvalidOperation,
        SetDirectoryDigest,
        SetHashlist,
        SetEntryType,
    }
}
