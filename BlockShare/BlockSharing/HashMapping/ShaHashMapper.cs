using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.HashMapping
{
    public class ShaHashMapper : HashMapper
    {
        private SHA256 sha = SHA256.Create();

        public string HashpartStoragePath { get; }
        public string HashlistStoragePath { get; }

        public ShaHashMapper(string hashpartStoragePath, string hashlistStoragePath)
        {
            HashpartStoragePath = hashpartStoragePath;
            HashlistStoragePath = hashlistStoragePath;

            if (!Directory.Exists(HashpartStoragePath))
            {
                Directory.CreateDirectory(HashpartStoragePath);
            }
            if (!Directory.Exists(HashlistStoragePath))
            {
                Directory.CreateDirectory(HashlistStoragePath);
            }
        }

        public override string GetHashpartFile(string filePath)
        {
            byte[] filePathBytes = Encoding.UTF8.GetBytes(filePath);
            byte[] shaHash = sha.ComputeHash(filePathBytes);
            string base64 = Utils.ToSafeBase64(shaHash);

            string fileHashPartPath = Path.Combine(HashpartStoragePath, base64 + Preferences.HashpartExtension);
            return fileHashPartPath;
        }

        public override string GetHashlistFile(string filePath)
        {
            byte[] filePathBytes = Encoding.UTF8.GetBytes(filePath);
            byte[] shaHash = sha.ComputeHash(filePathBytes);
            string base64 = Utils.ToSafeBase64(shaHash);

            string fileHashPartPath = Path.Combine(HashlistStoragePath, base64 + Preferences.HashlistExtension);
            return fileHashPartPath;
        }
    }
}
