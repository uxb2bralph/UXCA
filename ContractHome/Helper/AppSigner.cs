using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text;
using System.Xml;

using CommonLib.Utility;
using System.Security.Cryptography.Pkcs;
using CommonLib.Security.UseCrypto;

namespace ContractHome.Helper
{
    public class AppSigner
    {
        private const String __CERT_FILE = "DefaultSigner.bin";

        private X509Certificate2 _signerCert;
        private static AppSigner _instance = new AppSigner();

        private AppSigner()
        {

        }

        private String certPath
        {
            get
            {
                return Path.Combine(CommonLib.Core.Utility.FileLogger.Logger.LogPath, __CERT_FILE);
            }
        }

        private void prepareCertificate()
        {
            if (File.Exists(certPath))
            {
                using (FileStream fs = new FileStream(certPath, FileMode.Open, FileAccess.Read))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        fs.CopyTo(ms);
                        CipherDecipherSrv cipher = new CipherDecipherSrv();
                        var certBytes = cipher.decipherCode(ms.ToArray());
                        _signerCert = new X509Certificate2(certBytes);
                        ms.Close();
                    }
                    fs.Close();
                }
            }
        }


        public static bool Sign(XmlDocument docMsg)
        {
            if (AppSigner.SignerCertificate != null)
            {
                return CryptoUtility.SignXml(docMsg, null, null, AppSigner.SignerCertificate);
            }
            return false;
        }

        public static SignedCms SignCms(String dataToSign)
        {
            if (AppSigner.SignerCertificate != null)
            {
                ContentInfo content = new ContentInfo(Encoding.Default.GetBytes(dataToSign));
                SignedCms signedCms = new SignedCms(content, false);
                CmsSigner signer = new CmsSigner(AppSigner.SignerCertificate);

                signedCms.ComputeSignature(signer, true);
                return signedCms;
            }
            return null;
        }

        public static void UpdateSigner(byte[] certBytes)
        {
            using (FileStream fs = new FileStream(_instance.certPath, FileMode.Create, FileAccess.Write))
            {
                var buf = (new CipherDecipherSrv()).cipherCode(certBytes);
                fs.Write(buf, 0, buf.Length);
                fs.Flush();
                fs.Close();
            }

            _instance.prepareCertificate();
        }

        public static X509Certificate2 SignerCertificate
        {
            get
            {
                if (_instance._signerCert == null)
                    _instance.prepareCertificate();
                return _instance._signerCert;
            }
        }
    }
}
