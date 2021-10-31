using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareTypes
{
    public class NetStat : ICloneable
    {
        public ulong TotalReceived { get; set; }
        public ulong TotalSent { get; set; }
        public ulong Payload { get; set; }

        public static NetStat operator -(NetStat a, NetStat b)
        {
            if (b == null)
            {
                return a;
            }

            if (a == null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            NetStat diff = new NetStat
            {
                TotalSent = a.TotalSent - b.TotalSent,
                TotalReceived = a.TotalReceived - b.TotalReceived,
                Payload = a.Payload - b.Payload
            };
            return diff;
        }

        public object Clone()
        {
            return CloneNetStat();
        }

        public NetStat CloneNetStat()
        {
            NetStat clone = new NetStat
            {
                TotalSent = TotalSent,
                TotalReceived = TotalReceived,
                Payload = Payload
            };
            return clone;
        }
    }
}
