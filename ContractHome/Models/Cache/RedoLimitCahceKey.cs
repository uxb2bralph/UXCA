using DocumentFormat.OpenXml.Spreadsheet;

namespace ContractHome.Models.Cache
{
    public class RedoLimitCahceKey : ICacheKey<Default>
    {
        private readonly string _item;
        public RedoLimitCahceKey(string item)
        {
            _item = item;
        }

        public string CacheKey => $"RedoLimit_{this._item}";

        public string DurationSetting => "Default";
    }
}
