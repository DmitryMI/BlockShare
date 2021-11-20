using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BlockShare.BlockSharing.BlockShareSecurity
{
    public static class SecurityUtils
    {
        public static X509Certificate2 CreateFromPkcs12(string pkcs12File)
        {
            if (!File.Exists(pkcs12File))
            {
                throw new FileNotFoundException($"Failed to load certificate from file {pkcs12File}: file does not exist");
            }

            X509Certificate2 cert = new X509Certificate2(pkcs12File);

            return cert;
        }

        public static void LogSecurityInfo(Action<string, int> Log, SslStream stream)
        {
            Log(String.Format("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength), 0);
            Log(String.Format("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength), 0);
            Log(String.Format("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength), 0);
            Log(String.Format("Protocol: {0}", stream.SslProtocol), 0);

            Log(String.Format("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer), 0);
            Log(String.Format("Is Signed: {0}", stream.IsSigned), 0);
            Log(String.Format("Is Encrypted: {0}", stream.IsEncrypted), 0);

            Log(String.Format("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite), 0);
            Log(String.Format("Can timeout: {0}", stream.CanTimeout), 0);

            Log(String.Format("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus), 0);

            X509Certificate localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                Log(String.Format("Local cert was issued to {0} and is valid from {1} until {2}.",
                    localCertificate.Subject,
                    localCertificate.GetEffectiveDateString(),
                    localCertificate.GetExpirationDateString()), 0);
            }
            else
            {
                Log(String.Format("Local certificate is null."), 0);
            }
            // Display the properties of the client's certificate.
            X509Certificate remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                Log(String.Format("Remote cert was issued to {0} and is valid from {1} until {2}.",
                    remoteCertificate.Subject,
                    remoteCertificate.GetEffectiveDateString(),
                    remoteCertificate.GetExpirationDateString()), 0);
            }
            else
            {
                Log(String.Format("Remote certificate is null."), 0);
            }

        }

        public static bool CompareCertificates(X509Certificate certificate1, X509Certificate certificate2)
        {
            byte[] cert1Hash = certificate1.GetCertHash();
            byte[] cert2Hash = certificate2.GetCertHash();
            return Utils.CompareBytes(cert1Hash, cert2Hash);
        }        

        public static bool VerifyCertificate(X509Certificate cert, X509Certificate root)
        { 
            X509Chain chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            X509Certificate2 root2 = new X509Certificate2(root);
            chain.ChainPolicy.ExtraStore.Add(root2);
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

            X509Certificate2 cert2 = new X509Certificate2(cert);
            var isValid = chain.Build(cert2);

            var chainRoot = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
            isValid = isValid && chainRoot.RawData.SequenceEqual(root.GetRawCertData());

            return isValid;
        }
    }
}
