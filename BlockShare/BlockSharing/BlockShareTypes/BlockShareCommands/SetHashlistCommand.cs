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

        public override BlockShareCommandType CommandType => BlockShareCommandType.SetHashlist;

        public SetHashlistCommand()
        {

        }
        public override void WriteValuesToClient(TcpClient tcpClient, NetStat netStat)
        {
            NetUtils.WriteLong(FileLength, tcpClient, netStat);
            NetUtils.WriteBytes(HashlistSerialized, tcpClient, netStat);
        }

        protected override void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            FileLength = NetUtils.ReadLong(tcpClient, netStat, timeout);
            HashlistSerialized = NetUtils.ReadBytes(tcpClient, netStat, timeout);
        }

        public override string ToString()
        {
            return $"SetHashlist(FileLength: {FileLength}, HashlistSerialized: {Utils.PrintHex(HashlistSerialized, 0, 16)}...)";
        }
    }
}
