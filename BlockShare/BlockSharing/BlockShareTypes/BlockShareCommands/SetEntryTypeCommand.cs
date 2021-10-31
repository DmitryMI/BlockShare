using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands
{
    public class SetEntryTypeCommand : BlockShareCommand
    {
        public FileSystemEntryType EntryType { get; set; }

        public SetEntryTypeCommand(FileSystemEntryType entryType)
        {
            CommandType = BlockShareCommandType.SetEntryType;
            EntryType = entryType;
        }

        public SetEntryTypeCommand()
        {
            CommandType = BlockShareCommandType.SetEntryType;
        }

        protected static FileSystemEntryType ReadEntryType(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] valueBytes = new byte[1];
            Utils.ReadPackage(tcpClient, networkStream, valueBytes, 0, valueBytes.Length, timeout);
            netStat.TotalReceived += (ulong)valueBytes.Length;
            FileSystemEntryType result = (FileSystemEntryType)valueBytes[0];
            return result;
        }

        protected static void WriteEntryType(FileSystemEntryType value, TcpClient tcpClient, NetStat netStat)
        {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] valueBytes = new byte[] { (byte)value };
            networkStream.Write(valueBytes, 0, valueBytes.Length);
            netStat.TotalSent += (ulong)valueBytes.Length;
        }

        public override void WriteValuesToClient(TcpClient tcpClient, NetStat netStat)
        {
            WriteEntryType(EntryType, tcpClient, netStat);
        }

        protected override void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            EntryType = ReadEntryType(tcpClient, netStat, timeout);
        }

        public override string ToString()
        {
            return $"SetEntryType(EntryType: {EntryType})";
        }
    }
}
