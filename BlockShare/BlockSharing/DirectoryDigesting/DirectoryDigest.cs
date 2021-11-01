using BlockShare.BlockSharing.PreferencesManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BlockShare.BlockSharing.DirectoryDigesting
{
    public class DirectoryDigest
    {
        private DirectoryInfo directoryInfo;
        private DirectoryInfo rootDirectoryInfo;

        private List<DirectoryDigest> subdirsDigestList = new List<DirectoryDigest>();
        private List<FileDigest> fileDigestList = new List<FileDigest>();

        public DirectoryInfo GetDirectoryInfo()
        {
            return directoryInfo;
        }

        public bool IsLoaded { get; private set; }
        public long Size { get; private set; }
        public string RelativePath { get; private set; }
        public string Name { get; private set; }

        public DirectoryDigest (DirectoryInfo directoryInfo, DirectoryInfo rootDirectoryInfo, int recursionLevel = Int32.MaxValue)
        {
            this.directoryInfo = directoryInfo;
            this.rootDirectoryInfo = rootDirectoryInfo;            

            if (recursionLevel > 0)
            {
                IEnumerable<DirectoryInfo> directoryInfos = directoryInfo.EnumerateDirectories();

                foreach (DirectoryInfo directory in directoryInfos)
                {
                    DirectoryDigest directoryDigest = new DirectoryDigest(directory, rootDirectoryInfo, recursionLevel - 1);
                    Size += directoryDigest.Size;
                    subdirsDigestList.Add(directoryDigest);
                }

                IEnumerable<FileInfo> fileInfos = directoryInfo.EnumerateFiles();
                foreach (FileInfo fileInfo in fileInfos)
                {
                    string file = fileInfo.FullName;
                    if (file.EndsWith(Preferences.HashlistExtension))
                    {
                        return;
                    }
                    string relativePath = file.Replace(rootDirectoryInfo.FullName, "");
                    if (relativePath[0] == '\\' || relativePath[0] == '/')
                    {
                        relativePath = relativePath.Remove(0, 1);
                    }
                    FileDigest fileDigest = new FileDigest(relativePath, file);
                    Size += fileDigest.Size;
                    fileDigestList.Add(fileDigest);
                }

                IsLoaded = true;
            }
            else
            {
                IsLoaded = false;
                Size = Utils.GetDirSize(directoryInfo);
            }

            string dir = directoryInfo.FullName;
            string dirRelativePath = dir.Replace(rootDirectoryInfo.FullName, "");            
            if (dirRelativePath.Length > 0 && (dirRelativePath[0] == '\\' || dirRelativePath[0] == '/'))
            {
                dirRelativePath = dirRelativePath.Remove(0, 1);
            }

            RelativePath = dirRelativePath;
            Name = directoryInfo.Name;
        }

        private XmlElement ToXmlElement(XmlDocument doc, XmlElement parent)
        { 
            XmlElement directoryDigestElement = doc.CreateElement(string.Empty, "DirectoryDigest", string.Empty);

            if (parent != null)
            {
                parent.AppendChild(directoryDigestElement);
            }

            XmlAttribute sizeAttribute = doc.CreateAttribute("Size");
            sizeAttribute.Value = Size.ToString();
            directoryDigestElement.SetAttributeNode(sizeAttribute);

            XmlAttribute pathAttribute = doc.CreateAttribute("Path");
            pathAttribute.Value = RelativePath;
            directoryDigestElement.SetAttributeNode(pathAttribute);

            XmlAttribute nameAttribute = doc.CreateAttribute("Name");
            nameAttribute.Value = Name;
            directoryDigestElement.SetAttributeNode(nameAttribute);

            XmlAttribute loadedAttribute = doc.CreateAttribute("Loaded");
            loadedAttribute.Value = IsLoaded.ToString();
            directoryDigestElement.SetAttributeNode(loadedAttribute);

            foreach (DirectoryDigest directoryDigest in subdirsDigestList)
            {
                XmlElement dirElement = directoryDigest.ToXmlElement(doc, directoryDigestElement);
                directoryDigestElement.AppendChild(dirElement);
            }

            foreach (FileDigest fileDigest in fileDigestList)
            {
                XmlElement fileElement = fileDigest.ToXmlElement(doc);
                directoryDigestElement.AppendChild(fileElement);
            }

            return directoryDigestElement;
        }

        public static XmlDocument ToXmlDocument(DirectoryDigest rootDigest)
        {
            XmlDocument doc;

            doc = new XmlDocument();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            XmlElement rootElement = rootDigest.ToXmlElement(doc, root);
            doc.AppendChild(rootElement);
            return doc;
        }

        public static string GetXmlString(DirectoryDigest rootDigest)
        {
            string xml = ToXmlDocument(rootDigest).OuterXml;
            return xml;
        }

        public static byte[] Serialize(DirectoryDigest rootDigest)
        {
            return Encoding.UTF8.GetBytes(GetXmlString(rootDigest));
        }

        public void FromXmlElement(XmlElement xmlElement)
        {
            foreach (XmlElement childElement in xmlElement)
            {
                if (childElement.Name == "FileDigest")
                {
                    FileDigest fileDigest = new FileDigest(childElement);
                    fileDigestList.Add(fileDigest);
                }
                if (childElement.Name == "DirectoryDigest")
                {
                    DirectoryDigest directoryDigest = new DirectoryDigest(childElement);
                    subdirsDigestList.Add(directoryDigest);
                }
            }

            XmlAttribute sizeAttribute = xmlElement.GetAttributeNode("Size");
            if (sizeAttribute == null)
            {
                Console.WriteLine("[DirectoryDigest] Warning: XML digest is malformed (Size Attribute not found)");
                Size = 0;
            }
            else
            {
                Size = long.Parse(sizeAttribute.Value);
            }           

            XmlAttribute pathAttribute = xmlElement.GetAttributeNode("Path");
            if (pathAttribute == null)
            {
                Console.WriteLine("[DirectoryDigest] Warning: XML digest is malformed (Path Attribute not found)");
                RelativePath = String.Empty;
            }
            else
            {
                RelativePath = pathAttribute.Value;
            }

            XmlAttribute nameAttribute = xmlElement.GetAttributeNode("Name");
            if (nameAttribute == null)
            {
                Console.WriteLine("[DirectoryDigest] Warning: XML digest is malformed (Name Attribute not found)");
                Name = String.Empty;
            }
            else
            {
                Name = nameAttribute.Value;
            }

            XmlAttribute loadedAttribute = xmlElement.GetAttributeNode("Loaded");
            if (sizeAttribute == null)
            {
                Console.WriteLine("[DirectoryDigest] Warning: XML digest is malformed (Loaded Attribute not found)");
                IsLoaded = true;
            }
            else
            {
                IsLoaded = bool.Parse(loadedAttribute.Value);
            }
        }

        public static DirectoryDigest FromXmlDocument(XmlDocument xmlDigest)
        {
            XmlElement directoryDigestElement = xmlDigest["DirectoryDigest"];

            if (directoryDigestElement == null)
            {
                throw new ArgumentException("Directory Digest is malformed");
            }

            DirectoryDigest directoryDigest = new DirectoryDigest(directoryDigestElement);

            return directoryDigest;
        }

        public static DirectoryDigest FromXmlString(string xmlString)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);

            return FromXmlDocument(doc);
        }

        public DirectoryDigest(XmlElement xmlElementDigest)
        {
            FromXmlElement(xmlElementDigest);
        }

        public static DirectoryDigest Deserialize(byte[] serializedDigest)
        {
            string xmlString = Encoding.UTF8.GetString(serializedDigest);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);

            return FromXmlDocument(doc);
        }

        public IReadOnlyList<DirectoryDigest> GetSubDirectories()
        {
            return subdirsDigestList;
        }

        public IReadOnlyList<FileDigest> GetFiles()
        {
            return fileDigestList;
        }

        public IReadOnlyList<FileDigest> GetFilesRecursive()
        {
            List<FileDigest> fileList = new List<FileDigest>();
            foreach(DirectoryDigest dir in subdirsDigestList)
            {
                fileList.AddRange(dir.GetFilesRecursive());
            }

            fileList.AddRange(GetFiles());

            return fileList;
        }

        public void LoadEntriesFrom(DirectoryDigest directoryDigest)
        {
            IsLoaded = directoryDigest.IsLoaded;
            fileDigestList = directoryDigest.fileDigestList;
            subdirsDigestList = directoryDigest.subdirsDigestList;
        }
    }
}
