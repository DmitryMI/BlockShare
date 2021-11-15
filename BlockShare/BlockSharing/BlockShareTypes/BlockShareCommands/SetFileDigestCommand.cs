using BlockShare.BlockSharing.DirectoryDigesting;
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
    public class SetFileDigestCommand : BlockShareCommand
    {
        public FileDigest FileDigest { get; set; }

        public override BlockShareCommandType CommandType => BlockShareCommandType.SetFileDigest;

        public override void WriteValuesToClient(Stream networkStream, NetStat netStat)
        {
            NetUtils.WriteNetSerializable(FileDigest, networkStream, netStat);
        }

        protected override void ReadValuesFromClient(Stream networkStream, NetStat netStat, long timeout)
        {
            FileDigest = new FileDigest();
            NetUtils.ReadNetSerializable(FileDigest, networkStream, netStat, timeout);
        }

        public override string ToString()
        {
            return $"SetFileDigest(FileDigest: {FileDigest})";
        }
    }
}
