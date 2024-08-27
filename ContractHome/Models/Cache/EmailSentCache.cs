using DocumentFormat.OpenXml.Spreadsheet;

namespace ContractHome.Models.Cache
{
    public class EmailSentCache : ICacheKey<EmailSent>
    {
        private string _email;
        public EmailSentCache(string email)
        {
            _email = email;
        }


        public string CacheKey => $"{this.GetType().Name}_{this._email}";

    }

    public class EmailSent
    {
    }
}
