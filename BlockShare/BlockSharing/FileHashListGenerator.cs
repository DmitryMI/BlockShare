using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing
{
    public static class FileHashListGenerator
    {
        public static FileHashList GenerateHashList(Stream fileStream, Preferences preferences, IProgressReporter progressReporter)
        {
            SHA256 sha256 = SHA256.Create();

            long fileLength = fileStream.Length;
            int blocksCount = (int)(fileLength / preferences.BlockSize);
            if(fileLength % preferences.BlockSize != 0)
            {
                blocksCount++;
            }

            FileHashList hashList = new FileHashList(blocksCount);

            for(int i = 0; i < blocksCount; i++)
            {
                long filePosition = i * preferences.BlockSize;
                byte[] block = new byte[preferences.BlockSize];
                int readBytes = fileStream.Read(block, 0, (int)preferences.BlockSize);
                byte[] hash = sha256.ComputeHash(block, 0, readBytes);
                FileHashBlock hashBlock = new FileHashBlock(filePosition, hash);
                hashList[i] = hashBlock;

                double progress = (double)i / blocksCount;
                progressReporter?.ReportProgress(null, progress);
            }

            return hashList;
        }

        public static void GenerateHashList(Stream fileStream, Preferences preferences, FileHashList hashList, IProgressReporter progressReporter)
        {
            SHA256 sha256 = SHA256.Create();

            long fileLength = fileStream.Length;
            int blocksCount = (int)(fileLength / preferences.BlockSize);
            if (fileLength % preferences.BlockSize != 0)
            {
                blocksCount++;
            }

            for (int i = 0; i < blocksCount; i++)
            {
                long filePosition = i * preferences.BlockSize;
                byte[] block = new byte[preferences.BlockSize];
                int readBytes = fileStream.Read(block, 0, (int)preferences.BlockSize);
                byte[] hash = sha256.ComputeHash(block, 0, readBytes);
                FileHashBlock hashBlock = new FileHashBlock(filePosition, hash);
                hashList[i] = hashBlock;

                double progress = (double)i / blocksCount;
                progressReporter?.ReportProgress(null, progress);
            }
            progressReporter?.ReportProgress(null, 1.0f);
        }

        public static FileHashBlock CalculateBlock(Stream fileStream, Preferences preferences, int blockIndex)
        {
            SHA256 sha256 = SHA256.Create();
            long filePosition = blockIndex * preferences.BlockSize;
            byte[] block = new byte[preferences.BlockSize];
            fileStream.Seek(filePosition, SeekOrigin.Begin);
            fileStream.Read(block, 0, (int)preferences.BlockSize);
            byte[] hash = sha256.ComputeHash(block);
            FileHashBlock hashBlock = new FileHashBlock(filePosition, hash);
            return hashBlock;
        }

        public static FileHashBlock CalculateBlock(byte[] block, int offset, int length, Preferences preferences, int blockIndex)
        {
            SHA256 sha256 = SHA256.Create();
            long filePosition = blockIndex * preferences.BlockSize;            
            byte[] hash = sha256.ComputeHash(block, offset, length);
            FileHashBlock hashBlock = new FileHashBlock(filePosition, hash);
            return hashBlock;
        }        
    }
}
