using ContractHome.Controllers;
using ContractHome.Helper;

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
    }
}
