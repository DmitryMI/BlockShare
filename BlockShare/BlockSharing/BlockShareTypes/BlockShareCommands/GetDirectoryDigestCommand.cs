﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes.BlockShareCommands
{
    public class GetDirectoryDigestCommand : BlockShareCommand
    {
        public string Path { get; set; }
        public int RecursionLevel { get; set; }

        public override BlockShareCommandType CommandType => BlockShareCommandType.GetDirectoryDigest;

        public GetDirectoryDigestCommand()
        {

        }

        public GetDirectoryDigestCommand(string path, int recursionLevel)
        {
            Path = path;
            RecursionLevel = recursionLevel;
        }


        public override void WriteValuesToClient(TcpClient tcpClient, NetStat netStat)
        {
            NetUtils.WriteString(Path, tcpClient, netStat);
            NetUtils.WriteInt(RecursionLevel, tcpClient, netStat);
        }

        protected override void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            Path = NetUtils.ReadString(tcpClient, netStat, timeout);
            RecursionLevel = NetUtils.ReadInt(tcpClient, netStat, timeout);
        }

        public override string ToString()
        {
            return $"GetDirectoryDigest(Path: {Path}, RecursionLevel: {RecursionLevel})";
        }
    }
}
