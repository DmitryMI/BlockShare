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
    public class GetFileDigestCommand : BlockShareCommand
    {
        public string Path { get; set; }

        public override BlockShareCommandType CommandType => BlockShareCommandType.GetFileDigest;

        public GetFileDigestCommand()
        {
            
        }

        public GetFileDigestCommand(string path)
        {            
            Path = path;
        }

        public override void WriteValuesToClient(Stream networkStream, NetStat netStat)
        {
            NetUtils.WriteString(Path, networkStream, netStat);
        }

        protected override void ReadValuesFromClient(Stream networkStream, NetStat netStat, long timeout)
        {
            Path = NetUtils.ReadString(networkStream, netStat, timeout);
        }

        public override string ToString()
        {
            return $"GetFileInfo(Path: {Path})";
        }
    }
}
