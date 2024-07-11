using ContractHome.Models.Dto;

namespace ContractHome.Controllers
{
    internal class JsonResponseException : Exception
    {
        public JsonResponseException(object obj):base(message: obj.ToString())
        {
        }
    }
}