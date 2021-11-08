using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes
{
    public interface INetSerializable
    {
        void WriteToClient(TcpClient tcpClient, NetStat netStat);

        void ReadFromClient(TcpClient tcpClient, NetStat netStat, long timeout);
    }
}
