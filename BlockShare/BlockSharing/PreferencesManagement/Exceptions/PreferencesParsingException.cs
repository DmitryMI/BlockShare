using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.PreferencesManagement.Exceptions
{
    public class PreferencesParsingException : Exception
    {
        public string ConfigFilePath { get; set; }
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }
        

        public PreferencesParsingException(string config, int line, int column) : base($"Unexpected character in file {config} on {line}:{column}")
        {
            ConfigFilePath = config;
            LineNumber = line;
            ColumnNumber = column;
        }
    }
}
