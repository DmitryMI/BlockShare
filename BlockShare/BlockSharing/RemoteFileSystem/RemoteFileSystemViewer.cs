using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using BlockShare.BlockSharing.DirectoryDigesting;

namespace BlockShare.BlockSharing.RemoteFileSystem
{
    public class RemoteFileSystemViewer
    {
        public static char PathSeparatorChar = Path.DirectorySeparatorChar;

        private readonly RemoteDirectoryInfo remoteRoot;

        private RemoteDirectoryInfo currentDirectory;
        public RemoteDirectoryInfo CurrentDirectory => currentDirectory;

        private void EnsureFilePathExists(string fullPath, FileDigest fileDigest = null)
        {
            if (fullPath[0] == Path.DirectorySeparatorChar || fullPath[0] == Path.AltDirectorySeparatorChar)
            {
                fullPath = fullPath.Remove(0, 1);
            }
            if (fullPath[fullPath.Length - 1] == PathSeparatorChar || fullPath[fullPath.Length - 1] == Path.AltDirectorySeparatorChar)
            {
                fullPath = fullPath.Remove(fullPath.Length - 1, 1);
            }

            string[] pathParts = fullPath.Split(PathSeparatorChar);
            RemoteDirectoryInfo dir = remoteRoot;
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                var childrenDirs = dir.EnumerateDirectories();
                RemoteDirectoryInfo child = childrenDirs.FirstOrDefault(c => c.Name == pathParts[i]);
                if (child == null)
                {
                    child = new RemoteDirectoryInfo(dir.RemoteFullPath + PathSeparatorChar + pathParts[i], pathParts[i], dir);
                    dir.Add(child);
                }

                dir = child;
            }

            RemoteFileInfo file = new RemoteFileInfo(fullPath, pathParts[pathParts.Length - 1], dir, fileDigest);
            dir.Add(file);
        }
        
        [Obsolete]
        public RemoteFileSystemViewer(string[] remoteFiles)
        {
            remoteRoot = new RemoteDirectoryInfo("", string.Empty, null);

            foreach (var fileName in remoteFiles)
            {
                EnsureFilePathExists(fileName);
            }

            currentDirectory = remoteRoot;
        }

        public RemoteFileSystemViewer(DirectoryDigest directoryDigest)
        {
            remoteRoot = new RemoteDirectoryInfo("", string.Empty, null);

            foreach (var fileDigest in directoryDigest)
            {
                EnsureFilePathExists(fileDigest.RelativePath, fileDigest);
            }

            currentDirectory = remoteRoot;
        }

        public RemoteDirectoryInfo[] ListCurrentSubDirectories()
        {
            var dirs = currentDirectory.EnumerateDirectories();
            return dirs.ToArray();
        }

        public RemoteFileInfo[] ListCurrentFiles()
        {
            var files = currentDirectory.EnumerateFiles();
            return files.ToArray();
        }

        public bool GoUp()
        {
            if (currentDirectory.Parent != null)
            {
                currentDirectory = currentDirectory.Parent;
                return true;
            }

            return false;
        }

        public bool EnterByAbsolutePath(string name)
        {
            if (name[0] == PathSeparatorChar)
            {
                name = name.Remove(0, 1);
            }
            if (name[name.Length - 1] == PathSeparatorChar)
            {
                name = name.Remove(name.Length - 1, 1);
            }

            string[] pathParts = name.Split(PathSeparatorChar);
            RemoteDirectoryInfo dir = remoteRoot;
            for (int i = 0; i < pathParts.Length; i++)
            {
                var childrenDirs = dir.EnumerateDirectories();
                RemoteDirectoryInfo child = childrenDirs.FirstOrDefault(c => Utils.ArePathsEqual(c.Name, pathParts[i]));
                if (child == null)
                {
                    return false;
                }

                dir = child;
            }

            if (dir != null)
            {
                currentDirectory = dir;
            }

            return false;
        }

        public bool EnterFromCurrentDirectory(string name)
        {
            if (name[0] == PathSeparatorChar)
            {
                name = name.Remove(0, 1);
            }
            if (name[name.Length - 1] == PathSeparatorChar)
            {
                name = name.Remove(name.Length - 1, 1);
            }

            string[] pathParts = name.Split(PathSeparatorChar);
            RemoteDirectoryInfo dir = currentDirectory;
            for (int i = 0; i < pathParts.Length; i++)
            {
                var childrenDirs = dir.EnumerateDirectories();
                RemoteDirectoryInfo child = childrenDirs.FirstOrDefault(c => Utils.ArePathsEqual(c.Name, pathParts[i]));
                if (child == null)
                {
                    return false;
                }

                dir = child;
            }

            if (dir != null)
            {
                currentDirectory = dir;
            }

            return false;
        }
    }
}
