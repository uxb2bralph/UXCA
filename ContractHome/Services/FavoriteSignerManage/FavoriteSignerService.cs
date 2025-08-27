using CommonLib.Utility;
using ContractHome.Models.DataEntity;
using ContractHome.Helper;
using static ContractHome.Services.FavoriteSignerManage.FavoriteSignerDtos;

namespace ContractHome.Services.FavoriteSignerManage
{
    public class FavoriteSignerService(DCDataContext db) : IFavoriteSignerService
    {
        private readonly DCDataContext _db = db;

        public void CreateFavoriteSigner(FavoriteSignerCreateRequest request)
        {
            // 檢查簽署人是否已存在
            int? signerUID = _db.UserProfile.Where(u => u.EMail == request.Email).Select(u => u.UID).FirstOrDefault();

            signerUID ??= CreateSigner(request.Email, request.CreatorUID);

            // 檢查簽署人跟統編是否已存在
            var organizationUser = (from u in db.UserProfile
                                   join ou in db.OrganizationUser on u.UID equals ou.UID
                                   join o in db.Organization on ou.CompanyID equals o.CompanyID
                                   where u.UID == signerUID && o.ReceiptNo == request.ReceiptNo
                                   select ou).FirstOrDefault();

            if (organizationUser != null)
            {
                CreateFavoriteSigner(signerUID.Value, request.CreatorUID);
                return;
            }

            // 檢查公司統編是否存在
            int? companyID = _db.Organization.Where(c => c.ReceiptNo == request.ReceiptNo).Select(c => c.CompanyID).FirstOrDefault();

            if (companyID == null)
            {
                CreateOrganizationUser(request.CompanyName, request.ReceiptNo, signerUID.Value);
            }

            // 建立常用簽署人
            CreateFavoriteSigner(signerUID.Value, request.CreatorUID);
        }

        public IEnumerable<FavoriteSignerInfoModel> QueryFavoriteSigner(int creatorUID)
        {
            var result = from f in db.FavoriteSigner
                         join u in db.UserProfile on f.SignerUID equals u.UID
                         join ou in db.OrganizationUser on u.UID equals ou.UID
                         join o in db.Organization on ou.CompanyID equals o.CompanyID
                         where f.CreateUID == creatorUID
                         select new FavoriteSignerInfoModel
                         {
                             KeyID = f.FavoriteSignerID.EncryptKey(),
                             Email = u.EMail,
                             CompanyName = o.CompanyName,
                             ReceiptNo = o.ReceiptNo,
                             CreatorKeyID = f.CreateUID.EncryptKey()
                         };

            return result.ToList();
        }

        private int CreateSigner(string email, int creatorUID)
        {
            // 建立簽署人
            UserProfile userProfile = new()
            {
                EMail = email,
                PID = email,
                Password = $"@{email}".HashPassword(),
                Region = "O",
                Creator = creatorUID
            };

            db.UserProfile.InsertOnSubmit(userProfile);
            db.SubmitChanges();
            // 建立腳色
            UserRole userRole = new()
            {
                RoleID = (int)UserRoleDefinition.RoleEnum.User,
                UID = userProfile.UID
            };

            db.UserRole.InsertOnSubmit(userRole);
            db.SubmitChanges();

            return userProfile.UID;
        }

        private void CreateFavoriteSigner(int signerUID, int createUID)
        {
            // 建立常用簽署人
            FavoriteSigner favoriteSigner = new()
            {
                SignerUID = signerUID,
                CreateUID = createUID
            };

            db.FavoriteSigner.InsertOnSubmit(favoriteSigner);
            db.SubmitChanges();
        }

        private void CreateOrganizationUser(string companyName, string receiptNo, int uid)
        {
            // 建立公司
            Organization organization = new()
            {
                CompanyName = companyName,
                ReceiptNo = receiptNo,
                CanCreateContract = false
            };

            db.Organization.InsertOnSubmit(organization);
            db.SubmitChanges();

            // 建立公司使用者關聯
            OrganizationUser organizationUser = new()
            {
                CompanyID = organization.CompanyID,
                UID = uid,
            };

            db.OrganizationUser.InsertOnSubmit(organizationUser);
            db.SubmitChanges();
        }

        public void DeleteFavoriteSigner(FavoriteSignerDeleteRequest request)
        {
            var favoriteSigner = db.FavoriteSigner
                                .Where(x => x.FavoriteSignerID == request.FavoriteSignerID && x.CreateUID == request.CreatorUID)
                                .FirstOrDefault();

            if (favoriteSigner != null)
            {
                db.FavoriteSigner.DeleteOnSubmit(favoriteSigner);
                db.SubmitChanges();
            }
        }
    }
}
