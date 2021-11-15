using BlockShare.BlockSharing.NetworkStatistics;
using System;
using System.Collections.Generic;
using System.IO;
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
        public override void WriteValuesToClient(Stream networkStream, NetStat netStat)
        {
            NetUtils.WriteLong(FileLength, networkStream, netStat);
            NetUtils.WriteBytes(HashlistSerialized, networkStream, netStat);
        }

        protected override void ReadValuesFromClient(Stream networkStream, NetStat netStat, long timeout)
        {
            FileLength = NetUtils.ReadLong(networkStream, netStat, timeout);
            HashlistSerialized = NetUtils.ReadBytes(networkStream, netStat, timeout);
        }

        public override string ToString()
        {
            return $"SetHashlist(FileLength: {FileLength}, HashlistSerialized: {Utils.PrintHex(HashlistSerialized, 0, 16)}...)";
        }
    }
}
