namespace ContractHome.Models.Dto
{
    public class BaseResponse
    {
        public bool HasError { get; set; }
        public object Data { get; set; } = string.Empty;
        public object Message { get; set; }

        public BaseResponse()
        {
            HasError = false;
        }
        public BaseResponse(bool haserror, object error)
        {
            HasError = haserror;
            Message = error;
        }
    }
}
