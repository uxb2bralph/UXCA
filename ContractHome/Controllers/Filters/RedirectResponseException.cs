namespace ContractHome.Controllers.Filters
{
    internal class RedirectResponseException: Exception
    {
        public RedirectResponseException(object obj) : base(message: obj.ToString())
        { 
        }
    }
}