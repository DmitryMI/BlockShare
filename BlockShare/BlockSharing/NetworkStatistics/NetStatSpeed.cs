using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.NetworkStatistics
{
    public class NetStatSpeed
    {
        public double DownSpeed { get; set; }
        public double UpSpeed { get; set; }

        public NetStatSpeed(double down, double up)
        {
            DownSpeed = down;
            UpSpeed = up;
        }
    }
}
