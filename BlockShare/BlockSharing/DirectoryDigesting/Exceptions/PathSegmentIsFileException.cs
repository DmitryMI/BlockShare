using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.DirectoryDigesting.Exceptions
{
    public class PathSegmentIsFileException : Exception
    {
        public string RequestPath { get; set; }
        public string ErrorPath { get; set; }
        public PathSegmentIsFileException(string requestedPath, string pathToErrorEntry) : base($"Requested entry {requestedPath} does not exist, because {pathToErrorEntry} is not a directory")
        {
            RequestPath = requestedPath;
            ErrorPath = pathToErrorEntry;
        }
    }
}
