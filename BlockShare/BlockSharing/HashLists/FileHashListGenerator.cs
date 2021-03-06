using BlockShare.BlockSharing.BlockShareTypes;
using BlockShare.BlockSharing.HashLists;
using BlockShare.BlockSharing.PreferencesManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.HashLists
{
    public static class FileHashListGenerator
    {
        public static FileHashList GenerateHashList(Stream fileStream, Stream serializationStream, Preferences preferences, Action<Stream, double> onHashingProgressChanged = null, Action<Stream> onHashingFinished = null)
        {
            SHA256 sha256 = SHA256.Create();

            long fileLength = fileStream.Length;
            int blocksCount = (int)(fileLength / preferences.BlockSize);
            if(fileLength % preferences.BlockSize != 0)
            {
                blocksCount++;
            }

            FileHashList hashList = new FileHashList(blocksCount, serializationStream, preferences);

            for(int i = 0; i < blocksCount; i++)
            {
                long filePosition = i * preferences.BlockSize;
                byte[] block = new byte[preferences.BlockSize];
                int readBytes = fileStream.Read(block, 0, (int)preferences.BlockSize);
                byte[] hash = sha256.ComputeHash(block, 0, readBytes);
                FileHashBlock hashBlock = new FileHashBlock(filePosition, hash);
                hashList[i] = hashBlock;

                double progress = (double)i / blocksCount;

                onHashingProgressChanged?.Invoke(fileStream, progress);
            }
            onHashingFinished?.Invoke(fileStream);
            return hashList;
        }

        [Obsolete]
        public static void GenerateHashList(Stream fileStream, Preferences preferences, FileHashList hashList, Action<Stream, double> onHashingProgressChanged = null, Action<Stream> onHashingFinished = null)
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
                //progressReporter?.ReportOverallProgress(null, progress);
                onHashingProgressChanged?.Invoke(fileStream, progress);
            }
            //progressReporter?.ReportOverallProgress(null, 1.0f);
            onHashingFinished?.Invoke(fileStream);
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
