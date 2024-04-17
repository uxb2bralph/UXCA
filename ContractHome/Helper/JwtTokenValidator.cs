using CommonLib.Core.Utility;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using static ContractHome.Helper.JwtTokenGenerator;

namespace ContractHome.Helper
{
    public class JwtTokenValidator
    {
        public static JwtToken DecodeJwtToken(string token)
        {
            //FileLogger.Logger.Error($"token={token}");
            var tokenParts = token.Split('.');
            // Step 1: Decode the Header and Payload
            var encodedHeader = tokenParts[0];
            var encodedPayload = tokenParts[1];

            var header = Encoding.UTF8.GetString(Base64UrlDecode(encodedHeader));
            var payload = Encoding.UTF8.GetString(Base64UrlDecode(encodedPayload));
            //FileLogger.Logger.Error($"header={header}");
            //FileLogger.Logger.Error($"payload={payload}");
            // Step 2: Verify the Signature
            var signature = Base64UrlDecode(tokenParts[2]);
            FileLogger.Logger.Error($"tokenParts[2]={tokenParts[2]}");
            return new JwtToken()
            {
                headerObj = JsonConvert.DeserializeObject<JwtHeader>(header),
                header = encodedHeader,
                payloadObj = JsonConvert.DeserializeObject<JwtPayload>(payload),
                payload = encodedPayload,
                signature = signature
            };
        }

        public static bool ValidateJwtToken(string jwtToken, string secretKey)
        {
            JwtToken jwtTokenObj = DecodeJwtToken(jwtToken);

            var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
            var input = $"{jwtTokenObj.header}.{jwtTokenObj.payload}";
            FileLogger.Logger.Error($"input={input}");

            var isValidSignature = VerifyTokenSignature(input, jwtTokenObj.signature, secretKeyBytes);

            // Step 3: Check Token Expiration (if applicable)
            var isTokenExpired = CheckTokenExpiration(jwtTokenObj.payloadObj);

            // Step 4: Return the Token Validation Result
            return isValidSignature && !isTokenExpired;
        }

        private static bool VerifyTokenSignature(string input, byte[] signature, byte[] secretKey)
        {
            using (var hmac = new HMACSHA256(secretKey))
            {
                var computedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
                FileLogger.Logger.Error($"computedSignature={computedSignature}");
                return computedSignature.SequenceEqual(signature);
            }
        }

        //private static bool CheckTokenExpiration(string payload)
        //{
        //    var payloadJson = JsonConvert.DeserializeObject<dynamic>(payload);
        //    var expiration = payloadJson.exp;
        //    if (expiration == null)
        //    {
        //        // Expiration claim does not exist
        //        return false;
        //    }
        //    //var expirationDateTime = DateTimeOffset.FromUnixTimeSeconds((long)expiration);
        //    //return DateTimeOffset.UtcNow > expirationDateTime;
        //    long exp = payloadJson.exp;
        //    long now = DateTime.Now.Ticks;
        //    return (now > exp);
        //}

        private static bool CheckTokenExpiration(JwtPayload payload)
        {
            //var payloadJson = JsonConvert.DeserializeObject<dynamic>(payload);
            var expiration = payload.exp;
            if (expiration == null)
            {
                // Expiration claim does not exist
                return false;
            }
            //var expirationDateTime = DateTimeOffset.FromUnixTimeSeconds((long)expiration);
            //return DateTimeOffset.UtcNow > expirationDateTime;
            long exp = payload.exp;
            long now = DateTime.Now.Ticks;
            return (now > exp);
        }

        internal static string Base64UrlDecodeToString(string input)
        {
            var ttt = Base64UrlDecode(input);
            return Encoding.UTF8.GetString(ttt);
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var base64Url = input.Replace("-", "+").Replace("_", "/");
            switch (base64Url.Length % 4)
            {
                case 2: base64Url += "=="; break;
                case 3: base64Url += "="; break;
            }
            return Convert.FromBase64String(base64Url);
        }
    }
}
