using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.PreferencesManagement.Exceptions
{
    public class RequiredOptionMissingException : Exception
    {
        public string ConfigFilePath { get; set; }
        public string OptionName { get; set; }

        public RequiredOptionMissingException(string config, string option) : base($"Required option {option} is missing in config {config}")
        {
            ConfigFilePath = config;
            OptionName = option;
        }
    }
}
