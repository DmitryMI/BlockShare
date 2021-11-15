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
    public class SetDirectoryDigestCommand : BlockShareCommand
    {
        public string XmlPayload { get; set; }

        public override BlockShareCommandType CommandType => BlockShareCommandType.SetDirectoryDigest;

        public SetDirectoryDigestCommand()
        {

        }

        public override void WriteValuesToClient(Stream networkStream, NetStat netStat)
        {
            NetUtils.WriteString(XmlPayload, networkStream, netStat);
        }

        protected override void ReadValuesFromClient(Stream networkStream, NetStat netStat, long timeout)
        {
            XmlPayload = NetUtils.ReadString(networkStream, netStat, timeout);
        }

        public override string ToString()
        {
            return $"SetDirectoryDigest(XmlPayloadLength: {XmlPayload.Length})";
        }
    }
}
