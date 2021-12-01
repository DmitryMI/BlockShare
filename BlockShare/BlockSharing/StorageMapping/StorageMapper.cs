using BlockShare.BlockSharing.BlockShareTypes;
using BlockShare.BlockSharing.PreferencesManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.StorageMapping
{
    public class StorageMapper
    {
        private Preferences preferences;
        private Dictionary<string, string> mappings;
        public ILogger Logger { get; set; }

        public StorageMapper(Preferences preferences, string mappingsFile, ILogger logger)
        {
            this.preferences = preferences;
            Logger = logger;
            mappings = new Dictionary<string, string>();

            if (mappingsFile != null)
            {
                LoadMappingsFromFile(mappingsFile);
            }
        }

        private void ReadFromLine(string line, out string key, out string value)
        {
            StringBuilder keyBuilder = new StringBuilder();
            StringBuilder valueBuilder = new StringBuilder();
            int keyState = 0;
            int valueState = 0; 
            for(int i = 0; i < line.Length; i++)
            {
                if(line[i] == '"')
                {
                    if(keyState < 2)
                    {
                        keyState++;
                    }
                    else if(valueState < 2)
                    {
                        valueState++;
                    }
                    else
                    {
                        throw new MappingFileParsingException();
                    }                    
                }
                else
                {
                    if(keyState == 1)
                    {
                        keyBuilder.Append(line[i]);
                    }

                    if(valueState == 1)
                    {
                        valueBuilder.Append(line[i]);
                    }
                }                
            }

            if(keyState != 2 || valueState != 2)
            {
                throw new MappingFileParsingException();
            }

            key = keyBuilder.ToString();
            value = valueBuilder.ToString();
        }

        private void LoadMappingsFromFile(string file)
        {
            string[] lines = File.ReadAllLines(file);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                try
                {
                    ReadFromLine(line, out string key, out string value);
                    mappings.Add(key, value);
                    Logger?.Log($"Mapping loaded: {key} : {value}");
                }
                catch(MappingFileParsingException)
                {
                    Logger?.Log($"Mapping loading failed on line {i}. Wrong syntax. Use \"<key>\" \"<value>\"");
                    throw;
                }                
            }
        }

        private string GetDefaultPath(string remotePath)
        {
            string localPath = Path.Combine(preferences.ClientStoragePath, remotePath);
            return localPath;
        }

        private static string CombinePathSegments(string[] segments, int depth)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < depth; i++)
            {
                stringBuilder.Append(segments[i]);
                if (i < depth - 1)
                {
                    stringBuilder.Append(Path.DirectorySeparatorChar);
                }
            }

            return stringBuilder.ToString();
        }

        private static string ReplaceSegments(string[] remoteSegments, string[] localSegments, int replaceCount)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for(int i = 0; i < remoteSegments.Length; i++)
            {
                if(i < replaceCount - localSegments.Length)
                {
                    continue;
                }
                if (i >= replaceCount - localSegments.Length && i < replaceCount)
                {
                    int index = i - (replaceCount - localSegments.Length);
                    stringBuilder.Append(localSegments[index]);
                }
                else
                {
                    stringBuilder.Append(remoteSegments[i]);
                }

                if(i < remoteSegments.Length - 1)
                {
                    stringBuilder.Append(Path.DirectorySeparatorChar);
                }
            }

            string result =  stringBuilder.ToString();
            return result;
        }

        public string GetLocalPath(string remotePath)
        {            
            string[] remotePathSegments = remotePath.Split(Path.DirectorySeparatorChar);

            string mappedLocalPath = null;            
            int depth;
            for (depth = remotePathSegments.Length; depth > 0; depth--)
            {
                string mapKey = CombinePathSegments(remotePathSegments, depth);
                bool hasMapping = mappings.TryGetValue(mapKey, out mappedLocalPath);
                if(hasMapping)
                {                    
                    break;
                }
            }
            
            if(mappedLocalPath == null)
            {
                return GetDefaultPath(remotePath);
            }

            string[] mappingSegments = mappedLocalPath.Split(Path.DirectorySeparatorChar);
            string reconstructedPath = ReplaceSegments(remotePathSegments, mappingSegments, depth);
            string localPath = Path.Combine(preferences.ClientStoragePath, reconstructedPath);

            return localPath;           
        }
    }
}
