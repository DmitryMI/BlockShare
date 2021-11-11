using BlockShare.BlockSharing.NetworkStatistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands
{
    public class SetBlockCommand : BlockShareCommand
    {
        public string Path { get; set; }
        public long BlockIndex { get; set; }
        public byte[] Block { get; set; }

        public override BlockShareCommandType CommandType => BlockShareCommandType.SetBlock;

        public SetBlockCommand()
        {
        }

        public SetBlockCommand(string path, long blockIndex, byte[] block)
        {
            Path = path;
            BlockIndex = blockIndex;
            Block = block;            
        }

        public override void WriteValuesToClient(TcpClient tcpClient, NetStat netStat)
        {
            //NetUtils.WriteBytesFixed(Block, 0, (int)Preferences.BlockSize, tcpClient, netStat);

            NetUtils.WriteString(Path, tcpClient, netStat);
            NetUtils.WriteLong(BlockIndex, tcpClient, netStat);
            NetUtils.WriteBytes(Block, tcpClient, netStat);

            netStat.Payload += (ulong)Preferences.BlockSize;
        }

        protected override void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            //Block = NetUtils.ReadBytesFixed((int)Preferences.BlockSize, tcpClient, netStat, timeout);
            Path = NetUtils.ReadString(tcpClient, netStat, timeout);
            BlockIndex = NetUtils.ReadLong(tcpClient, netStat, timeout);
            Block = NetUtils.ReadBytes(tcpClient, netStat, timeout);
            netStat.Payload += (ulong)Block.Length;
        }

        public override string ToString()
        {
            return $"SetBlock(Path: {Path}, BlockIndex: {BlockIndex}, Block: {Utils.PrintHex(Block, 0, 16)}...)";
        }
    }
}
