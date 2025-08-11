using ContractHome.Models.DataEntity;
using CommonLib.Utility;

namespace ContractHome.Services.UserProfileManage
{
    public class UserProfileService : IUserProfileService
    {
        public void PIDAndPasswordUpdate(PIDAndPasswordUpdateModel model)
        {
            using var db = new DCDataContext();

            UserProfile userProfile = db.UserProfile
                                     .Where(u => u.UID == model.UID)
                                     .FirstOrDefault() ?? throw new Exception("User not found");

            userProfile.PID = model.PID;
            userProfile.LoginFailedCount = 0;
            userProfile.Password = model.NewPassword.HashPassword();
            userProfile.Password2 = model.NewPassword.HashPassword();
            userProfile.PasswordUpdatedDate = DateTime.Now;

            db.SubmitChanges();
        }
    }
}
