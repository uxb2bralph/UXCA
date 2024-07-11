using DocumentFormat.OpenXml.Spreadsheet;

namespace ContractHome.Models.Cache
{
    public class EmailSentCache : ICacheKey
    {
        private string _email;
        public EmailSentCache()
        {

        }


        public string CacheKey => $"{this.GetType().Name}_{this._email}";

        public void CreateCacheKey(string email)
        {
            _email = email;
        }
    }
}
