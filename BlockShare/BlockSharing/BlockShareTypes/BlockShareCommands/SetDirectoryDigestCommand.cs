using System;
using System.Collections.Generic;
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

        public override void WriteValuesToClient(TcpClient tcpClient, NetStat netStat)
        {
            WriteString(XmlPayload, tcpClient, netStat);
        }

        protected override void ReadValuesFromClient(TcpClient tcpClient, NetStat netStat, long timeout)
        {
            XmlPayload = ReadString(tcpClient, netStat, timeout);
        }

        public override string ToString()
        {
            return $"SetDirectoryDigest(XmlPayloadLength: {XmlPayload.Length})";
        }
    }
}
