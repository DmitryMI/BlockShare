using BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes
{
    public abstract class BlockShareCommand
    {
        public BlockShareCommandType CommandType { get; protected set; }

        public class CommandTypeNotRecognizedException : Exception
        {

        }

        protected abstract void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout);

        public abstract void WriteValuesToClient(TcpClient tcpClient, NetStat netStat);
                

        protected static int ReadInt(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] valueBytes = new byte[sizeof(int)];
            Utils.ReadPackage(tcpClient, networkStream, valueBytes, 0, valueBytes.Length, timeout);
            netStat.TotalReceived += (ulong)valueBytes.Length;
            int value = BitConverter.ToInt32(valueBytes, 0);
            return value;
        }
        protected static long ReadLong(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] valueBytes = new byte[sizeof(long)];
            Utils.ReadPackage(tcpClient, networkStream, valueBytes, 0, valueBytes.Length, timeout);
            netStat.TotalReceived += (ulong)valueBytes.Length;
            long value = BitConverter.ToInt64(valueBytes, 0);
            return value;
        }        

        protected static void WriteLong(long value, TcpClient tcpClient, NetStat netStat)
        {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] valueBytes = BitConverter.GetBytes(value);
            networkStream.Write(valueBytes, 0, valueBytes.Length);
            netStat.TotalSent += (ulong)valueBytes.Length;            
        }

        protected static void WriteBytes(byte[] valueBytes, TcpClient tcpClient, NetStat netStat)
        {
            NetworkStream networkStream = tcpClient.GetStream();

            byte[] valueLengthBytes = BitConverter.GetBytes(valueBytes.Length);

            networkStream.Write(valueLengthBytes, 0, valueLengthBytes.Length);
            netStat.TotalSent += (ulong)valueLengthBytes.Length;

            networkStream.Write(valueBytes, 0, valueBytes.Length);
            netStat.TotalSent += (ulong)valueBytes.Length;
        }

        protected static byte[] ReadBytes(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            NetworkStream networkStream = tcpClient.GetStream();

            byte[] valueLengthBytes = new byte[sizeof(int)];

            Utils.ReadPackage(tcpClient, networkStream, valueLengthBytes, 0, sizeof(int), timeout);
            netStat.TotalReceived += sizeof(int);

            int valueLength = BitConverter.ToInt32(valueLengthBytes, 0);
            byte[] valueBytes = new byte[valueLength];
            Utils.ReadPackage(tcpClient, networkStream, valueBytes, 0, valueLength, timeout);
            netStat.TotalReceived += (ulong)valueLength;

            return valueBytes;
        }

        protected static void WriteInt(int value, TcpClient tcpClient, NetStat netStat)
        {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] valueBytes = BitConverter.GetBytes(value);
            networkStream.Write(valueBytes, 0, valueBytes.Length);
            netStat.TotalSent += (ulong)valueBytes.Length;
        }

        protected static void WriteString(string value, TcpClient tcpClient, NetStat netStat)
        {
            byte[] valueBytes = Encoding.UTF8.GetBytes(value);

            WriteBytes(valueBytes, tcpClient, netStat);
        }

        protected static string ReadString(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            byte[] valueBytes = ReadBytes(tcpClient, netStat, timeout);

            string value = Encoding.UTF8.GetString(valueBytes);
            return value;
        }

        public static void WriteToClient(BlockShareCommand command, TcpClient tcpClient, NetStat netStat)
        {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] commandTypeBytes = new byte[1];
            commandTypeBytes[0] = (byte)command.CommandType;
            command.WriteValuesToClient(tcpClient, netStat);
        }

        public static BlockShareCommand ReadFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] commandTypeBytes = new byte[1];
            Utils.ReadPackage(tcpClient, networkStream, commandTypeBytes, 0, commandTypeBytes.Length, timeout);
            BlockShareCommandType commandType = (BlockShareCommandType)commandTypeBytes[0];

            BlockShareCommand command = null;
            switch (commandType)
            {
                case BlockShareCommandType.GetEntryType:
                    command = new GetEntryTypeCommand();
                    break;

                case BlockShareCommandType.GetDirectoryDigest:
                    command = new GetDirectoryDigestCommand();
                    break;

                case BlockShareCommandType.GetHashList:
                    command = new GetEntryTypeCommand();
                    break;

                case BlockShareCommandType.GetBlockRange:
                    command = new GetBlockRangeCommand();
                    break;

                case BlockShareCommandType.Disconnect:
                    command = new DisconnectCommand();
                    break;

                case BlockShareCommandType.Ok:
                    command = new OkCommand();
                    break;

                case BlockShareCommandType.InvalidOperation:
                    command = new InvalidOperationCommand();
                    break;

                case BlockShareCommandType.SetDirectoryDigest:
                    command = new SetDirectoryDigestCommand();
                    break;

                case BlockShareCommandType.SetHashlist:
                    command = new SetHashlistCommand();
                    break;
            }

            if(command == null)
            {
                throw new CommandTypeNotRecognizedException();
            }

            command.ReadValuesFromClient(tcpClient, netStat, timeout);

            return command;
        }
    }
}
