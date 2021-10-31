using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands
{
    public class SetHashlistCommand : BlockShareCommand
    {
        public long FileLength { get; set; }
        public byte[] HashlistSerialized { get; set; }

        public SetHashlistCommand()
        {
            CommandType = BlockShareCommandType.SetHashlist;
        }
        public override void WriteValuesToClient(TcpClient tcpClient, NetStat netStat)
        {
            WriteLong(FileLength, tcpClient, netStat);
            WriteBytes(HashlistSerialized, tcpClient, netStat);
        }

        protected override void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            FileLength = ReadLong(tcpClient, netStat, timeout);
            HashlistSerialized = ReadBytes(tcpClient, netStat, timeout);
        }

        public override string ToString()
        {
            return $"SetHashlist(FileLength: {FileLength}, HashlistSerialized: {Utils.PrintHex(HashlistSerialized, 0, 16)}...)";
        }
    }
}
