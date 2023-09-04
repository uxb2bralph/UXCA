using CommonLib.Utility.Properties;
using CommonLib.Utility;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Security.Permissions;
using System.Text;
using System.Xml;
using TWCACAPIAdapter.Models.ViewModel;
using CommonLib.Core.Utility;
using System.Net;

namespace TWCACAPIAdapter.Helper
{
    public class Signable
    {
        private X509Certificate2? _signerCert;

        public Signable()
        {

        }

        public bool SignCms(string message)
        {

            bool bResult = false;

            try
            {

                if (!chooseSignerCert())
                {
                    ErrorMessage = "無法取得簽署憑證";
                }
                else if (String.IsNullOrEmpty(message))
                {
                    ErrorMessage = "簽章本文為空白";
                }
                else
                {
                    ContentInfo content = new ContentInfo(Encoding.Default.GetBytes(message));
                    SignedCms signedCms = new SignedCms(content, true);
                    CmsSigner signer = new CmsSigner(_signerCert);

                    signedCms.ComputeSignature(signer, false);
                    bResult = true;
                    SignedMessage = Convert.ToBase64String(signedCms.Encode());
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.ToString();
            }

            return bResult;
        }

        public bool SignXml(string xmlMsg)
        {
            bool bResult = false;

            try
            {
                if (!chooseSignerCert())
                {
                    ErrorMessage = "無法取得簽署憑證";
                }
                else if (String.IsNullOrEmpty(xmlMsg))
                {
                    ErrorMessage = "簽章本文為空白";
                }
                else
                {
                    XmlDocument docMsg = new XmlDocument();
                    docMsg.PreserveWhitespace = true;
                    docMsg.LoadXml(xmlMsg);

                    if (signXml(docMsg))
                    {
                        bResult = true;
                        SignedMessage = docMsg.OuterXml;
                    }
                    else
                    {
                        ErrorMessage = "簽章無法完成";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.ToString();
            }

            return bResult;
        }

        private bool chooseSignerCert()
        {
            return chooseSignerCertFromStore();
        }

        public X509Certificate2Collection GetSignerCertificateCollection()
        {
            X509Store certStore = new X509Store(StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection certificates;
            if (String.IsNullOrEmpty(CertFilter))
            {
                certificates = certStore.Certificates;
            }
            else
            {
                certificates = new X509Certificate2Collection();
                foreach (X509Certificate2 cert in certStore.Certificates)
                {
                    if (cert.Subject.IndexOf(CertFilter) >= 0)
                    {
                        certificates.Add(cert);
                    }
                }
            }

            certStore.Close();
            return certificates;
        }


        private bool chooseSignerCertFromStore()
        {
            bool bResult = false;
            X509Store certStore = new X509Store(StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadOnly);

            if (!String.IsNullOrEmpty(Thumbprint))
            {
                foreach (X509Certificate2 cert in certStore.Certificates)
                {
                    if (cert.Thumbprint == Thumbprint)
                    {
                        _signerCert = new X509Certificate2(cert);
                        bResult = true;
                        break;
                    }
                }
            }
            else
            {

                X509Certificate2Collection certificates;
                if (String.IsNullOrEmpty(CertFilter))
                {
                    certificates = certStore.Certificates;
                }
                else
                {
                    certificates = new X509Certificate2Collection();
                    foreach (X509Certificate2 cert in certStore.Certificates)
                    {
                        if (cert.Subject.IndexOf(CertFilter) >= 0)
                        {
                            certificates.Add(cert);
                        }
                    }
                }

                X509CertificateCollection certUsed = X509Certificate2UI.SelectFromCollection(certificates, "選取簽章憑證", "請選取您用來進行電子簽章的數位憑證", X509SelectionFlag.SingleSelection, GetForegroundWindow());
                if (certUsed.Count > 0)
                {
                    _signerCert = new X509Certificate2(certUsed[0]);
                    bResult = true;
                }
            }
            certStore.Close();
            return bResult;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private bool signXml(XmlDocument docMsg)
        {
            if (_signerCert == null)
            {
                return false;
            }

            SignedXml signedXml = new SignedXml(docMsg);
            signedXml.SigningKey = _signerCert.GetRSAPrivateKey();  //.PrivateKey;

            Reference reference = new Reference();
            reference.Uri = "";

            //Add an enveloped transformation to the reference.
            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);

            // Add the reference to the SignedXml object.
            signedXml.AddReference(reference);

            // Add an RSAKeyValue KeyInfo (optional; helps recipient find key to validate).
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(_signerCert));
            signedXml.KeyInfo = keyInfo;

            signedXml.ComputeSignature();
            XmlElement xmlDigitalSignature = signedXml.GetXml();

            // Append the element to the XML document.
            docMsg.DocumentElement?.AppendChild(docMsg.ImportNode(xmlDigitalSignature, true));

            return true;
        }


        public string? CertFilter
        {
            get;
            set;
        }

        public string? ErrorMessage
        {
            get;
            private set;
        }


        public string? SignedMessage
        {
            get;
            private set;
        }

        public string? Thumbprint
        {
            get;
            set;
        }


        public bool AppendSignerInfo
        {
            get;
            set;
        }

        //public void PushSignature(TWCASignDataViewModel viewModel)
        //{
        //    if (viewModel.RemoteHost != null)
        //    {
        //        try
        //        {
        //            using WebClientEx client = new WebClientEx();
        //            client.Timeout = 43200000;
        //            client.Encoding = Encoding.UTF8;
        //            client.Headers[HttpRequestHeader.ContentType] = "application/json";

        //            var dataItem = viewModel.JsonStringify();
        //            String result = client.UploadString(viewModel.RemoteHost, dataItem);
        //            FileLogger.Logger.Info($"PushSignature:{dataItem},result:{result}");
        //        }
        //        catch(Exception ex)
        //        {
        //            FileLogger.Logger.Error(ex);
        //        }
        //    }
        //}
    }

}
