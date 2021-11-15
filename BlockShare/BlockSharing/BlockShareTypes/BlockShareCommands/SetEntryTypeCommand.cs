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
    public class SetEntryTypeCommand : BlockShareCommand
    {
        public FileSystemEntryType EntryType { get; set; }

        public override BlockShareCommandType CommandType => BlockShareCommandType.SetEntryType;

        public SetEntryTypeCommand(FileSystemEntryType entryType)
        {
            EntryType = entryType;
        }

        public SetEntryTypeCommand()
        {
        }

        protected static FileSystemEntryType ReadEntryType(Stream networkStream, NetStat netStat, long timeout)
        {
            byte[] valueBytes = new byte[1];
            Utils.ReadPackage(networkStream, valueBytes, 0, valueBytes.Length, timeout);
            netStat.TotalReceived += (ulong)valueBytes.Length;
            FileSystemEntryType result = (FileSystemEntryType)valueBytes[0];
            return result;
        }

        protected static void WriteEntryType(FileSystemEntryType value, Stream networkStream, NetStat netStat)
        {
            byte[] valueBytes = new byte[] { (byte)value };
            networkStream.Write(valueBytes, 0, valueBytes.Length);
            netStat.TotalSent += (ulong)valueBytes.Length;
        }

        public override void WriteValuesToClient(Stream networkStream, NetStat netStat)
        {
            WriteEntryType(EntryType, networkStream, netStat);
        }

        protected override void ReadValuesFromClient(Stream networkStream, NetStat netStat, long timeout)
        {
            EntryType = ReadEntryType(networkStream, netStat, timeout);
        }

        public override string ToString()
        {
            return $"SetEntryType(EntryType: {EntryType})";
        }
    }
}
