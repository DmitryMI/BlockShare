using BlockShare.BlockSharing.NetworkStatistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes
{
    public static class NetUtils
    {
        public static void WriteNetSerializable(INetSerializable serialializable, Stream networkStream, NetStat netStat)
        {
            serialializable.WriteToClient(networkStream, netStat);
        }

        public static T ReadNetSerializable<T>(Stream networkStream, NetStat netStat, long timeout) where T : INetSerializable
        {
            T serializable = Activator.CreateInstance<T>();
            serializable.ReadFromClient(networkStream, netStat, timeout);
            return serializable;
        }

        public static void ReadNetSerializable(INetSerializable serializable,Stream networkStream, NetStat netStat, long timeout)
        {
            serializable.ReadFromClient(networkStream, netStat, timeout);
        }

        public static int ReadInt(Stream networkStream, NetStat netStat, long timeout)
        {
            byte[] valueBytes = new byte[sizeof(int)];
            Utils.ReadPackage(networkStream, valueBytes, 0, valueBytes.Length, timeout);
            netStat.TotalReceived += (ulong)valueBytes.Length;
            int value = BitConverter.ToInt32(valueBytes, 0);
            return value;
        }
        public static long ReadLong(Stream networkStream, NetStat netStat, long timeout)
        {
            byte[] valueBytes = new byte[sizeof(long)];
            Utils.ReadPackage(networkStream, valueBytes, 0, valueBytes.Length, timeout);
            netStat.TotalReceived += (ulong)valueBytes.Length;
            long value = BitConverter.ToInt64(valueBytes, 0);
            return value;
        }

        public static void WriteLong(long value,Stream networkStream, NetStat netStat)
        {
            byte[] valueBytes = BitConverter.GetBytes(value);
            networkStream.Write(valueBytes, 0, valueBytes.Length);
            netStat.TotalSent += (ulong)valueBytes.Length;
        }

        public static void WriteBytesFixed(byte[] valueBytes, int offset, int length,Stream networkStream, NetStat netStat)
        {
            networkStream.Write(valueBytes, offset, length);
            netStat.TotalSent += (ulong)valueBytes.Length;
        }

        public static byte[] ReadBytesFixed(int length,Stream networkStream, NetStat netStat, long timeout)
        {
            byte[] valueBytes = new byte[length];
            Utils.ReadPackage(networkStream, valueBytes, 0, length, timeout);
            netStat.TotalReceived += (ulong)length;

            return valueBytes;
        }

        public static void WriteBytes(byte[] valueBytes,Stream networkStream, NetStat netStat)
        {
            int length = valueBytes.Length;
            byte[] valueLengthBytes = BitConverter.GetBytes(length);

            networkStream.Write(valueLengthBytes, 0, valueLengthBytes.Length);
            //Console.WriteLine($"\t--> {Utils.PrintHex(valueLengthBytes, 0, valueLengthBytes.Length)}");
            netStat.TotalSent += (ulong)valueLengthBytes.Length;

            networkStream.Write(valueBytes, 0, length);
            //Console.WriteLine($"\t--> {Utils.PrintHex(valueBytes, 0, length)}");
            netStat.TotalSent += (ulong)valueBytes.Length;
        }

        public static byte[] ReadBytes(Stream networkStream, NetStat netStat, long timeout)
        {
            byte[] valueLengthBytes = new byte[sizeof(int)];

            Utils.ReadPackage(networkStream, valueLengthBytes, 0, sizeof(int), timeout);
            //Console.WriteLine($"\t<-- {Utils.PrintHex(valueLengthBytes, 0, valueLengthBytes.Length)}");
            netStat.TotalReceived += sizeof(int);

            int valueLength = BitConverter.ToInt32(valueLengthBytes, 0);
            byte[] valueBytes = new byte[valueLength];
            Utils.ReadPackage(networkStream, valueBytes, 0, valueLength, timeout);
            //Console.WriteLine($"\t<-- {Utils.PrintHex(valueBytes, 0, valueBytes.Length)}");
            netStat.TotalReceived += (ulong)valueLength;

            return valueBytes;
        }

        public static void WriteInt(int value,Stream networkStream, NetStat netStat)
        {
            byte[] valueBytes = BitConverter.GetBytes(value);
            networkStream.Write(valueBytes, 0, valueBytes.Length);
            netStat.TotalSent += (ulong)valueBytes.Length;
        }

        public static void WriteString(string value,Stream networkStream, NetStat netStat)
        {
            byte[] valueBytes;
            //if (String.IsNullOrEmpty(value))
            //{
            //valueBytes = new byte[] { (byte)'\0' };
            //}
            //else
            {
                valueBytes = Encoding.UTF8.GetBytes(value);
            }
            //Console.WriteLine($"\t--> '{value}'");
            WriteBytes(valueBytes, networkStream, netStat);
        }

        public static string ReadString(Stream networkStream, NetStat netStat, long timeout)
        {
            byte[] valueBytes = ReadBytes(networkStream, netStat, timeout);

            string value = Encoding.UTF8.GetString(valueBytes);
            //Console.WriteLine($"\t<-- '{value}'");
            return value;
        }
    }
}
