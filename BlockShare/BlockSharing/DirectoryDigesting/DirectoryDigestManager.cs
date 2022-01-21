using BlockShare.BlockSharing.BlockShareTypes;
using BlockShare.BlockSharing.DirectoryDigesting.Exceptions;
using BlockShare.BlockSharing.PreferencesManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.DirectoryDigesting
{
    public class DirectoryDigestManager
    {
        private Preferences preferences;

        private DirectoryDigest rootDigest;
        private DirectoryInfo rootDirInfo;

        private FileSystemWatcher fileSystemWatcher;

        public ILogger Logger { get; set; }

        public DirectoryDigestManager(Preferences preferences, ILogger logger)
        {
            this.preferences = preferences;

            Logger = logger;

            rootDigest = new DirectoryDigest();
            rootDirInfo = new DirectoryInfo(preferences.ServerStoragePath);

            if (preferences.UseDigestCache)
            {
                throw new NotImplementedException();

                fileSystemWatcher = new FileSystemWatcher(preferences.ServerStoragePath);
                fileSystemWatcher.EnableRaisingEvents = true;
                fileSystemWatcher.IncludeSubdirectories = true;
                fileSystemWatcher.Created += FileSystemWatcher_Created;
                fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
                fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
                fileSystemWatcher.Error += FileSystemWatcher_Error;

                Logger?.Log("Generating Directory Digest for server storage directory");
               
                rootDigest.GenerateDigest(rootDirInfo, rootDirInfo, int.MaxValue);
                Logger?.Log("Digest generated");
            }
        }

        private void FileSystemWatcher_Error(object sender, ErrorEventArgs e)
        {
            Logger?.Log(e.GetException().Message);
        }

        private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {

        }

        private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {

        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {

        }

        public DirectoryDigest GetDirectoryDigest(DirectoryInfo targetDirectory, int recursionLevel = Int32.MaxValue)
        {
            if(preferences.UseDigestCache)
            {
                throw new NotImplementedException();

                string path = targetDirectory.FullName;
                string relativePath = GetRelativePath(path);
                DirectoryDigest cachedDigest = null;
                try
                {
                    EntryDigest entryDigest = rootDigest.GetEntry(relativePath);
                    cachedDigest = (DirectoryDigest)entryDigest;
                }
                catch (PathNotFoundException ex)
                {
                    Logger?.Log(ex.Message);
                }
                catch (PathSegmentIsFileException ex)
                {
                    Logger?.Log(ex.Message);
                }
                catch (Exception ex)
                {
                    Logger?.Log(ex.Message);
                }

                if (cachedDigest != null)
                {
                    return cachedDigest;
                }

                throw new NotImplementedException();
            }
            else
            {
                DirectoryDigest directoryDigest = new DirectoryDigest();
                directoryDigest.GenerateDigest(targetDirectory, rootDirInfo, recursionLevel);
                return directoryDigest;
            }
           
        }

        public string GetRelativePath(string fullPath)
        {
            string dirRelativePath = fullPath.Replace(rootDirInfo.FullName, "");
            if (dirRelativePath.Length > 0 && (dirRelativePath[0] == '\\' || dirRelativePath[0] == '/'))
            {
                dirRelativePath = dirRelativePath.Remove(0, 1);
            }

            return dirRelativePath;
        }
    }
}
