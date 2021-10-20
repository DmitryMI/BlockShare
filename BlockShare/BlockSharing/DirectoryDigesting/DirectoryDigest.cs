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
    public class DirectoryDigest : IList<FileDigest>
    {
        private DirectoryInfo directoryInfo;
        private DirectoryInfo rootDirectoryInfo;

        private List<FileDigest> fileDigestList = new List<FileDigest>();

        public DirectoryDigest (DirectoryInfo directoryInfo, DirectoryInfo rootDirectoryInfo)
        {
            this.directoryInfo = directoryInfo;
            this.rootDirectoryInfo = rootDirectoryInfo;

            void AddFileToList(string file, object notInUse)
            {
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
                fileDigestList.Add(fileDigest);
            }

            Utils.ForEachFsEntry<object>(directoryInfo.FullName, null, AddFileToList);
        }

        public XmlDocument ToXmlDocument()
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            XmlElement directoryDigestElement = doc.CreateElement(string.Empty, "DirectoryDigest", string.Empty);

            doc.AppendChild(directoryDigestElement);

            foreach (FileDigest fileDigest in fileDigestList)
            {
                XmlElement fileElement = fileDigest.ToXml(doc);
                directoryDigestElement.AppendChild(fileElement);
            }

            return doc;
        }

        public string GetXmlString()
        {
            return ToXmlDocument().OuterXml;
        }

        public byte[] Serialize()
        {
            return Encoding.UTF8.GetBytes(GetXmlString());
        }

        private void FromXmlDocument(XmlDocument xmlDigest)
        {
            XmlElement directoryDigestElement = xmlDigest["DirectoryDigest"];

            if (directoryDigestElement == null)
            {
                throw new ArgumentException("Directory Digest is malformed");
            }

            foreach (XmlElement fileElement in directoryDigestElement)
            {
                FileDigest fileDigest = new FileDigest(fileElement);
                fileDigestList.Add(fileDigest);
            }
        }

        public DirectoryDigest(XmlDocument xmlDigest)
        {
            FromXmlDocument(xmlDigest);
        }

        public DirectoryDigest(byte[] serializedDigest)
        {
            string xmlString = Encoding.UTF8.GetString(serializedDigest);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);

            FromXmlDocument(doc);
        }

        #region ListImplementation

        public IEnumerator<FileDigest> GetEnumerator()
        {
            return fileDigestList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) fileDigestList).GetEnumerator();
        }

        public void Add(FileDigest item)
        {
            fileDigestList.Add(item);
        }

        public void Clear()
        {
            fileDigestList.Clear();
        }

        public bool Contains(FileDigest item)
        {
            return fileDigestList.Contains(item);
        }

        public void CopyTo(FileDigest[] array, int arrayIndex)
        {
            fileDigestList.CopyTo(array, arrayIndex);
        }

        public bool Remove(FileDigest item)
        {
            return fileDigestList.Remove(item);
        }

        public int Count => fileDigestList.Count;

        public bool IsReadOnly => false;

        public int IndexOf(FileDigest item)
        {
            return fileDigestList.IndexOf(item);
        }

        public void Insert(int index, FileDigest item)
        {
            fileDigestList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            fileDigestList.RemoveAt(index);
        }

        public FileDigest this[int index]
        {
            get => fileDigestList[index];
            set => fileDigestList[index] = value;
        }


        #endregion
    }
}
