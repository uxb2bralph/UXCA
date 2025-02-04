using ContractHome.Models.Email.Template;
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
            private JwtHeader headerObj { get; set; }
            protected internal string header { get; set; }
            protected internal JwtPayload payloadObj { get; set; }
            protected internal string payload { get; set; }
            protected internal byte[] signature { get; set; }

            public string ContractID => payloadObj.data.ContractID??string.Empty;
            public int DecryptContractID => payloadObj.data.ContractID.DecryptKeyValue();
            public string UID => payloadObj.data.UID;
            public int DecryptUID => payloadObj.data.UID.DecryptKeyValue();

            public bool IsSeal => payloadObj.data.Func.Equals(typeof(NotifySeal).Name)
                || payloadObj.data.Func.Equals(typeof(TaskNotifySeal).Name);
            public bool IsSign => payloadObj.data.Func.Equals(typeof(NotifySign).Name)
                || payloadObj.data.Func.Equals(typeof(TaskNotifySign).Name);
            public bool IsFieldSet => payloadObj.data.Func.Equals(typeof(TaskNotifyFieldSet).Name);

            public JwtToken(JwtHeader headerObj, string header, JwtPayload payloadObj, string payload, byte[] signature)
            {
                this.headerObj = headerObj;
                this.header = header;
                this.payloadObj = payloadObj;
                this.payload = payload;
                this.signature = signature;
            }

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
            public string? ContractID { get; set; }
            public string? Func { get; set; }

            public override string? ToString()
            {
                return $"UID={UID} contractId={ContractID} Func={Func}";
            }
        }

        public static string GenerateJwtToken(int uid, string func, int tokenTTLMins = 4320)
        {
            JwtTokenGenerator.JwtPayloadData jwtPayloadData = new JwtTokenGenerator.JwtPayloadData()
            {
                UID = uid.EncryptKey(),
                ContractID = null,
                Func = func
            };

            JwtPayload jwtPayload = JwtTokenGenerator.CreateJwtPayload(
                payloadData: jwtPayloadData,
                tokenTTLMins: tokenTTLMins);

            return CreateJwtToken(jwtPayload);
        }

        public static string GenerateJwtToken(int uid, int contractID, string func,  int tokenTTLMins = 4320)
        {
            JwtTokenGenerator.JwtPayloadData jwtPayloadData = new JwtTokenGenerator.JwtPayloadData()
            {
                UID = uid.EncryptKey(),
                ContractID = contractID.EncryptKey(),
                Func = func
            };

            JwtPayload jwtPayload = JwtTokenGenerator.CreateJwtPayload(
                payloadData: jwtPayloadData,
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

        public static JwtPayload CreateJwtPayload(JwtPayloadData payloadData, int tokenTTLMins=1)
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
