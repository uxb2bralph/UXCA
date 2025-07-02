using CommonLib.Core.Utility;
using CommonLib.DataAccess;
using CommonLib.Utility;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Properties;
using Irony.Parsing;
using Microsoft.AspNetCore.Http.HttpResults;
using MimeKit.Cryptography;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Tls;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ContractHome.Helper
{
    public static class CHTSigningService
    {
        private static CHT_Token? _DefaultToken;

        static CHTSigningService() 
        {
            using(ModelSource models =  new ModelSource())
            {
                _DefaultToken = models.GetTable<CHT_Token>()
                    .Where(t => t.CompanyID == Settings.Default.CHTSigning.SystemTokenID)
                    .FirstOrDefault();
                var partner = _DefaultToken?.Organization;
            }
        }

        public static bool CHT_SignPdfByEnterprise(this GenericManager<DCDataContext> models, ContractSignatureRequest request, UserProfile signer)
        {
            CHT_Token key = request.Organization.CHT_Token;

            string privateKey = key.ApplicationKey;
            // 創建RSA加密服務提供者
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportFromPem(privateKey);

            // 輸入要加密的字串
            string inputString = $"{key.Token}{DateTime.Now:yyyyMMddHHmmss}";
            using SHA256 hash = SHA256.Create();
            byte[] inputBytes = hash.ComputeHash(Encoding.Default.GetBytes(inputString));
            // 將輸入字串轉換為byte陣列

            // 進行RSA加密運算
            byte[] encryptedBytes = rsa.SignData(inputBytes, hash); //rsa.Encrypt(inputBytes, false);

            encryptedBytes = rsa.SignHash(inputBytes, "SHA256");
            var apSignature = encryptedBytes.ToHexString(format: "x2");
            var data = new
            {
                clusterid = key.ClusterID,
                b64pdf = request.Contract.BuildContractWithSignatureBase64(models),
                uid = $"uxb2b-{signer.UID}",
                pdfpw = "",
                ownerpw = "",
                userpw = "",
                thirdpartyclusterid = key.ThirdPartyClusterID,
                email = key.Email,
                apOneTimeToken = inputString,
                apSignature = apSignature
            };

            String dataToSign = data.JsonStringify();
            request.RequestPath = Path.Combine(FileLogger.Logger.LogDailyPath, $"request-{Guid.NewGuid()}.json");
            File.WriteAllText(request.RequestPath, dataToSign);

            using (WebClientEx client = new WebClientEx())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                //3partypdfencsign加密簽只能在第一次簽章使用，第二次開始請用3partypdfsign(沒加密)
                String result = client.UploadString(models.GetTable<ContractSignatureRequest>()
                                    .Where(r=>r.ContractID==request.ContractID).Where(r=>r.SignatureDate.HasValue)
                                    .Any() 
                                        ? Settings.Default.CHTSigning.AP_SignPDF
                                        : Settings.Default.CHTSigning.AP_SignPDF_Encrypt, dataToSign);

                String responsePath = Path.Combine(FileLogger.Logger.LogDailyPath, $"response-{Guid.NewGuid()}.json");
                File.WriteAllText(responsePath, result);

                JObject content = JObject.Parse(result);
                if ((String)content["code"] == "0")
                {
                    request.ResponsePath = responsePath;
                    return true;
                }
            }

            return false;
        }

        public static JObject CHT_UserRequestTicket(this UserProfile signer)
        {
            if (_DefaultToken == null)
            {
                return new JObject();
            }

            string privateKey = _DefaultToken.ApplicationKey;
            // 創建RSA加密服務提供者
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportFromPem(privateKey);

            // 輸入要加密的字串
            string inputString = $"{_DefaultToken.Token}{DateTime.Now:yyyyMMddHHmmss}";
            using SHA256 hash = SHA256.Create();
            byte[] inputBytes = hash.ComputeHash(Encoding.Default.GetBytes(inputString));
            // 將輸入字串轉換為byte陣列

            // 進行RSA加密運算
            byte[] encryptedBytes = rsa.SignData(inputBytes, hash); //rsa.Encrypt(inputBytes, false);

            encryptedBytes = rsa.SignHash(inputBytes, "SHA256");
            var apSignature = encryptedBytes.ToHexString(format: "x2");
            var data = new
            {
                account = signer.EMail,
                authType = "email",
                oneTimeToken = inputString,
                signature = apSignature,
                certificateType = signer.Region ?? "O",
                certificateValidity = "1Y",
            };

            String dataToSign = data.JsonStringify();
            File.WriteAllText(Path.Combine(FileLogger.Logger.LogDailyPath, $"request-{Guid.NewGuid()}.json"), dataToSign);

            using (WebClientEx client = new WebClientEx())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                String result = client.UploadString(Settings.Default.CHTSigning.UserRequestTicket, dataToSign);

                String responsePath = Path.Combine(FileLogger.Logger.LogDailyPath, $"response-{Guid.NewGuid()}.json");
                File.WriteAllText(responsePath, result);

                JObject content = JObject.Parse(result);
                return content;
            }
        }

        public static JObject CHT_RequireIssue(this UserProfile signer,String discountCode)
        {
            if (_DefaultToken == null)
            {
                return new JObject();
            }

            string privateKey = _DefaultToken.ApplicationKey;
            // 創建RSA加密服務提供者
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportFromPem(privateKey);

            // 輸入要加密的字串
            string inputString = $"{_DefaultToken.Token}{DateTime.Now:yyyyMMddHHmmss}";
            using SHA256 hash = SHA256.Create();
            byte[] inputBytes = hash.ComputeHash(Encoding.Default.GetBytes(inputString));
            // 將輸入字串轉換為byte陣列

            // 進行RSA加密運算
            byte[] encryptedBytes = rsa.SignData(inputBytes, hash); //rsa.Encrypt(inputBytes, false);

            encryptedBytes = rsa.SignHash(inputBytes, "SHA256");
            var apSignature = encryptedBytes.ToHexString(format: "x2");
            var data = new
            {
                email = signer.EMail,
                authType = "IC",
                certificateType = signer.Region ?? "O",
                discountCode = discountCode,
                partner = _DefaultToken.Organization.CompanyName,
                returnUrl = Settings.Default.CHTSigning.ReturnUrl,
                oneTimeToken = inputString,
                signature = apSignature,
            };

            String dataToSign = data.JsonStringify();
            // 檢查是否簽署過
            bool hasSignedRecord = signer.ContractSignatureRequest.Where(x => !string.IsNullOrEmpty(x.RequestTicket)).Any();

            using (WebClientEx client = new WebClientEx())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                String result = client.UploadString(Settings.Default.CHTSigning.RequireIssue, dataToSign);

                String responsePath = Path.Combine(FileLogger.Logger.LogDailyPath, $"response-{Guid.NewGuid()}.json");
                File.WriteAllText(responsePath, result);

                JObject content = JObject.Parse(result);
                content.Add("hasSignedRecord", hasSignedRecord);
                return content;
            }
        }

        public static (bool,string) CHT_SignPdfByUser(this GenericManager<DCDataContext> models, ContractSignatureRequest request, UserProfile signer)
        {
            if (_DefaultToken == null)
            {
                return (false,string.Empty);
            }

            var data = new
            {
                clusterid = _DefaultToken.ClusterID,
                b64pdf = request.Contract.BuildContractWithSignatureBase64(models),
                uid = $"uxb2b-{signer.UID}",
                pdfpw = "",
                //ownerpw = "",
                //userpw = "",
                thirdpartyclusterid = _DefaultToken.ThirdPartyClusterID,
                email = signer.EMail,
                tid = request.RequestTicket,
            };

            String dataToSign = data.JsonStringify();
            request.RequestPath = Path.Combine(FileLogger.Logger.LogDailyPath, $"request-{Guid.NewGuid()}.json");
            File.WriteAllText(request.RequestPath, dataToSign);

            using (WebClientEx client = new WebClientEx())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                //3partypdfencsign加密簽只能在第一次簽章使用，第二次開始請用3partypdfsign(沒加密)
                //String result = client.UploadString(Settings.Default.CHTSigning.User_SignPDF_Encrypt, dataToSign);
                String result = client.UploadString(models.GetTable<ContractSignatureRequest>()
                    .Where(r => r.ContractID == request.ContractID).Where(r => r.SignatureDate.HasValue)
                    .Any()
                        ? Settings.Default.CHTSigning.User_SignPDF
                        : Settings.Default.CHTSigning.User_SignPDF_Encrypt, dataToSign);


                String responsePath = Path.Combine(FileLogger.Logger.LogDailyPath, $"response-{Guid.NewGuid()}.json");
                File.WriteAllText(responsePath, result);

                JObject content = JObject.Parse(result);
                if ((String)content["code"] == "0")
                {
                    request.ResponsePath = responsePath;
                    return (true, string.Empty);
                } 
                else
                {
                    return (false, (String)content["code"]);
                }
            }
        }

        public static CHT_Token? SystemToken => _DefaultToken;

        public static (string oneTimeToken, string signature) GetApplicationResponse()
        {
            if (_DefaultToken == null)
            {
                return (string.Empty, string.Empty);
            }

            string privateKey = _DefaultToken.ApplicationKey;
            // 創建RSA加密服務提供者
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportFromPem(privateKey);

            // 輸入要加密的字串
            string inputString = $"{_DefaultToken.Token}{DateTime.Now:yyyyMMddHHmmss}";
            using SHA256 hash = SHA256.Create();
            byte[] inputBytes = hash.ComputeHash(Encoding.Default.GetBytes(inputString));
            // 將輸入字串轉換為byte陣列

            // 進行RSA加密運算
            byte[] encryptedBytes = rsa.SignData(inputBytes, hash); //rsa.Encrypt(inputBytes, false);

            encryptedBytes = rsa.SignHash(inputBytes, "SHA256");
            var apSignature = encryptedBytes.ToHexString(format: "x2");


            return (inputString, apSignature);
        }
    }
}
