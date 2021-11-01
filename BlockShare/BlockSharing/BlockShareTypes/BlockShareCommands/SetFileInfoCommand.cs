using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands
{
    public class SetFileInfoCommand : BlockShareCommand
    {
        public long FileLength { get; set; }

        public override BlockShareCommandType CommandType => BlockShareCommandType.SetFileInfo;

        public override void WriteValuesToClient(TcpClient tcpClient, NetStat netStat)
        {
            WriteLong(FileLength, tcpClient, netStat);
        }

        protected override void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            FileLength = ReadLong(tcpClient, netStat, timeout);
        }

        public override string ToString()
        {
            return $"SetFileInfo(FileLength: {FileLength})";
        }
    }
}
