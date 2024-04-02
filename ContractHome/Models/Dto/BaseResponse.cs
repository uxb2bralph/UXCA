using CommonLib.Utility;
using System.Text.Json.Serialization;

namespace ContractHome.Models.Dto
{
    public class BaseResponse
    {
        public bool HasError { get; set; }
        public dynamic Data { get; set; }
        public dynamic Message { get; set; }


        public BaseResponse()
        {
            HasError = false;
            Data= string.Empty;
            Message = string.Empty;
        }

        public BaseResponse(dynamic data)
        {
            HasError = false;
            Data = data;
            Message = string.Empty;
        }

        public BaseResponse(bool haserror, object error)
        {
            HasError = haserror;
            Message = error;
            Data= string.Empty;
        }
    }
}

