using BlockShare.BlockSharing.PreferencesManagement.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BlockShare.BlockSharing.PreferencesManagement
{
    public class PreferencesManager<T> where T:new()
    {
        private Dictionary<PropertyInfo, bool> requiredOptionsStateDictionary = new Dictionary<PropertyInfo, bool>();        

        public T Preferences { get; }
        
        public PreferencesManager()
        {
            Type type = typeof(T);
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo propertyInfo in properties)
            {
                PreferenceParameterAttribute preferenceParameterAttribute = propertyInfo.GetCustomAttribute<PreferenceParameterAttribute>();
                if(preferenceParameterAttribute == null)
                {
                    continue;
                }
                if(preferenceParameterAttribute.IsRequired)
                {
                    requiredOptionsStateDictionary.Add(propertyInfo, false);
                }
            }

            Preferences = new T();
        }

        public bool HasMissingRequiredOptions()
        {
            return GetMissingRequiredOptions().Count != 0;
        }

        public IReadOnlyList<string> GetMissingRequiredOptions()
        {
            List<string> options = new List<string>();
            foreach(var entry in requiredOptionsStateDictionary)
            {
                if(entry.Value == false)
                {
                    options.Add(entry.Key.Name);
                }
            }

            return options;
        }

        public void LoadPreferences(string configFilePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(configFilePath);

            XmlElement rootElement = doc["Preferences"];

            Type type = typeof(T);
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo propertyInfo in properties)
            {
                XmlElement xmlElement = rootElement[propertyInfo.Name];
                if (xmlElement == null)
                {
                    continue;
                }
                if (IsBasicType(propertyInfo.PropertyType))
                {
                    object value = DeserializeBasicType(xmlElement, propertyInfo.PropertyType);
                    propertyInfo.SetValue(Preferences, value);
                }
                else if (IsEnumType(propertyInfo.PropertyType))
                {
                    object value = DeserializeEnumType(xmlElement.InnerText, propertyInfo.PropertyType);
                    propertyInfo.SetValue(Preferences, value);
                }
                else if (IsPreferencesSerializable(propertyInfo.PropertyType))
                {
                    IPreferencesSerializable serializable = (IPreferencesSerializable)Activator.CreateInstance(propertyInfo.PropertyType);
                    object value = serializable.FromXmlElement(xmlElement);
                    propertyInfo.SetValue(Preferences, value);
                }
                else
                {
                    throw new PreferenceTypeNotSupportedException(typeof(T), propertyInfo.PropertyType, propertyInfo.Name);
                }

                if(requiredOptionsStateDictionary.ContainsKey(propertyInfo))
                {
                    requiredOptionsStateDictionary[propertyInfo] = true;
                }
            }
        }

        public void SavePreferences(string configFilePath)
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
                if (IsBasicType(propertyInfo.PropertyType))
                {
                    object value = propertyInfo.GetValue(Preferences);
                    XmlElement xmlElement = SerializeBasicType(doc, propertyInfo.Name, value);
                    rootElement.AppendChild(xmlElement);
                }
                else if (IsEnumType(propertyInfo.PropertyType))
                {
                    object value = propertyInfo.GetValue(Preferences);
                    XmlElement xmlElement = SerializeEnumType(doc, propertyInfo.Name, value);
                    rootElement.AppendChild(xmlElement);
                }
                else if (IsPreferencesSerializable(propertyInfo.PropertyType))
                {
                    object value = propertyInfo.GetValue(Preferences);
                    IPreferencesSerializable preferencesSerializable = (IPreferencesSerializable)value;
                    XmlElement xmlElement = doc.CreateElement(propertyInfo.Name);
                    preferencesSerializable.ToXmlElement(doc, xmlElement);
                    rootElement.AppendChild(xmlElement);
                }
                else
                {
                    throw new PreferenceTypeNotSupportedException(typeof(T), propertyInfo.PropertyType, propertyInfo.Name);
                }
            }

            doc.AppendChild(rootElement);
            doc.Save(configFilePath);
        }

        private static string GetValueFromArgs(string propertyName, string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.Length < 2)
                {
                    continue;
                    //throw new CommandLineParsingException(arg);
                }
                if (arg[0] == '-' && arg[1] == '-')
                {
                    string strAlias = arg.Substring(2);
                    if (strAlias == propertyName)
                    {
                        return args[i + 1];
                    }
                }
            }

            return null;
        }

        private static string GetValueFromArgs(CommandLineAliasAttribute aliasAttribute, string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.Length < 2)
                {
                    continue;
                    //throw new CommandLineParsingException(arg);
                }
                if (arg[0] == '-' && arg[1] != '-')
                {
                    char c = arg[1];
                    if (aliasAttribute.CharAlias == c)
                    {
                        return args[i + 1];
                    }
                }
                else if (arg[1] == '-')
                {
                    string strAlias = arg.Substring(2);
                    if (strAlias == aliasAttribute.StringAlias)
                    {
                        return args[i + 1];
                    }
                }
            }

            return null;
        }

        public static List<AliasInfo> GetCommandLineAliases()
        {
            Type type = typeof(T);
            PropertyInfo[] properties = type.GetProperties();

            List<AliasInfo> result = new List<AliasInfo>();

            foreach (PropertyInfo propertyInfo in properties)
            {
                CommandLineAliasAttribute aliasAttribute = propertyInfo.GetCustomAttribute<CommandLineAliasAttribute>();
                if (aliasAttribute == null)
                {
                    continue;
                }
                AliasInfo aliasInfo = new AliasInfo(aliasAttribute.CharAlias, aliasAttribute.StringAlias, propertyInfo);
                result.Add(aliasInfo);
            }

            return result;
        }

        public void ParseCommandLine(string[] args)
        {
            Type type = typeof(T);
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo propertyInfo in properties)
            {
                string strValue = null;
                CommandLineAliasAttribute aliasAttribute = propertyInfo.GetCustomAttribute<CommandLineAliasAttribute>();
                if (aliasAttribute == null)
                {
                    strValue = GetValueFromArgs(propertyInfo.Name, args);
                }
                else
                {
                    strValue = GetValueFromArgs(aliasAttribute, args);
                }

                if (strValue == null)
                {
                    continue;
                }

                if (IsBasicType(propertyInfo.PropertyType))
                {
                    object value = DeserializeBasicType(strValue, propertyInfo.PropertyType);
                    propertyInfo.SetValue(Preferences, value);
                }
                else if (IsEnumType(propertyInfo.PropertyType))
                {
                    object value = DeserializeEnumType(strValue, propertyInfo.PropertyType);
                    propertyInfo.SetValue(Preferences, value);
                }
                else if (IsPreferencesSerializable(propertyInfo.PropertyType))
                {
                    throw new NotImplementedException("PreferencesSerializable types can not be deserialized from command line");
                }
                else
                {
                    throw new NotImplementedException("Complex types can not be deserialized from command line");
                }

                if (requiredOptionsStateDictionary.ContainsKey(propertyInfo))
                {
                    requiredOptionsStateDictionary[propertyInfo] = true;
                }
            }
        }


        #region Utils
        private static XmlElement SerializeBasicType(XmlDocument document, string name, object value)
        {
            XmlElement element = document.CreateElement(name);
            element.InnerText = value.ToString();
            return element;
        }

        private static XmlElement SerializeEnumType(XmlDocument document, string name, object value)
        {
            XmlElement element = document.CreateElement(name);
            element.InnerText = value.ToString();
            return element;
        }

        private static object DeserializeBasicType(XmlElement xmlElement, Type type)
        {
            if (type == typeof(string))
            {
                return xmlElement.InnerText;
            }
            MethodInfo parser = type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, new ParameterModifier[0]);

            string serializedValue = xmlElement.InnerText;
            object result = parser.Invoke(null, new object[] { serializedValue });
            return result;
        }

        private static object DeserializeBasicType(string serializedValue, Type type)
        {
            if (type == typeof(string))
            {
                return serializedValue;
            }
            MethodInfo parser = type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, new ParameterModifier[0]);
            object result = parser.Invoke(null, new object[] { serializedValue });
            return result;
        }

        private static bool IsBasicType(Type type)
        {
            if (type == typeof(string))
            {
                return true;
            }
            MethodInfo parser = type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, new ParameterModifier[0]);
            return parser != null;
        }

        private static bool IsEnumType(Type type)
        {
            return type.IsEnum;
        }

        private static object DeserializeEnumType(string serializedValue, Type type)
        {
            object value = Enum.Parse(type, serializedValue);
            return value;
        }

        private static bool IsPreferencesSerializable(Type type)
        {
            Type preferencesSerializableInterface = typeof(IPreferencesSerializable);
            if (preferencesSerializableInterface.IsAssignableFrom(type))
            {
                return true;
            }
            return false;
        }

        #endregion

    }
}
