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
    public class InvalidOperationCommand : BlockShareCommand
    {
        public override BlockShareCommandType CommandType => BlockShareCommandType.InvalidOperation;

        public InvalidOperationCommand()
        {

        }

        public override void WriteValuesToClient(Stream networkStream, NetStat netStat)
        {
            
        }

        protected override void ReadValuesFromClient(Stream networkStream, NetStat netStat, long timeout)
        {
            
        }

        public override string ToString()
        {
            return $"InvalidOperation()";
        }
    }
}
