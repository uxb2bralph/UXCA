using CERTENROLLLib;
using CommonLib.Core.Utility;
using Microsoft.AspNetCore.Mvc;
using TWCACAPIAdapter.Models.ViewModel;
using TWCACAPIXWrapper;
using CommonLib.Utility;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using TWCACAPIAdapter.Helper;
using System.Net;

namespace TWCACAPIAdapter.Controllers
{
    public class UXPKIController : Controller
    {
        public static String? DataToSign { private set; get; }
        public static String? DataSignature { private set; get; }
        public static String? ErrorMessage { private set; get; }

        public IActionResult Index(TWCASignDataViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            return Content("Index");
		}

        public IActionResult Ping()
        {
            return View("~/Views/UXPKI/CloseWindow.cshtml");
        }
        public async Task<IActionResult> PostSignatureAsync()
        {
			await Request.SaveAsAsync(Path.Combine(FileLogger.Logger.LogDailyPath, $"{DateTime.Now.Ticks}.txt"), true);
            return View("~/Views/UXPKI/PostSignature.cshtml");
        }

        public IActionResult Sign(TWCASignDataViewModel viewModel)
        {
			ViewBag.ViewModel = viewModel;
            ErrorMessage = "已取消簽章";

            Signable signable = new Signable();
			viewModel.Subject= viewModel.Subject.GetEfficientString();
			signable.CertFilter = viewModel.Subject;

			return View("~/Views/UXPKI/Sign.cshtml", signable.GetSignerCertificateCollection());
        }

		public IActionResult SignCms(TWCASignDataViewModel viewModel)
		{
            Signable capi = new Signable();
            try
            {
				DataSignature = DataToSign = null;
                capi.Thumbprint = viewModel.Thumbprint;
                if (capi.SignCms(viewModel.DataToSign))
                {
                    viewModel.DataSignature = capi.SignedMessage;
                    DataToSign = viewModel.DataToSign;
                    DataSignature = viewModel.DataSignature;
					ErrorMessage = null;
                    FileLogger.Logger.Info(viewModel.DataToSign);
                    FileLogger.Logger.Info(viewModel.DataSignature);
                    //capi.PushSignature(viewModel);
                }
                else
                {
                    viewModel.ErrorMessage = capi.ErrorMessage;
					ErrorMessage = capi.ErrorMessage;
                }

                FileLogger.Logger.Info(viewModel.JsonStringify());

            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex);
            }

            return View("~/Views/UXPKI/SignCms.cshtml");
        }

        public IActionResult SignXml([FromBody] TWCASignDataViewModel viewModel)
		{
			Signable capi = new Signable();
			try
			{
				if(capi.SignXml(viewModel.DataToSign))
                {
                    return Json(new { dataSignature = capi.SignedMessage, errorCode = 0 });
                }
                else
                {
					return Json(new { errorCode = -1, errorMessage = capi.ErrorMessage });
				}
			}
			catch (Exception ex)
			{
				FileLogger.Logger.Error(ex);
				return Json(new { errorCode = -1, errorMessage = ex.Message });

			}
		}

		public IActionResult CreatePKCS10([FromBody] TWCAPKCSViewModel viewModel)
        {
			try
			{
				CCspInformationClass objCSP = new CCspInformationClass();
				CCspInformationsClass objCSPs = new CCspInformationsClass();
				CX509PrivateKeyClass objPrivateKey = new CX509PrivateKeyClass();
				CX509CertificateRequestPkcs10Class objRequest = new CX509CertificateRequestPkcs10Class();
				//var objRequest1 = objCertEnrollClassFactory.CreateObject("X509Enrollment.CX509CertificateRequestPkcs10V2")
				CObjectIdsClass objObjectIds = new CObjectIdsClass();
				CObjectIdClass objObjectId = new CObjectIdClass();
				CX509ExtensionEnhancedKeyUsageClass objX509ExtensionEnhancedKeyUsage = new CX509ExtensionEnhancedKeyUsageClass();
				CX509ExtensionTemplateNameClass objExtensionTemplate = new CX509ExtensionTemplateNameClass();

				CX500DistinguishedNameClass objDn = new CX500DistinguishedNameClass();

				CX509EnrollmentClass objEnroll = new CX509EnrollmentClass();

				var domain = viewModel.FirstName.GetEfficientString() ?? "";
				var organizationUnit = viewModel.OU;
				var organization = viewModel.O;
				objPrivateKey.ExportPolicy = viewModel.KeyStore == KeyStoreType.eToken
					? X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_NONE
					: X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_FLAG;	//	1;            
																				//	0:不可以匯出,1:可以匯出
				var country = viewModel.Country;
				//var commonName = document.proform.mail_firstName.value.trim() ;
				//commonName = commonName.toUpperCase;
				//domain= domain.toUpperCase;
				//  Initialize the csp object using the desired Cryptograhic Service Provider (CSP)
				if(viewModel.KeyStore == KeyStoreType.eToken)
                {
					objCSP.InitializeFromName("eToken Base Cryptographic Provider");
				}
				else
                {
					objCSP.InitializeFromName("Microsoft Enhanced Cryptographic Provider v1.0");
                }

				//  Add this CSP object to the CSP collection object
				objCSPs.Add(objCSP);

				//  Provide key container name, key length and key spec to the private key object
				//objPrivateKey.ContainerName = "AlejaCMa";
				objPrivateKey.Length = 2048;
				objPrivateKey.KeySpec = X509KeySpec.XCN_AT_KEYEXCHANGE; //1; // AT_KEYEXCHANGE = 1

				//  Provide the CSP collection object (in this case containing only 1 CSP object)
				//  to the private key object
				objPrivateKey.CspInformations = objCSPs;

				if(viewModel.KeyStore == KeyStoreType.eToken)
                {
					objPrivateKey.ProviderName = "eToken Base Cryptographic Provider";
					objPrivateKey.ProviderType = X509ProviderType.XCN_PROV_RSA_FULL;	//"1";
				}
				// Initialize P10 based on private key
				objRequest.InitializeFromPrivateKey(X509CertificateEnrollmentContext.ContextUser, objPrivateKey, ""); // context user = 1

				// 1.3.6.1.5.5.7.3.2 Oid - Extension
				objObjectId.InitializeFromValue("1.3.6.1.4.1.311.2.1.21");
				objObjectIds.Add(objObjectId);
				objX509ExtensionEnhancedKeyUsage.InitializeEncode(objObjectIds);
				objRequest.X509Extensions.Add((CX509Extension)objX509ExtensionEnhancedKeyUsage);

				// 1.3.6.1.5.5.7.3.3 Oid - Extension
				//objExtensionTemplate.InitializeEncode("1.3.6.1.5.5.7.3.3");
				//objRequest.X509Extensions.Add(objExtensionTemplate);

				// DN related stuff
				var strDN = $"CN={domain},OU={organizationUnit},O={organization},C={country}";
				objDn.Encode(strDN, 0);
				//objDn.Encode("CN="+commonName, 0); // XCN_CERT_NAME_STR_NONE = 0
				objRequest.Subject = objDn;

				// Enroll
				objEnroll.InitializeFromRequest(objRequest);
				var p10 = objEnroll.CreateRequest(EncodingType.XCN_CRYPT_STRING_BASE64REQUESTHEADER); // XCN_CRYPT_STRING_BASE64REQUESTHEADER = 3
				return Json(new { Pkcs10 = p10 });
			}
			catch(Exception ex)
            {
				FileLogger.Logger.Error(ex);
				return Json(new { Pkcs10 = "" });
			}
		}

		public IActionResult BuildCertificate([FromBody] TWCAPKCSViewModel viewModel)
        {
			if (viewModel.CSR != null)
			{
				String fileName = DateTime.Now.Ticks.ToString();
				String csrPath = Path.Combine(FileLogger.Logger.LogDailyPath, $"{fileName}.csr");
				String crtPath = Path.Combine(FileLogger.Logger.LogDailyPath, $"{fileName}.crt");
				if(viewModel.CSR.StartsWith("-----"))
                {
					System.IO.File.WriteAllText(csrPath, viewModel.CSR);
                }
				else
                {
					System.IO.File.WriteAllText(csrPath, "-----BEGIN NEW CERTIFICATE REQUEST-----\r\n");
					System.IO.File.AppendAllText(csrPath, viewModel.CSR.GetEfficientString());
					System.IO.File.AppendAllText(csrPath, "\r\n-----END NEW CERTIFICATE REQUEST-----");
				}

				String batch = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "build-cert.bat");
				System.IO.File.WriteAllText(batch, $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "openssl.exe")} x509 -req -days 7300 -out {crtPath} -in {csrPath} -CA {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ca.crt")} -CAkey {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ca.key")} -sha256 -passin pass:111111");
				ProcessStartInfo info = new ProcessStartInfo(batch)
				{
					CreateNoWindow = true,
					UseShellExecute = false
				};
				var proc = Process.Start(info);
				proc?.WaitForExit();

				if(System.IO.File.Exists(crtPath))
                {
					X509Certificate2 cert = new X509Certificate2(X509Certificate2.CreateFromCertFile(crtPath));
					return Json(new 
					{ 
						Pkcs7 = Convert.ToBase64String(cert.RawData),
						cert.SerialNumber,
						cert.Issuer,
						cert.Subject,
						NotBefore = cert.NotBefore.ToString("yyyyMMddHHmmss"),
						NotAfter = cert.NotAfter.ToString("yyyyMMddHHmmss"),

					});
                }
			}
			return Json(new { });
        }

		public IActionResult InstallCertificate([FromBody] TWCAPKCSViewModel viewModel)
        {
            try 
			{
				String pkcs7_Cert = viewModel.Pkcs7.GetEfficientString();
				if (pkcs7_Cert != null)
				{
					CX509EnrollmentClass objEnroll = new CX509EnrollmentClass();
					objEnroll.Initialize(X509CertificateEnrollmentContext.ContextUser); //1
					objEnroll.InstallResponse(0, pkcs7_Cert, EncodingType.XCN_CRYPT_STRING_BASE64_ANY/*6*/, "");

					return Json(new { result = true });

				}

				return Json(new { result = false });
			}
			catch (Exception ex)
			{
				FileLogger.Logger.Error(ex);
				return Json(new { Message = ex.Message });
			}
		}

	}
}
