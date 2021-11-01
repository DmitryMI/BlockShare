using BlockShare.BlockSharing.BlockShareTypes;
using System;
using System.Xml;

namespace BlockShare.BlockSharing.HashMapping
{
    public class HashMapper : IPreferencesSerializable
    {
        public virtual string GetHashpartFile(string filePath)
        {
            throw new NotImplementedException();
        }
        public virtual string GetHashlistFile(string filePath)
        {
            throw new NotImplementedException();
        }

        public virtual void ToXmlElement(XmlDocument doc, XmlElement xmlElement)
        {
            XmlAttribute attributeType = doc.CreateAttribute("HashMapperType");
            attributeType.Value = GetType().FullName;
            xmlElement.SetAttributeNode(attributeType);
        }

        public virtual object FromXmlElement(XmlElement xmlElement)
        {
            string typeName = xmlElement.GetAttribute("HashMapperType");
            Type type = Type.GetType(typeName);
            HashMapper hashMapper = (HashMapper)Activator.CreateInstance(type);
            hashMapper = (HashMapper)hashMapper.FromXmlElement(xmlElement);
            return hashMapper;
        }
    }
}
