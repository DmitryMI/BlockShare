using BlockShare.BlockSharing.PreferencesManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BlockShare.BlockSharing.HashMapping
{
    public class ShaHashMapper : HashMapper
    {
        private SHA256 sha = SHA256.Create();

        public string HashpartStoragePath { get; private set; }
        public string HashlistStoragePath { get; private set; }

        public ShaHashMapper()
        {

        }

        private void EnsureHashpartDirectoryExist()
        {
            if (!Directory.Exists(HashpartStoragePath))
            {
                Directory.CreateDirectory(HashpartStoragePath);
            }
        }

        private void EnsureHashlistDirectoryExist()
        {
            if (!Directory.Exists(HashlistStoragePath))
            {
                Directory.CreateDirectory(HashlistStoragePath);
            }
        }

        public ShaHashMapper(string hashpartStoragePath, string hashlistStoragePath)
        {
            HashpartStoragePath = hashpartStoragePath;
            HashlistStoragePath = hashlistStoragePath;    
        }

        public override string GetHashpartFile(string filePath)
        {
            EnsureHashpartDirectoryExist();
            byte[] filePathBytes = Encoding.UTF8.GetBytes(filePath);
            byte[] shaHash = sha.ComputeHash(filePathBytes);
            string base64 = Utils.ToSafeBase64(shaHash);

            string fileHashPartPath = Path.Combine(HashpartStoragePath, base64 + Preferences.HashpartExtension);
            return fileHashPartPath;
        }

        public override string GetHashlistFile(string filePath)
        {
            EnsureHashlistDirectoryExist();
            byte[] filePathBytes = Encoding.UTF8.GetBytes(filePath);
            byte[] shaHash = sha.ComputeHash(filePathBytes);
            string base64 = Utils.ToSafeBase64(shaHash);

            string fileHashPartPath = Path.Combine(HashlistStoragePath, base64 + Preferences.HashlistExtension);
            return fileHashPartPath;
        }

        public override void ToXmlElement(XmlDocument doc, XmlElement xmlElement)
        {
            base.ToXmlElement(doc, xmlElement);

            xmlElement.SetAttribute("HashpartStoragePath", HashpartStoragePath);
            xmlElement.SetAttribute("HashlistStoragePath", HashlistStoragePath);
        }

        public override object FromXmlElement(XmlElement xmlElement)
        {
            HashpartStoragePath = xmlElement.GetAttribute("HashpartStoragePath");
            HashlistStoragePath = xmlElement.GetAttribute("HashlistStoragePath");

            return this;
        }
    }
}
