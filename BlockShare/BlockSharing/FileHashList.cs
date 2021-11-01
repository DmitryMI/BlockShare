using BlockShare.BlockSharing.PreferencesManagement;
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
        private bool[] dirtyBlocks;

        private Stream serializationStream;
        private Preferences preferences;

        public FileHashList(Stream serializationStream, Preferences preferences)
        {
            hashBlocks = new FileHashBlock[0];
            dirtyBlocks = new bool[0];
            this.serializationStream = serializationStream;
            this.preferences = preferences;

            if(serializationStream != null)
            {
                DeserializeFromStream(preferences);
            }
        }
        public FileHashList(int blocksCount, Stream serializationStream, Preferences preferences)
        {
            hashBlocks = new FileHashBlock[blocksCount];
            dirtyBlocks = new bool[blocksCount];
            this.serializationStream = serializationStream;
            this.preferences = preferences;
        }        

        private void DeserializeFromStream(Preferences preferences)
        {
            int blocksCount = (int)(serializationStream.Length / preferences.HashSize);
            if(hashBlocks.Length <= blocksCount)
            {
                Array.Resize(ref hashBlocks, blocksCount);
                Array.Resize(ref dirtyBlocks, blocksCount);
            }
            for (int i = 0; i < blocksCount; i++)
            {
                byte[] hash = new byte[preferences.HashSize];
                long filePosition = i * preferences.BlockSize;
                serializationStream.Read(hash, 0, hash.Length);
                hashBlocks[i] = new FileHashBlock(filePosition, hash);
                dirtyBlocks[i] = false;
            }
        }

        public static FileHashList Deserialise(byte[] serializedHashList, Stream serializationStream, Preferences preferences)
        {
            int blocksCount = serializedHashList.Length / preferences.HashSize;
            FileHashList list = new FileHashList(blocksCount, serializationStream, preferences);

            for(int i = 0; i < blocksCount; i++)
            {
                byte[] hash = new byte[preferences.GetHashSize()];
                long filePosition = (i * preferences.BlockSize);
                Array.Copy(serializedHashList, i * preferences.GetHashSize(), hash, 0, preferences.GetHashSize());
                list[i] = new FileHashBlock(filePosition, hash);               
            }

            return list;
        }

        public IEnumerator<FileHashBlock> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize()
        {
            int dataLength = hashBlocks.Length * preferences.GetHashSize();
            byte[] data = new byte[dataLength];

            for(int i = 0; i < hashBlocks.Length; i++)
            {
                int filePosition = i * preferences.GetHashSize();
                Array.Copy(hashBlocks[i].Hash, 0, data, filePosition, preferences.GetHashSize());
            }

            return data;
        }

        public int BlocksCount => hashBlocks.Length;

        public void Flush(int block)
        {
            if (serializationStream == null)
            {
                throw new InvalidOperationException("This hashlist cannot be serialized to dist: SerializationStream is null");
            }

            int streamPosition = block * preferences.GetHashSize();
            serializationStream.Seek(streamPosition, SeekOrigin.Begin);
            serializationStream.Write(hashBlocks[block].Hash, 0, preferences.GetHashSize());
            dirtyBlocks[block] = false;

            serializationStream.Flush();
        }

        public void Flush()
        {
            if(serializationStream == null)
            {
                throw new InvalidOperationException("This hashlist cannot be serialized to dist: SerializationStream is null");
            }

            for(int i = 0; i < BlocksCount; i++)
            {
                if(dirtyBlocks[i])
                {
                    int streamPosition = i * preferences.GetHashSize();
                    serializationStream.Seek(streamPosition, SeekOrigin.Begin);
                    serializationStream.Write(hashBlocks[i].Hash, 0, preferences.GetHashSize());
                    dirtyBlocks[i] = false;
                }
            }

            serializationStream.Flush();
        }

        public FileHashBlock this[int block]
        {
            get
            {
                if(block < BlocksCount)
                {
                    return hashBlocks[block];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if(block >= BlocksCount)
                {
                    Array.Resize(ref hashBlocks, block + 1);
                    Array.Resize(ref dirtyBlocks, block + 1);
                }
                hashBlocks[block] = value;
                dirtyBlocks[block] = true;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
