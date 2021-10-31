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

        public GetBlockRangeCommand()
        {
            CommandType = BlockShareCommandType.GetBlockRange;
        }

        public GetBlockRangeCommand(string path, long blockIndex, long blocksCount)
        {
            CommandType = BlockShareCommandType.GetBlockRange;
            Path = path;
            BlockIndex = blockIndex;
            BlocksCount = blocksCount;
        }

        public override void WriteValuesToClient(TcpClient tcpClient, NetStat netStat)
        {
            WriteString(Path, tcpClient, netStat);
            WriteLong(BlockIndex, tcpClient, netStat);
            WriteLong(BlocksCount, tcpClient, netStat);
        }

        protected override void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            Path = ReadString(tcpClient, netStat, timeout);
            BlockIndex = ReadLong(tcpClient, netStat, timeout);
            BlocksCount = ReadLong(tcpClient, netStat, timeout);
        }

        public override string ToString()
        {
            return $"GetBlockRange(Path: {Path}, Blocks: [{BlockIndex}]-[{BlockIndex + BlocksCount - 1}])";
        }
    }
}
