using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BlockShare.BlockSharing.PreferencesManagement
{
    public class SecurityPreferences : IPreferencesSerializable
    {
        
        public SecurityMethod Method { get; set; }

        public string CertificateAuthorityPath { get; set; }
        public string ServerCertificatePath { get; set; }
        public string ClientCertificatePath { get; set; }
        public string AcceptedCertificatesDirectoryPath { get; set; }
        public string ServerName { get; set; }
        public string ClientName { get; set; }

        public SecurityPreferences(SecurityMethod method)
        {
            Method = method;
        }

        public SecurityPreferences()
        {
            Method = SecurityMethod.None;            
        }

        public object FromXmlElement(XmlElement xmlElement)
        {
            if(xmlElement == null)
            {
                Method = SecurityMethod.None;
                return this;
            }

            XmlElement methodElement = xmlElement["Method"];

            if (methodElement == null)
            {
                Method = SecurityMethod.None;
            }
            else
            {

                Method = (SecurityMethod)Enum.Parse(typeof(SecurityMethod), methodElement.InnerText);
            }

            XmlElement certificateAuthorityPath = xmlElement["CertificateAuthorityPath"];
            if(certificateAuthorityPath != null)
            {
                CertificateAuthorityPath = certificateAuthorityPath.InnerText;
            }
            XmlElement serverCertificatePath = xmlElement["ServerCertificatePath"];
            if (serverCertificatePath != null)
            {
                ServerCertificatePath = serverCertificatePath.InnerText;
            }
            XmlElement clientCertificatePath = xmlElement["ClientCertificatePath"];
            if (clientCertificatePath != null)
            {
                ClientCertificatePath = clientCertificatePath.InnerText;
            }
            XmlElement acceptedCertificatesDirectoryPath = xmlElement["AcceptedCertificatesDirectoryPath"];
            if (acceptedCertificatesDirectoryPath != null)
            {
                AcceptedCertificatesDirectoryPath = acceptedCertificatesDirectoryPath.InnerText;
            }
            XmlElement serverName = xmlElement["ServerName"];
            if (serverName != null)
            {
                ServerName = serverName.InnerText;
            }
            XmlElement clientName = xmlElement["ClientName"];
            if (clientName != null)
            {
                ClientName = clientName.InnerText;
            }

            return this;
        }

        public void ToXmlElement(XmlDocument doc, XmlElement xmlElement)
        {
            XmlElement methodElement = doc.CreateElement("Method");
            methodElement.InnerText = Method.ToString();
            xmlElement.AppendChild(methodElement);

            if(CertificateAuthorityPath != null)
            {
                XmlElement prefsElement = doc.CreateElement("CertificateAuthorityPath");
                prefsElement.InnerText = CertificateAuthorityPath;
                xmlElement.AppendChild(prefsElement);
            }
            if (ServerCertificatePath != null)
            {
                XmlElement prefsElement = doc.CreateElement("ServerCertificatePath");
                prefsElement.InnerText = ServerCertificatePath;
                xmlElement.AppendChild(prefsElement);
            }
            if (ClientCertificatePath != null)
            {
                XmlElement prefsElement = doc.CreateElement("ClientCertificatePath");
                prefsElement.InnerText = ClientCertificatePath;
                xmlElement.AppendChild(prefsElement);
            }
            if (AcceptedCertificatesDirectoryPath != null)
            {
                XmlElement prefsElement = doc.CreateElement("AcceptedCertificatesDirectoryPath");
                prefsElement.InnerText = AcceptedCertificatesDirectoryPath;
                xmlElement.AppendChild(prefsElement);
            }
            if (ServerName != null)
            {
                XmlElement prefsElement = doc.CreateElement("ServerName");
                prefsElement.InnerText = ServerName;
                xmlElement.AppendChild(prefsElement);
            }
            if (ClientName != null)
            {
                XmlElement prefsElement = doc.CreateElement("ClientName");
                prefsElement.InnerText = ClientName;
                xmlElement.AppendChild(prefsElement);
            }

        }
    }
}
