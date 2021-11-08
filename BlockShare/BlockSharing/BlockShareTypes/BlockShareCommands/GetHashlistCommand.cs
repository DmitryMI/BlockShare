using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands
{
    public class GetHashlistCommand : BlockShareCommand
    {
        public string Path { get; private set; }

        public override BlockShareCommandType CommandType => BlockShareCommandType.GetHashList;

        public GetHashlistCommand()
        {

        }

        public GetHashlistCommand(string path)
        {
            Path = path;
        }

        public override void WriteValuesToClient(TcpClient tcpClient, NetStat netStat)
        {
            NetUtils.WriteString(Path, tcpClient, netStat);
        }

        protected override void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            Path = NetUtils.ReadString(tcpClient, netStat, timeout);
        }

        public override string ToString()
        {
            return $"GetHashList(Path: {Path})";
        }
    }
}
