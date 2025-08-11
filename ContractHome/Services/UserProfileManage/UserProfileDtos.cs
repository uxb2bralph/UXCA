using ContractHome.Helper;
namespace ContractHome.Services.UserProfileManage
{
    public class UserProfileBaseModel
    {
        public string KeyID { get; set; } = string.Empty;
    }
    public class PIDAndPasswordUpdateModel : UserProfileBaseModel
    {
        public int UID => KeyID.DecryptKeyValue();
        public string PID { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ReNewPassword { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
