using BlockShare.BlockSharing.DirectoryDigesting;
using System;
using System.Collections.Generic;
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

        public override void WriteValuesToClient(TcpClient tcpClient, NetStat netStat)
        {
            NetUtils.WriteNetSerializable(FileDigest, tcpClient, netStat);
        }

        protected override void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            FileDigest = new FileDigest();
            NetUtils.ReadNetSerializable(FileDigest, tcpClient, netStat, timeout);
        }

        public override string ToString()
        {
            return $"SetFileDigest(FileDigest: {FileDigest})";
        }
    }
}
