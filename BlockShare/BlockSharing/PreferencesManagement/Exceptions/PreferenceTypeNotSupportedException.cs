using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.PreferencesManagement.Exceptions
{
    public class PreferenceTypeNotSupportedException : Exception
    {
        public Type PreferencesClassType { get; set; }
        public Type OptionType { get; set; }
        public string OptionName { get; set; }

        public PreferenceTypeNotSupportedException(Type preferencesClassType, Type optionType, string optionName) : base($"Preferences class ({preferencesClassType.Name}) contains option ({optionName}) with unsupported type {optionType.Name}")
        {
            PreferencesClassType = preferencesClassType;
            OptionType = optionType;
            OptionName = optionName;
        }
    }
}
