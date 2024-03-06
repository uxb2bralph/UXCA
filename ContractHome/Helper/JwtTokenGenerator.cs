using ContractHome.Models.DataEntity;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using System.Data.Linq.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace ContractHome.Helper
{
    public class JwtTokenGenerator
    {
        internal readonly static string secretKey = "XulpspCzuyL1fmCWxfw9g2o8qyMKHAUl";
        internal readonly static int tokenTTLMins = 10;
        public class JwtToken
        {
            public JwtHeader headerObj { get; set; }
            public string header { get; set; }
            public JwtPayload payloadObj { get; set; }
            public string payload { get; set; }
            public byte[] signature { get; set; }

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
            public string id { get; set; }
            public string email { get; set; }
            public string contractId { get; set; }
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

        public static string GenerateJwtToken(object payload)
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

        private static string Base64UrlEncode(byte[] input)
        {
            var base64 = Convert.ToBase64String(input);
            var base64Url = base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
            return base64Url;
        }

        public static JwtPayload GetJwtPayload(string uid, string email, string contractId, int tokenTTLMins=10)
        {
            DateTimeOffset now = DateTime.Now;
            return new JwtPayload()
            {
                id = uid,
                email = email,
                contractId = contractId,
                iat = now.Ticks,
                exp = now.AddMinutes(tokenTTLMins).Ticks
            };
        }

    }
}
