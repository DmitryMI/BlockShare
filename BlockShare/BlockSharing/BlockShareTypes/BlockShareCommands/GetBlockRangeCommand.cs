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
    public class GetBlockRangeCommand : BlockShareCommand
    {
        public string Path { get; set; }
        public long BlockIndex { get; set; }
        public long BlocksCount { get; set; }

        public override BlockShareCommandType CommandType => BlockShareCommandType.GetBlockRange;

        public GetBlockRangeCommand()
        {
        }

        public GetBlockRangeCommand(string path, long blockIndex, long blocksCount)
        {
            Path = path;
            BlockIndex = blockIndex;
            BlocksCount = blocksCount;
        }

        public override void WriteValuesToClient(Stream networkStream, NetStat netStat)
        {
            NetUtils.WriteString(Path, networkStream, netStat);
            NetUtils.WriteLong(BlockIndex, networkStream, netStat);
            NetUtils.WriteLong(BlocksCount, networkStream, netStat);
        }

        protected override void ReadValuesFromClient(Stream networkStream, NetStat netStat, long timeout)
        {
            Path = NetUtils.ReadString(networkStream, netStat, timeout);
            BlockIndex = NetUtils.ReadLong(networkStream, netStat, timeout);
            BlocksCount = NetUtils.ReadLong(networkStream, netStat, timeout);
        }

        public override string ToString()
        {
            return $"GetBlockRange(Path: {Path}, Blocks: [{BlockIndex}]-[{BlockIndex + BlocksCount - 1}])";
        }
    }
}
