using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BlockShare.BlockSharing.PreferencesManagement
{
    public static class PreferencesManager
    {
        public static Preferences LoadPreferences(string configFilePath)
        {
            Preferences preferences = new Preferences();

            XmlDocument doc = new XmlDocument();
            doc.Load(configFilePath);

            XmlElement rootElement = doc["Preferences"];

            Type type = typeof(Preferences);
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo propertyInfo in properties)
            {
                if (IsBasicType(propertyInfo.PropertyType))
                {
                    XmlElement xmlElement = rootElement[propertyInfo.Name];
                    object value = DeserializeBasicType(xmlElement, propertyInfo.PropertyType);
                    propertyInfo.SetValue(preferences, value);
                }
                else if (IsPreferencesSerializable(propertyInfo.PropertyType))
                {
                    XmlElement xmlElement = rootElement[propertyInfo.Name];
                    IPreferencesSerializable serializable = (IPreferencesSerializable)Activator.CreateInstance(propertyInfo.PropertyType);
                    object value = serializable.FromXmlElement(xmlElement);
                    propertyInfo.SetValue(preferences, value);
                }
            }

            return preferences;
        }

        private static XmlElement SerializeBasicType(XmlDocument document, string name, object value)
        {
            XmlElement element = document.CreateElement(name);
            element.InnerText = value.ToString();
            return element;
        }

        private static object DeserializeBasicType(XmlElement xmlElement, Type type)
        {
            if(type == typeof(string))
            {
                return xmlElement.InnerText;
            }
            MethodInfo parser = type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, new ParameterModifier[0]);

            string serializedValue = xmlElement.InnerText;
            object result = parser.Invoke(null, new object[] { serializedValue });
            return result;
        }

        private static T DeserializeBasicType<T>(XmlElement xmlElement)
        {
            Type type = typeof(T);
            return (T)DeserializeBasicType(xmlElement, type);
        }

        private static bool IsBasicType(Type type)
        {
            if(type == typeof(string))
            {
                return true;
            }
            MethodInfo parser = type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, new ParameterModifier[0]);
            return parser != null;
        }

        private static bool IsPreferencesSerializable(Type type)
        {
            Type preferencesSerializableInterface = typeof(IPreferencesSerializable);
            if(preferencesSerializableInterface.IsAssignableFrom(type))
            {
                return true;
            }
            return false;
        }

        public static void SavePreferences(Preferences preferences, string configFilePath)
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            XmlElement rootElement = doc.CreateElement("Preferences");

            Type type = typeof(Preferences);
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo propertyInfo in properties)
            {
                if(IsBasicType(propertyInfo.PropertyType))
                {
                    object value = propertyInfo.GetValue(preferences);
                    XmlElement xmlElement = SerializeBasicType(doc, propertyInfo.Name, value);
                    rootElement.AppendChild(xmlElement);
                }
                else if(IsPreferencesSerializable(propertyInfo.PropertyType))
                {
                    object value = propertyInfo.GetValue(preferences);
                    IPreferencesSerializable preferencesSerializable = (IPreferencesSerializable)value;
                    XmlElement xmlElement = doc.CreateElement(propertyInfo.Name);
                    preferencesSerializable.ToXmlElement(doc, xmlElement);
                    rootElement.AppendChild(xmlElement);
                }
            }

            doc.AppendChild(rootElement);
            doc.Save(configFilePath);
        }
    }
}
