using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands
{
    public class GetDirectoryDigestCommand : BlockShareCommand
    {
        public string Path { get; set; }
        public int RecursionLevel { get; set; }

        public GetDirectoryDigestCommand()
        {
            CommandType = BlockShareCommandType.GetDirectoryDigest;
        }


        public override void WriteValuesToClient(TcpClient tcpClient, NetStat netStat)
        {
            WriteString(Path, tcpClient, netStat);
            WriteInt(RecursionLevel, tcpClient, netStat);
        }

        protected override void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            Path = ReadString(tcpClient, netStat, timeout);
            RecursionLevel = ReadInt(tcpClient, netStat, timeout);
        }
    }
}
