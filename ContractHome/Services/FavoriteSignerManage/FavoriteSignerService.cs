using CommonLib.Core.Utility;
using CommonLib.Utility;
using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using System.Data.SqlClient;
using static ContractHome.Services.FavoriteSignerManage.FavoriteSignerDtos;

namespace ContractHome.Services.FavoriteSignerManage
{
    public class FavoriteSignerService(DCDataContext db) : IFavoriteSignerService
    {
        private readonly DCDataContext _db = db;

        public void CreateFavoriteSigner(FavoriteSignerCreateRequest request)
        {
            // 檢查簽署人是否已存在
            int signerUID = _db.UserProfile.Where(u => u.EMail == request.Email).Select(u => u.UID).FirstOrDefault();

            if (signerUID == 0)
            {
                signerUID = CreateSigner(request.Email, request.CreatorUID);
            }

            // 檢查簽署人的公司是否已存在
            var organizationUser = (from u in _db.UserProfile
                                   join ou in _db.OrganizationUser on u.UID equals ou.UID
                                   join o in _db.Organization on ou.CompanyID equals o.CompanyID
                                   where u.UID == signerUID
                                   select ou).FirstOrDefault();

            // 簽署人有對應的公司 
            if (organizationUser != null)
            {
                // 直接忽視統編的檢查做存檔
                CreateFavoriteSigner(signerUID, request.CreatorUID);
                return;
            }

            // 檢查公司統編是否存在
            int companyID = _db.Organization.Where(c => c.ReceiptNo == request.ReceiptNo).Select(c => c.CompanyID).FirstOrDefault();

            if (companyID == 0)
            {
                // 統編不存在 建立公司及關聯
                CreateOrganizationAndUser(request.CompanyName, request.ReceiptNo, signerUID, request.CreatorUID);
            }
            else
            {
                // 統編存在 建立關聯
                CreateOrganizationUser(companyID, signerUID);
            }

            // 建立常用簽署人
            CreateFavoriteSigner(signerUID, request.CreatorUID);
        }

        public IEnumerable<FavoriteSignerInfoModel> QueryFavoriteSigner(int creatorUID)
        {
            var result = from f in _db.FavoriteSigner
                         join u in _db.UserProfile on f.SignerUID equals u.UID
                         join ou in _db.OrganizationUser on u.UID equals ou.UID
                         join o in _db.Organization on ou.CompanyID equals o.CompanyID
                         where f.CreateUID == creatorUID
                         select new FavoriteSignerInfoModel
                         {
                             KeyID = f.FavoriteSignerID.EncryptKey(),
                             Email = u.EMail,
                             CompanyName = o.CompanyName,
                             ReceiptNo = o.ReceiptNo
                         };

            return result.ToList();
        }

        private int CreateSigner(string email, int creatorUID)
        {
            // 建立簽署人
            UserProfile userProfile = new()
            {
                EMail = email,
                PID = email + '_' + Guid.NewGuid(),
                Password = $"@{email}".HashPassword(),
                Region = "O",
                Creator = creatorUID
            };

            _db.UserProfile.InsertOnSubmit(userProfile);
            _db.SubmitChanges();
            // 建立腳色
            UserRole userRole = new()
            {
                RoleID = (int)UserRoleDefinition.RoleEnum.User,
                UID = userProfile.UID
            };

            _db.UserRole.InsertOnSubmit(userRole);
            _db.SubmitChanges();

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

            _db.FavoriteSigner.InsertOnSubmit(favoriteSigner);
            _db.SubmitChanges();
        }

        private int CreateOrganization(string companyName, string receiptNo, int createUID)
        {
            // 建立公司
            Organization organization = new()
            {
                CompanyName = companyName,
                ReceiptNo = receiptNo,
                CanCreateContract = false,
                CreateUID = createUID
            };
            _db.Organization.InsertOnSubmit(organization);
            _db.SubmitChanges();
            return organization.CompanyID;
        }

        private void CreateOrganizationUser(int companyID, int signerUID)
        {
            // 建立公司使用者關聯
            OrganizationUser organizationUser = new()
            {
                CompanyID = companyID,
                UID = signerUID,
            };

            _db.OrganizationUser.InsertOnSubmit(organizationUser);
            _db.SubmitChanges();
        }

        private void CreateOrganizationAndUser(string companyName, string receiptNo, int signerUID, int createUID)
        {
            int companyID = CreateOrganization(companyName, receiptNo, createUID);
            CreateOrganizationUser(companyID, signerUID);
        }

        public void DeleteFavoriteSigner(FavoriteSignerDeleteRequest request)
        {
            var favoriteSigner = _db.FavoriteSigner
                                .Where(x => x.FavoriteSignerID == request.FavoriteSignerID && x.CreateUID == request.CreatorUID)
                                .FirstOrDefault();

            if (favoriteSigner != null)
            {
                _db.FavoriteSigner.DeleteOnSubmit(favoriteSigner);
                _db.SubmitChanges();

                var userProfile = _db.UserProfile
                            .Where(u => u.UID == favoriteSigner.SignerUID)
                            .FirstOrDefault();

                if (userProfile != null && userProfile.Creator == favoriteSigner.CreateUID)
                {
                    try
                    {
                        // 刪除相關 UserProfile 的資料
                        _db.UserProfile.DeleteOnSubmit(userProfile);
                        _db.SubmitChanges();
                    }
                    catch (SqlException ex)
                    {
                        FileLogger.Logger.Error(ex);
                    }
                }

                var organization = (from o in _db.Organization
                                    join ou in _db.OrganizationUser on o.CompanyID equals ou.CompanyID into ouGroup
                                    from ou in ouGroup.DefaultIfEmpty()
                                    where o.OrganizationUser.Count() == 0 && o.ReceiptNo == request.ReceiptNo
                                    select o).FirstOrDefault();

                if (organization != null && organization.CreateUID == request.CreatorUID)
                {
                    try
                    {
                        // 刪除Oraganization未有User的公司
                        _db.Organization.DeleteOnSubmit(organization);
                        _db.SubmitChanges();
                    }
                    catch (SqlException ex)
                    {
                        FileLogger.Logger.Error(ex);
                    }
                }


            }


        }

        public IEnumerable<CompanyInfoModel> SearchCompany(QueryInfoModel query)
        {
            if (string.IsNullOrEmpty(query.Keyword))
            {
                return [];
            }

            var result = from o in _db.Organization
                         where o.CompanyName.Contains(query.Keyword) || o.ReceiptNo.Contains(query.Keyword)
                         select new CompanyInfoModel
                         {
                             KeyID = o.CompanyID.EncryptKey(),
                             CompanyName = o.CompanyName,
                             ReceiptNo = o.ReceiptNo,
                         };

            return result.ToList();
        }

        public IEnumerable<SignerInfoModel> SearchSigner(QueryInfoModel query)
        {
            if (string.IsNullOrEmpty(query.ReceiptNo) && string.IsNullOrEmpty(query.Keyword))
            {
                return [];
            }

            var result = from u in _db.UserProfile
                         join ou in _db.OrganizationUser on u.UID equals ou.UID
                         join o in _db.Organization on ou.CompanyID equals o.CompanyID
                         select new SignerInfoModel {
                             KeyID = u.UID.EncryptKey(),
                             Email = u.EMail,
                             CompanyName = o.CompanyName,
                             ReceiptNo = o.ReceiptNo,
                         };

            if (!string.IsNullOrEmpty(query.ReceiptNo))
            {
                result = result.Where(x => x.ReceiptNo.Contains(query.ReceiptNo));
            }

            if (!string.IsNullOrEmpty(query.Keyword))
            {
                result = result.Where(x => x.Email.Contains(query.Keyword));
            }

            return result.ToList();
        }
    }
}
