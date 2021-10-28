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
        public string Name { get; private set; }
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
                Name = fileInfo.Name;
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

            XmlAttribute pathAttribute = fileElement.GetAttributeNode("Path");
            if (pathAttribute == null)
            {
                Console.WriteLine("[FileDigest] Warning: XML digest is malformed (Path Attribute not found)");
                RelativePath = String.Empty;
            }
            else
            {
                RelativePath = pathAttribute.Value;
            }
            XmlAttribute nameAttribute = fileElement.GetAttributeNode("Name");
            if (nameAttribute == null)
            {
                Console.WriteLine("[FileDigest] Warning: XML digest is malformed (Name Attribute not found)");
                Name = String.Empty;
            }
            else
            {
                Name = nameAttribute.Value;
            }
        }

        public XmlElement ToXmlElement(XmlDocument document)
        {
            XmlElement fileElement = document.CreateElement(string.Empty, "FileDigest", string.Empty);
            //fileElement.InnerText = RelativePath;
            XmlAttribute sizeAttribute = document.CreateAttribute("Size");
            sizeAttribute.Value = Size.ToString();
            fileElement.SetAttributeNode(sizeAttribute);

            XmlAttribute pathAttribute = document.CreateAttribute("Path");
            pathAttribute.Value = RelativePath;
            fileElement.SetAttributeNode(pathAttribute);

            XmlAttribute nameAttribute = document.CreateAttribute("Name");
            nameAttribute.Value = Name;
            fileElement.SetAttributeNode(nameAttribute);

            return fileElement;
        }
    }
}
