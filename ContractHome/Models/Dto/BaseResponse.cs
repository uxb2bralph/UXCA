using CommonLib.Utility;
using ContractHome.Models.DataEntity;
using ContractHome.Properties;
using System.Text.Json.Serialization;

namespace ContractHome.Models.Dto
{
    public class BaseResponse
    {
        public bool HasError { get; set; }
        public bool Result { get => !HasError; }
        public dynamic Data { get; set; }
        public dynamic Message { get; set; }
        public string Url { get; set; }


        public BaseResponse()
        {
            HasError = false;
            Data = string.Empty;
            Message = string.Empty;
            Url = string.Empty;
        }

        public BaseResponse(dynamic data)
        {
            HasError = false;
            Data = data;
            Message = string.Empty;
            Url = string.Empty;
        }

        public BaseResponse(bool haserror, object error)
        {
            HasError = haserror;
            Message = error;
            Data= string.Empty;
            Url = string.Empty;
        }

        public BaseResponse AddContractMessage(Contract contract)
        {
            Message = $"{contract.Title}({contract.ContractNo}) {this.Message}";
            return this;
        }

        public BaseResponse (WebReasonEnum reason)
        {
            HasError = true;
            Data = string.Empty;

            switch (reason)
            {
                case WebReasonEnum.Relogin:
                    {
                        Message= "請重新登入";
                        Url = Settings.Default.WebAppDomain;
                    };
                    break;
                case WebReasonEnum.ContractNotExisted:
                    {
                        Message = "合約資料有誤";
                        Url = Settings.Default.ContractListUrl;
                    };
                    break;
                default:
                    {
                        Message = "請重新登入";
                        Url = Settings.Default.WebAppDomain;
                    };
                    break;
            }

        }

    }

    public enum WebReasonEnum
    {
        Relogin,
        ContractNotExisted
    }

}

