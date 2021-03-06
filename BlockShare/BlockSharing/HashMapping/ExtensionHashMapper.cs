using BlockShare.BlockSharing.PreferencesManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BlockShare.BlockSharing.HashMapping
{
    public class ExtensionHashMapper : HashMapper
    {
        public override string GetHashpartFile(string filePath)
        {
            string fileHashPartPath = filePath + Preferences.HashpartExtension;
            return fileHashPartPath;
        }

        public override string GetHashlistFile(string filePath)
        {
            string fileHashListPath = filePath + Preferences.HashlistExtension;
            return fileHashListPath;
        }

        public override void ToXmlElement(XmlDocument doc, XmlElement xmlElement)
        {
            base.ToXmlElement(doc, xmlElement);
        }

        public override object FromXmlElement(XmlElement xmlElement)
        {
            return this;
        }
    }
}
