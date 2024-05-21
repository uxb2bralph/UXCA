using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using ContractHome.Models.Dto;
using Org.BouncyCastle.Ocsp;
using DocumentFormat.OpenXml.Drawing;

namespace ContractHome.Helper
{
    public class IdentityCertHelper
    {


        private static X509Certificate2 X509Cert;
        private static string begin = "-----BEGIN CERTIFICATE-----";
        private static string end = "-----END CERTIFICATE-----";


        public IdentityCertHelper(string x509PemString)
        {
            X509PemString = x509PemString;
            if (!x509PemString.Contains(begin))
            {
                X509PemString = $"{begin}{x509PemString}{end}";
            }
            
            try
            {
                X509Cert = X509Certificate2.CreateFromPem(X509PemString);
            }
            catch (Exception)
            {
                X509Cert = null;
            }

        }


        public string X509PemString { get; }
        public bool IsCorporateCert => X509Cert.Issuer.Contains("工商憑證");

        public bool IsSubjectMatch(string containKeyWord)
        {
            if (string.IsNullOrEmpty(containKeyWord)) { return false; }
            if (X509Cert == null) return false;
            return X509Cert.Subject.Contains(containKeyWord);
        }

        public bool IsSignatureValid(string tbs, string Signature)
        {
            if (X509Cert == null) return false;
            if (!GeneralValidator.TryFromBase64String(Signature)) return false;
            try
            {
                RSA rsa = X509Cert.GetRSAPublicKey();
                string sha256Oid = CryptoConfig.MapNameToOID(nameof(SHA256));
                byte[] bytePKCS1Signature = Convert.FromBase64String(Signature);
                byte[] byteTbs = Encoding.UTF8.GetBytes(tbs);
                bool isValid = rsa.VerifyData(byteTbs, bytePKCS1Signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return isValid;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public string GetCertType()
        {
            return (X509Cert.Issuer.Contains("工商")) ? "O" : "P";
        }

    }
}
