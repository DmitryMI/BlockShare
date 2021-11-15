using BlockShare.BlockSharing.NetworkStatistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes
{
    public interface INetSerializable
    {
        void WriteToClient(Stream networkStream, NetStat netStat);

        void ReadFromClient(Stream networkStream, NetStat netStat, long timeout);
    }
}
