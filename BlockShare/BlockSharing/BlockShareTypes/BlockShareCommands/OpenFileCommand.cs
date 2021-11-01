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

        public override BlockShareCommandType CommandType => BlockShareCommandType.OpenFile;

        public OpenFileCommand()
        {
        }

        public OpenFileCommand(string path)
        {
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

        public override string ToString()
        {
            return $"OpenFile(Path: {Path})";
        }
    }
}
