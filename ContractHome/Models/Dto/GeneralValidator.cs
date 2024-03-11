using ContractHome.Controllers;
using ContractHome.Helper;
using DocumentFormat.OpenXml;

namespace ContractHome.Models.Dto
{
    public static class GeneralValidator
    {

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
    }
}
