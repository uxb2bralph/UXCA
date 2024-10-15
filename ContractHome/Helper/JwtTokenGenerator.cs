using CommonLib.Core.Utility;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Email.Template;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using System.Data.Linq.SqlClient;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Text;
using static ContractHome.Helper.JwtTokenGenerator;

namespace ContractHome.Helper
{
    public class JwtTokenGenerator
    {
        internal readonly static string secretKey = "XulpspCzuyL1fmCWxfw9g2o8qyMKHAUl";
        internal readonly static int tokenTTLMins = 10;
        public class JwtToken
        {
            public JwtHeader headerObj { get; set; }
            internal string header { get; set; }
            public JwtPayload payloadObj { get; set; }
            internal string payload { get; set; }
            internal byte[] signature { get; set; }

            public string Email => payloadObj.data.Email;
            public string ContractID => payloadObj.data.ContractID;
            public string UID => payloadObj.data.UID;
            //public EmailBody.EmailTemplate? EmailTemplate => payloadObj.data.EmailTemplate;
            public bool IsSeal => payloadObj.data.Func.Equals(typeof(NotifySeal).Name)
                || payloadObj.data.Func.Equals(typeof(TaskNotifySeal).Name);
            public bool IsSign => payloadObj.data.Func.Equals(typeof(NotifySign).Name)
                || payloadObj.data.Func.Equals(typeof(TaskNotifySign).Name);
            public override string? ToString()
            {
                return payloadObj.ToString();
            }
        }

        public class JwtHeader
        {
            public string alg { get; set; }
            public string typ { get; set; }
        }

        public class JwtPayload
        {
            public long exp { get; set; }
            public long iat { get; set; }
            //public string id { get; set; }
            //public string email { get; set; }
            //public string contractId { get; set; }
            public JwtPayloadData data { get; set; }

            public override string? ToString()
            {
                return $"exp={exp} iat={iat} id={data.UID} data={data.ToString()} ";
            }
        }

        public static string GetTicket(string data, string key)
        {
            return ComputeHMACSHA256(data, key).Substring(0, 20);
        }

        public enum OneTimeUse
        {
            ApplyPassword = 1
        }

        public static string ComputeHMACSHA256(string data, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using (var hmacSHA = new HMACSHA256(keyBytes))
            {
                var dataBytes = Encoding.UTF8.GetBytes(data);
                var hash = hmacSHA.ComputeHash(dataBytes, 0, dataBytes.Length);
                return BitConverter.ToString(hash).Replace("-", "").ToUpper();
            }
        }
        public class JwtPayloadData
        {
            public string UID { get; set; }
            public string? Email { get; set; }
            public string? ContractID { get; set; }
            //public EmailBody.EmailTemplate? EmailTemplate { get; set; }
            public string? Func { get; set; }

            public override string? ToString()
            {
                return $"UID={UID} Email={Email} contractId={ContractID} Func={Func}";
            }
        }

        public static string GenerateJwtToken(JwtPayloadData jwtTokenData, int tokenTTLMins = 1)
        {
            JwtPayload jwtPayload = JwtTokenGenerator.GetJwtPayload(
                payloadData: jwtTokenData,
                tokenTTLMins: tokenTTLMins);

            return CreateJwtToken(jwtPayload);
        }

        public static string CreateJwtToken(JwtPayload payload)
        {
            var header = new { alg = "HS256", typ = "JWT" };
            var encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(header)));

            var payloadJson = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

            var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);

            var signature = SignToken($"{encodedHeader}.{encodedPayload}", secretKeyBytes);
            var encodedSignature = Base64UrlEncode(signature);

            var jwtToken = $"{encodedHeader}.{encodedPayload}.{encodedSignature}";

            return jwtToken;
        }

        private static byte[] SignToken(string input, byte[] secretKey)
        {
            using (var hmac = new HMACSHA256(secretKey))
            {
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }

        internal static string Base64UrlEncode(string input)
        {
            var base64Bytes = Encoding.UTF8.GetBytes(input);
            return Base64UrlEncode(base64Bytes);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            var base64 = Convert.ToBase64String(input);
            var base64Url = base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
            return base64Url;
        }

        public static JwtPayload GetJwtPayload(JwtPayloadData payloadData, int tokenTTLMins=1)
        {
            DateTimeOffset now = DateTime.Now;
            return new JwtPayload()
            {
                data = payloadData,
                iat = now.Ticks,
                exp = now.AddMinutes(tokenTTLMins).Ticks
            };
        }

    }
}
