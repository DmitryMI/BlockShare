using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.PreferencesManagement
{
    class PreferenceParameterAttribute : Attribute
    {
        public bool IsRequired { get; set; }

        public PreferenceParameterAttribute()
        {
            IsRequired = false;
        }

        public PreferenceParameterAttribute(bool isRequired)
        {
            IsRequired = isRequired;
        }
    }
}
