using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using ContractHome.Models.Dto;

namespace ContractHome.Helper
{
    public class IdentityCertHelper
    {


        private static X509Certificate2 X509Cert;


        public IdentityCertHelper(string x509PemString)
        {
            X509PemString = x509PemString;
            try
            {
                X509Cert = X509Certificate2.CreateFromPem(x509PemString);
            }
            catch (Exception)
            {
                X509Cert = null;
            }

        }

        //public string X509String => $"{begin}MIIEtjCCA56gAwIBAgIRAPXItal3grrgM80615nwNlQwDQYJKoZIhvcNAQELBQAwRzELMAkGA1UEBhMCVFcxEjAQBgNVBAoMCeihjOaUv+mZojEkMCIGA1UECwwb5YWn5pS/6YOo5oaR6K2J566h55CG5Lit5b+DMB4XDTE5MDExMjAyNDczNVoXDTI3MDExMjE1NTk1OVowPDELMAkGA1UEBhMCVFcxEjAQBgNVBAMMCeiztOmbr+eRhDEZMBcGA1UEBRMQMDAwMDAwMDExNjc3MDcyMjCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAMfu7f3Zvq6OVzFBL8oMvXy5J02vJa46o1MhaOz40S9Czi4P78ZTQqnoK+F5/+CTmq5mbLkeyMu0sTOJjkVXfTcc9GUOg639FgFEVgRZtisEbGkRbX1xjUiuafzAOiEWgrf/XTjFWOeufUL0Hw2mt7afIIfQa6hTGOqdivUhcLInGMlloH/DIRDbNAmlWVuxx36YoR8SpXsFGcyis6wKWtCrOblLJ+oYQtfswgqWseHMiK+KINtAUlW9wY64k0kTRlbDFdt8NLHqS5+TtfWD5TojbMfpvI6eVzEwkiFzKCLxZ1f6jyrnsTV+aVk+iwCGxGdwDu+ZYJDWg399xdFe35ECAwEAAaOCAaYwggGiMB8GA1UdIwQYMBaAFPqbNGcJCpgi92JIi4ImpkXFwyKkMB0GA1UdDgQWBBTI5cMw9VxUwVF5Lii8Eg4dlxPEqTAOBgNVHQ8BAf8EBAMCB4AwFAYDVR0gBA0wCzAJBgdghnZlAAMDMDMGA1UdCQQsMCowFQYHYIZ2AWQCATEKBghghnYBZAMBATARBgdghnYBZAIzMQYMBDMwMTMwfAYDVR0fBHUwczA2oDSgMoYwaHR0cDovL29jc3AtbW9pY2EubW9pLmdvdi50dy9jcmwvTU9JQ0EtMTAtMzEuY3JsMDmgN6A1hjNodHRwOi8vb2NzcC1tb2ljYS5tb2kuZ292LnR3L2NybC9NT0lDQS1jb21wbGV0ZS5jcmwwgYYGCCsGAQUFBwEBBHoweDBHBggrBgEFBQcwAoY7aHR0cDovL21vaWNhLm5hdC5nb3YudHcvcmVwb3NpdG9yeS9DZXJ0cy9Jc3N1ZWRUb1RoaXNDQS5wN2IwLQYIKwYBBQUHMAGGIWh0dHA6Ly9vY3NwLW1vaWNhLm1vaS5nb3YudHcvT0NTUDANBgkqhkiG9w0BAQsFAAOCAQEAA/2W8nXy1rikohBI9VFrV4KqXUk7+p5WYLk13ISkV4VjMGgvjH12VHmPoeiW7YYse1gkYmndyTyZT+GX97C2X14OhMnkBJx3CmSvDRGecyLowPgQxdZ9FVPnLaG6lo5/kOIU0pi3AIkfJlVUfzVG/7FWS7M9kjCWsgTZTq4rnzBOMstKNYIj9O6NF2ixtJXRQPCl9woLK2G9iI4/wkifM+R07E/6F6DGyd7GcltGbEU/UfnoQXPaNG34GBjjGDQi+zB7l65rHmH+ssYkg/Q/MajpYh0oKVdmt0CCZkli44SqXNUAvIcx3KcKM0j/tJoxiGN1tA9m/O3euNoLw+LUZQ=={end}";

        //public static X509Certificate2 GetX509CertStringWithHeader(string x509PemWithoutHeader) 
        //    => X509Certificate2.CreateFromPem($"{begin}{x509PemWithoutHeader}{end}");

        public string X509PemString { get; }
        public bool IsSignatureValid(string tbs, string Signature)
        {
            if (X509Cert == null) return false;
            if (!GeneralValidator.TryFromBase64String(Signature)) return false;
            try
            {
                //for test
                tbs = "abc";
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
