using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing
{
    public class FileHashBlock
    {
        public long FilePosition { get; set; }
        public byte[] Hash { get; set; }

        public FileHashBlock(long filePosition, byte[] hash)
        {
            FilePosition = filePosition;
            Hash = hash;
        }

        public bool CompareHash(FileHashBlock otherBlock)
        {
            byte[] otherBlockHash = otherBlock.Hash;
            if(otherBlockHash == null || Hash == null)
            {
                return Hash == otherBlockHash;
            }
            
            for(int i = 0; i < Hash.Length; i++)
            {
                if(Hash[i] != otherBlockHash[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if(obj is FileHashBlock otherBlock)
            {
                return FilePosition == otherBlock.FilePosition && CompareHash(otherBlock);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hashCode = 35217793;
            hashCode = hashCode * -1521134295 + FilePosition.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(Hash);
            return hashCode;
        }

        public static bool operator ==(FileHashBlock block1, FileHashBlock block2)
        {
            if((object)block1 == null || (object)block2 == null)
            {
                return (object)block1 == (object)block2;
            }
            return block1.FilePosition == block2.FilePosition && block1.CompareHash(block2);
        }

        public static bool operator !=(FileHashBlock block1, FileHashBlock block2)
        {
            if ((object)block1 == null || (object)block2 == null)
            {
                return (object)block1 != (object)block2;
            }

            return block1.FilePosition != block2.FilePosition || block1.CompareHash(block2);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"[{FilePosition}] (");
            for(int i = 0; i < Hash.Length; i++)
            {
                builder.Append($"{Hash[i]:X2}");
                if(i < Hash.Length - 1)
                {
                    //builder.Append(' ');
                }
            }
            builder.Append(')');
            return builder.ToString();
        }
    }
}
