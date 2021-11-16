using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.PreferencesManagement
{
    internal class CommandLineAliasAttribute : Attribute
    {
        public char? CharAlias { get; set; }
        public string StringAlias { get; set; }

        public CommandLineAliasAttribute(char aliasChar, string aliasString)
        {
            CharAlias = aliasChar;
            StringAlias = aliasString;
        }

        public CommandLineAliasAttribute(char aliasChar)
        {
            CharAlias = aliasChar;
            StringAlias = null;
        }

        public CommandLineAliasAttribute(string aliasString)
        {
            CharAlias = null;
            StringAlias = aliasString;
        }
    }
}
