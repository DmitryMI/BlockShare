using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BlockShare.BlockSharing.DirectoryDigesting
{
    public class FileDigest
    {
        public string RelativePath { get; set; }
        public long Size { get; set; }

        public FileDigest(string relativePath, string absoluteFilePath)
        {
            RelativePath = relativePath;

            //Console.WriteLine($"[FileDigest] Generating digest for {absoluteFilePath}...");

            string osPath = absoluteFilePath;
            if (File.Exists(osPath))
            {
                FileInfo fileInfo = new FileInfo(osPath);
                Size = fileInfo.Length;
            }
            else
            {
                Console.WriteLine($"[FileDigest] Warning: file {osPath} not found!");
            }
        }

        public FileDigest(XmlElement fileElement)
        {
            XmlAttribute sizeAttribute = fileElement.GetAttributeNode("Size");
            if (sizeAttribute == null)
            {
                Console.WriteLine("[FileDigest] Warning: XML digest is malformed (Size Attribute not found)");
                Size = 0;
            }
            else
            {
                Size = long.Parse(sizeAttribute.Value);
            }
            RelativePath = fileElement.InnerText;
        }

        public XmlElement ToXml(XmlDocument document)
        {
            XmlElement fileElement = document.CreateElement(string.Empty, "File", string.Empty);
            fileElement.InnerText = RelativePath;
            XmlAttribute sizeAttribute = document.CreateAttribute("Size");
            sizeAttribute.Value = Size.ToString();
            fileElement.SetAttributeNode(sizeAttribute);
            return fileElement;
        }
    }
}
