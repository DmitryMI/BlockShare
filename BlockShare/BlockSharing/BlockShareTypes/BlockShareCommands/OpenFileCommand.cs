using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands
{
    public class OpenFileCommand : BlockShareCommand
    {
        public string Path { get; set; }

        public OpenFileCommand()
        {
            CommandType = BlockShareCommandType.OpenFile;
        }
        public OpenFileCommand(string path)
        {
            CommandType = BlockShareCommandType.OpenFile;
            Path = path;
        }

        public override void WriteValuesToClient(TcpClient tcpClient, NetStat netStat)
        {
            WriteString(Path, tcpClient, netStat);
        }

        protected override void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            Path = ReadString(tcpClient, netStat, timeout);
        }
    }
}
