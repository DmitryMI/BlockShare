﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlServer.Server;

namespace BlockShare.BlockSharing
{
    static class Utils
    {
        public static void ReadPackage(Stream stream, byte[] package, int offset, int length, long timeoutMillis)
        {
            //Console.WriteLine($"[UTILS] Waiting for package of length: {length}");
            int bytesRead = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (bytesRead < length)
            {
                int stepOffset = offset + bytesRead;
                int stepLength = length - bytesRead;
                int stepBytes = stream.Read(package, stepOffset, stepLength);
                bytesRead += stepBytes;
                //Console.WriteLine($"[UTILS] Bytes read: {bytesRead}/{length}");
                if(timeoutMillis > 0)
                {
                    //Console.WriteLine($"[UTILS] Checking timeout:");
                    if (sw.ElapsedMilliseconds > timeoutMillis)
                    {
                        //Console.WriteLine($"[UTILS] TIMEOUT");
                        throw new TimeoutException();
                    }
                }
            }
            sw.Stop();
            //Console.WriteLine($"[UTILS] Package received");
        }

        public static string PrintHex(byte[] array, int offset, int length)
        {
            StringBuilder builder = new StringBuilder();
            for(int i = 0; i < length; i++)
            {
                builder.Append(array[i].ToString("X2"));
            }

            return builder.ToString();
        }

        public static void ForEachFsEntry<T>(string root, T state, Action<string, T> action)
        {
            if (File.Exists(root))
            {
                //Console.WriteLine($"[UTILS] Enumerated: {root}");
                action(root, state);
            }
            else if (Directory.Exists(root))
            {
                //Console.WriteLine($"[UTILS] Entered directory: {root}");
                var dirs = Directory.EnumerateDirectories(root);
                foreach (var dir in dirs)
                {
                    ForEachFsEntry(dir, state, action);
                }

                var files = Directory.EnumerateFiles(root);
                foreach (var file in files)
                {
                    //Console.WriteLine($"[UTILS] Enumerated: {file}");
                    action(file, state);
                }
            }
        }

        [Obsolete]
        public static string GenerateDirectoryDigest(DirectoryInfo directoryInfo, DirectoryInfo rootDirectoryInfo)
        {
            XmlDocument doc = new XmlDocument();

            //(1) the xml declaration is recommended, but not mandatory
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            //(2) string.Empty makes cleaner code
            XmlElement directoryDigestElement = doc.CreateElement(string.Empty, "DirectoryDigest", string.Empty);

            //XmlAttribute usesAbsolutePathsAttribute = doc.CreateAttribute("UsesAbsolutePaths");
            //usesAbsolutePathsAttribute.Value = cmdParams.UseAbsolutePathsForRefolding.ToString();
            //refoldingInfo.SetAttributeNode(usesAbsolutePathsAttribute);
            doc.AppendChild(directoryDigestElement);

            List<string> filesInDirectory = new List<string>();

            void AddFileToList(string file, object notInUse)
            {
                if (file.EndsWith(Preferences.HashlistExtension))
                {
                    return;
                }
                file = file.Replace(rootDirectoryInfo.FullName, "");
                if (file[0] == '\\' || file[0] == '/')
                {
                    file = file.Remove(0, 1);
                }
                filesInDirectory.Add(file);
            }

            ForEachFsEntry<object>(directoryInfo.FullName, null, AddFileToList);

            //Console.WriteLine($"[UTILS] All files enumerated. Generating xml...");

            foreach (string fileName in filesInDirectory)
            {
                //Console.WriteLine($"[UTILS] Appending {fileName} to xml...");
                XmlElement fileElement = doc.CreateElement(string.Empty, "File", string.Empty);
                fileElement.InnerText = fileName;
                directoryDigestElement.AppendChild(fileElement);
                //Console.WriteLine($"[UTILS] {fileName} appended to xml");
            }

            //Console.WriteLine($"[UTILS] Xml generated");
            return doc.OuterXml;
        }

        [Obsolete]
        public static string[] GetFileNamesFromDigest(XmlDocument xmlDigest)
        {
            XmlElement directoryDigestElement = xmlDigest["DirectoryDigest"];

            if (directoryDigestElement == null)
            {
                throw new ArgumentException("Directory Digest is malformed");
            }

            string[] fileNames = new string[directoryDigestElement.ChildNodes.Count];

            int index = 0;
            foreach (XmlElement fileElement in directoryDigestElement)
            {
                string fileName = fileElement.InnerText;

                fileNames[index] = fileName;
                index++;
            }

            return fileNames;
        }

        public static bool ArePathsEqual(string path1, string path2)
        {
            if (path1 == null && path2 == null)
            {
                return true;
            }

            if ((path1 == null) != (path2 == null))
            {
                return false;
            }

            if (path1[path1.Length - 1] == Path.DirectorySeparatorChar ||
                path1[path1.Length - 1] == Path.AltDirectorySeparatorChar)
            {
                path1 = path1.Remove(path1.Length - 1, 1);
            }
            if (path2[path2.Length - 1] == Path.DirectorySeparatorChar ||
                path2[path2.Length - 1] == Path.AltDirectorySeparatorChar)
            {
                path2 = path2.Remove(path2.Length - 1, 1);
            }

            return path1.Equals(path2);
        }

        public static void Dehash(string path)
        {
            void EnumerateFile(string file, object notInUse)
            {
                if (file.EndsWith(Preferences.HashlistExtension) || file.EndsWith(Preferences.HashpartExtension))
                {
                    Console.WriteLine($"Removing {file}...");
                    File.Delete(file);
                }
            }

            ForEachFsEntry<object>(path, null, EnumerateFile);
        }

        public static string ToSafeBase64(byte[] toEncodeAsBytes)
        {
            char[] padding = { '=' };
            string returnValue = System.Convert.ToBase64String(toEncodeAsBytes)
                .TrimEnd(padding).Replace('+', '-').Replace('/', '_');

            return returnValue;
        }

        public static byte[] FromSafeBase64(string base64)
        {
            string incoming = base64
                .Replace('_', '/').Replace('-', '+');
            switch (base64.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }
            byte[] bytes = Convert.FromBase64String(incoming);

            return bytes;
        }

        public static string FormatByteSize(long sizeInBytes)
        {
            if (sizeInBytes < 1024)
            {
                return $"{sizeInBytes} B";
            }

            if (sizeInBytes < 1024 * 1024)
            {
                double kib = sizeInBytes / 1024.0;
                return $"{kib:F1} KiB";
            }

            if (sizeInBytes < 1024 * 1024 * 1024)
            {
                double mib = sizeInBytes / 1024.0 / 1024.0;
                return $"{mib:F1} MiB";
            }

            double gib = sizeInBytes / 1024.0 / 1024.0 / 1024.0;
            return $"{gib:F1} GiB";
        }
    }
}
