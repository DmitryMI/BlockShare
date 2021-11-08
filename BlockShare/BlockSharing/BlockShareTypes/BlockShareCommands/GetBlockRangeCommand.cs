using System;
using System.Collections.Generic;
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

        public override void WriteValuesToClient(TcpClient tcpClient, NetStat netStat)
        {
            NetUtils.WriteString(Path, tcpClient, netStat);
            NetUtils.WriteLong(BlockIndex, tcpClient, netStat);
            NetUtils.WriteLong(BlocksCount, tcpClient, netStat);
        }

        protected override void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            Path = NetUtils.ReadString(tcpClient, netStat, timeout);
            BlockIndex = NetUtils.ReadLong(tcpClient, netStat, timeout);
            BlocksCount = NetUtils.ReadLong(tcpClient, netStat, timeout);
        }

        public override string ToString()
        {
            return $"GetBlockRange(Path: {Path}, Blocks: [{BlockIndex}]-[{BlockIndex + BlocksCount - 1}])";
        }
    }
}
