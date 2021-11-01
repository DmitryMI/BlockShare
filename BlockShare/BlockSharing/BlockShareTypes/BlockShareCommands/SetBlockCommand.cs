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
        public byte[] Block { get; set; }

        public override BlockShareCommandType CommandType => BlockShareCommandType.SetBlock;

        public SetBlockCommand()
        {
        }

        public SetBlockCommand(byte[] block)
        {
            Block = block;            
        }

        public override void WriteValuesToClient(TcpClient tcpClient, NetStat netStat)
        {
            WriteBytesFixed(Block, 0, (int)Preferences.BlockSize, tcpClient, netStat);
            netStat.Payload += (ulong)Preferences.BlockSize;
        }

        protected override void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            Block = ReadBytesFixed((int)Preferences.BlockSize, tcpClient, netStat, timeout);
        }

        public override string ToString()
        {
            return $"SetBlock(Block: {Utils.PrintHex(Block, 0, 16)}...)";
        }
    }
}
