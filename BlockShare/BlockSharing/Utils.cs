using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing
{
    static class Utils
    {
        public static void ReadPackage(Stream stream, byte[] package, int offset, int length)
        {
            int bytesRead = 0;
            while (bytesRead < length)
            {
                int stepOffset = offset + bytesRead;
                int stepLength = length - bytesRead;
                int stepBytes = stream.Read(package, stepOffset, stepLength);
                bytesRead += stepBytes;
            }
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
