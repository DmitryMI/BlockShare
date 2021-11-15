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
    public class GetDirectoryDigestCommand : BlockShareCommand
    {
        public string Path { get; set; }
        public int RecursionLevel { get; set; }

        public override BlockShareCommandType CommandType => BlockShareCommandType.GetDirectoryDigest;

        public GetDirectoryDigestCommand()
        {

        }

        public GetDirectoryDigestCommand(string path, int recursionLevel)
        {
            Path = path;
            RecursionLevel = recursionLevel;
        }


        public override void WriteValuesToClient(Stream networkStream, NetStat netStat)
        {
            NetUtils.WriteString(Path, networkStream, netStat);
            NetUtils.WriteInt(RecursionLevel, networkStream, netStat);
        }

        protected override void ReadValuesFromClient(Stream networkStream, NetStat netStat, long timeout)
        {
            Path = NetUtils.ReadString(networkStream, netStat, timeout);
            RecursionLevel = NetUtils.ReadInt(networkStream, netStat, timeout);
        }

        public override string ToString()
        {
            return $"GetDirectoryDigest(Path: {Path}, RecursionLevel: {RecursionLevel})";
        }
    }
}
