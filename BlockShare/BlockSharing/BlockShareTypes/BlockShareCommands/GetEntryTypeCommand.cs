using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands
{
    public class GetEntryTypeCommand : BlockShareCommand
    {
        public string Path { get; private set; }

        public GetEntryTypeCommand()
        {
            CommandType = BlockShareCommandType.GetEntryType;
        }

        public GetEntryTypeCommand(string path)
        {
            CommandType = BlockShareCommandType.GetEntryType;
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
            return $"GetEntryType(Path: {Path})";
        }
    }
}
