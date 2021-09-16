using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing
{
    public class FileHashList : IEnumerable<FileHashBlock>
    {
        private FileHashBlock[] hashBlocks;

        public FileHashList()
        {
            hashBlocks = new FileHashBlock[0];
        }
        public FileHashList(int blocksCount)
        {
            hashBlocks = new FileHashBlock[blocksCount];            
        }

        public FileHashList(int blocksCount, int blockSize)
        {
            hashBlocks = new FileHashBlock[blocksCount];
            for(int i = 0; i < hashBlocks.Length; i++)
            {
                long pos = i * blockSize;
                hashBlocks[i] = new FileHashBlock(pos, null);
            }
        }

        public static FileHashList Deserialise(byte[] serializedHashList, int hashLength, int blockSize)
        {
            int blocksCount = serializedHashList.Length / hashLength;
            FileHashList list = new FileHashList();
            list.hashBlocks = new FileHashBlock[blocksCount];

            for(int i = 0; i < blocksCount; i++)
            {
                byte[] hash = new byte[hashLength];
                int filePosition = i * blockSize;
                Array.Copy(serializedHashList, i * hashLength, hash, 0, hashLength);
                list.hashBlocks[i] = new FileHashBlock(filePosition, hash);          
            }

            return list;
        }

        public IEnumerator<FileHashBlock> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize(int hashLength)
        {
            int dataLength = hashBlocks.Length * hashLength;
            byte[] data = new byte[dataLength];

            for(int i = 0; i < hashBlocks.Length; i++)
            {
                int filePosition = i * hashLength;
                Array.Copy(hashBlocks[i].Hash, 0, data, filePosition, hashLength);
            }

            return data;
        }

        public int BlocksCount => hashBlocks.Length;

        public FileHashBlock this[int block] { get => hashBlocks[block]; set => hashBlocks[block] = value; }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
