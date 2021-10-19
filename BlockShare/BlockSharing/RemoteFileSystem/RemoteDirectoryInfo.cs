using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.RemoteFileSystem
{
    public class RemoteDirectoryInfo : RemoteFileSystemEntryInfo, IList<RemoteFileSystemEntryInfo>
    {
        private readonly List<RemoteFileSystemEntryInfo> entries = new List<RemoteFileSystemEntryInfo>();

        public IEnumerable<RemoteFileInfo> EnumerateFiles()
        {
            return entries.OfType<RemoteFileInfo>();
        }

        public IEnumerable<RemoteDirectoryInfo> EnumerateDirectories()
        {
            return entries.OfType<RemoteDirectoryInfo>();
        }

        public IEnumerator<RemoteFileSystemEntryInfo> GetEnumerator()
        {
            return entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) entries).GetEnumerator();
        }

        public void Add(RemoteFileSystemEntryInfo item)
        {
            entries.Add(item);
        }

        public void Clear()
        {
            entries.Clear();
        }

        public bool Contains(RemoteFileSystemEntryInfo item)
        {
            return entries.Contains(item);
        }

        public void CopyTo(RemoteFileSystemEntryInfo[] array, int arrayIndex)
        {
            entries.CopyTo(array, arrayIndex);
        }

        public bool Remove(RemoteFileSystemEntryInfo item)
        {
            return entries.Remove(item);
        }

        public int Count => entries.Count;

        public bool IsReadOnly => false;

        public int IndexOf(RemoteFileSystemEntryInfo item)
        {
            return entries.IndexOf(item);
        }

        public void Insert(int index, RemoteFileSystemEntryInfo item)
        {
            entries.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            entries.RemoveAt(index);
        }

        public RemoteFileSystemEntryInfo this[int index]
        {
            get => entries[index];
            set => entries[index] = value;
        }

        public RemoteDirectoryInfo(string fullPath, string name, RemoteDirectoryInfo parent) : base(fullPath, name, parent)
        {
        }
    }
}
