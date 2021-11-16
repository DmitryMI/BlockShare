using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.PreferencesManagement
{
    public class AliasInfo
    {
        public char? CharAlias { get; set; }
        public string StringAlias { get; set; }
        public PropertyInfo PropertyInfo { get; set; }

        public AliasInfo(char? c, string s, PropertyInfo propertyInfo)
        {
            CharAlias = c;
            StringAlias = s;
            PropertyInfo = propertyInfo;
        }
    }
}
