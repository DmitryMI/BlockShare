using BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands;
using BlockShare.BlockSharing.PreferencesManagement;
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
        public abstract BlockShareCommandType CommandType { get; }

        public Preferences Preferences { get; set; }

        public class CommandTypeNotRecognizedException : Exception
        {
            public byte SerializedCommandType { get; }

            public CommandTypeNotRecognizedException(byte serializedValue) : base($"Unrecognized command type: {serializedValue}")
            {
                SerializedCommandType = serializedValue;
            }
        }

        public class CommandUnexpectedException : Exception
        {
            public BlockShareCommandType Expected { get;}
            public BlockShareCommandType Actual { get; }
            public CommandUnexpectedException(BlockShareCommandType expected, BlockShareCommandType actual) : base($"Unexpected command received: expected {expected}, actual {actual}")
            {
                Expected = expected;
                Actual = actual;
            }
        }

        protected abstract void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout);

        public abstract void WriteValuesToClient(TcpClient tcpClient, NetStat netStat);
                        

        public static void WriteToClient(BlockShareCommand command, TcpClient tcpClient, NetStat netStat, ILogger logger = null)
        {
            NetworkStream networkStream = tcpClient.GetStream();

            byte[] commandTypeBytes = new byte[1];
            commandTypeBytes[0] = (byte)command.CommandType;
            networkStream.Write(commandTypeBytes, 0, commandTypeBytes.Length);
            command.WriteValuesToClient(tcpClient, netStat);
            logger?.Log($"--> {command}");
        }

        public static T ReadFromClient<T>(TcpClient tcpClient, NetStat netStat, long timeout, Preferences preferences = null, ILogger logger = null) where T : BlockShareCommand
        {            
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] commandTypeBytes = new byte[1];
            Utils.ReadPackage(tcpClient, networkStream, commandTypeBytes, 0, commandTypeBytes.Length, timeout);
            BlockShareCommandType commandType = (BlockShareCommandType)commandTypeBytes[0];

            T command = Activator.CreateInstance<T>();           
            if(command.CommandType != commandType)
            {
                throw new CommandUnexpectedException(command.CommandType, commandType);
            }
            command.Preferences = preferences;
            command.ReadValuesFromClient(tcpClient, netStat, timeout);

            logger?.Log($"<-- {command}");

            return command;
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
                    command = new GetHashlistCommand();
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

                case BlockShareCommandType.OpenFile:
                    command = new OpenFileCommand();
                    break;

                case BlockShareCommandType.SetEntryType:
                    command = new SetEntryTypeCommand();
                    break;

                case BlockShareCommandType.SetBlock:
                    command = new SetBlockCommand();
                    break;

                case BlockShareCommandType.GetFileDigest:
                    command = new GetFileDigestCommand();
                    break;

                case BlockShareCommandType.SetFileDigest:
                    command = new SetFileDigestCommand();
                    break;
            }

            if (command == null)
            {
                throw new CommandTypeNotRecognizedException(commandTypeBytes[0]);
            }

#if DEBUG
            if (command.CommandType != commandType)
            {
                throw new Exception("Wrong command recognition code!");
            }
#endif

            command.ReadValuesFromClient(tcpClient, netStat, timeout);
            Console.WriteLine($"<-- {command}");
            return command;
        }
    }
}
