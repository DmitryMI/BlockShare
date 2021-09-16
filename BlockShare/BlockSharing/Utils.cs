using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing
{
    static class Utils
    {
        public static void ReadPackage(Stream stream, byte[] package, int offset, int length, long timeoutMillis)
        {
            int bytesRead = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (bytesRead < length)
            {
                int stepOffset = offset + bytesRead;
                int stepLength = length - bytesRead;
                int stepBytes = stream.Read(package, stepOffset, stepLength);
                bytesRead += stepBytes;
                if(timeoutMillis > 0)
                {
                    if(sw.ElapsedMilliseconds > timeoutMillis)
                    {
                        throw new TimeoutException();
                    }
                }
            }
            sw.Stop();
        }

        public static string PrintHex(byte[] array, int offset, int length)
        {
            StringBuilder builder = new StringBuilder();
            for(int i = 0; i < length; i++)
            {
                builder.Append(array[i].ToString("X2"));
            }

            return builder.ToString();
        }
    }
}
