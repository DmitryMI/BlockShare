using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.PreferencesManagement.Exceptions
{
    public class CommandLineParsingException : Exception
    {
        public string Argument { get; set; }

        public CommandLineParsingException(string argument) : base($"Unexpected sequence: {argument}")
        {
            Argument = argument;
        }
    }
}
