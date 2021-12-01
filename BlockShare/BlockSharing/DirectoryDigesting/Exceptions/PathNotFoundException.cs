using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.DirectoryDigesting.Exceptions
{
    public class PathNotFoundException : Exception
    {
        public string Path { get; set; }
        public PathNotFoundException(string path) : base($"{path} not found")
        {
            Path = path;
        }
    }
}
