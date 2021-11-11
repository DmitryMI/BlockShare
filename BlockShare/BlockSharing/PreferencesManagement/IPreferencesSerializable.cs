using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BlockShare.BlockSharing.PreferencesManagement
{
    public interface IPreferencesSerializable
    {
        void ToXmlElement(XmlDocument doc, XmlElement xmlElement);

        object FromXmlElement(XmlElement xmlElement);
    }
}
