using ContractHome.Controllers;
using ContractHome.Helper;
using DocumentFormat.OpenXml;
using FluentValidation;

namespace ContractHome.Models.Dto
{
    public static class GeneralValidator
    {

        //public static IRuleBuilderOptions<T, string> MatchPhoneNumber<T>(this IRuleBuilder<T, string> rule)
        //    => rule.Matches(@"^(1-)?\d{3}-\d{3}-\d{4}$").WithMessage("Invalid phone number");

        public static bool TryDecryptKeyValue(string key)
        {
            try
            {
                var decryptKeyValue = key.DecryptKeyValue();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool MustAfterOrIsToday(string dateTimeString)
        {
            return DateTime.TryParse(dateTimeString, out DateTime dateValue)
                && (dateValue.CompareTo(DateTime.Now)>=0);
        }

        public static bool MustBeforeToday(string dateTimeString)
        {
            return DateTime.TryParse(dateTimeString, out DateTime dateValue)
                && (dateValue.CompareTo(DateTime.Now) < 0);
        }

        public static bool TryFromBase64String(string bs64String)
        {
            return Convert.TryFromBase64String(bs64String, new Span<byte>(new byte[bs64String.Length]), out int bytesParsed);
        }
    }
}
