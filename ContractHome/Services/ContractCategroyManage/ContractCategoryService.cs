using ContractHome.Helper;
using ContractHome.Models.DataEntity;

namespace ContractHome.Services.ContractCategroyManage
{
    /// <summary>
    /// 合約分類service
    /// </summary>
    public class ContractCategoryService : IContractCategoryService
    {
        /// <summary>
        /// 建立合約分類及權限
        /// </summary>
        /// <param name="request"></param>
        public void CreateContractCategroy(ContractCategoryCreateRequest request)
        {
            using var db = new DCDataContext();

            ContractCategory cc = new()
            {
                CategoryName = request.CategoryName,
                Code = request.Code,
                CompanyID = request.CompanyID,
                CreateUID = request.CreateUID,
                CreateDate = request.CreateDate
            };

            db.ContractCategory.InsertOnSubmit(cc);

            db.SubmitChanges();

            db.ContractCategoryPermission.InsertAllOnSubmit(request.Permissions.Select(p => new ContractCategoryPermission
            {
                ContractCategoryID = cc.ContractCategoryID,
                CreateUID = cc.CreateUID,
                CreateDate = p.CreateDate,
                UID = p.UID,
            }));

            db.SubmitChanges();
        }

        /// <summary>
        /// 刪除合約分類及權限
        /// </summary>
        /// <param name="request"></param>
        public void DeleteContractCategroy(ContractCategoryDeleteRequest request)
        {
            using var db = new DCDataContext();

            ContractCategory cc = db.ContractCategory.FirstOrDefault(c => c.ContractCategoryID == request.ContractCategoryID) 
                                  ?? throw new Exception("Contract category not found.");

            // 刪除合約分類
            db.ContractCategory.DeleteOnSubmit(cc);

            // 刪除合約分類權限
            db.ContractCategoryPermission.DeleteAllOnSubmit(
                db.ContractCategoryPermission.Where(p => p.ContractCategoryID == request.ContractCategoryID));


            db.SubmitChanges();
        }

        /// <summary>
        /// 取得公司所有使用者
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public IEnumerable<UserInfoModel> GetCompanyUsers(ContractCategoryQueryModel request)
        {
            using var db = new DCDataContext();

            var users = from u in db.UserProfile
                        join o in db.OrganizationUser on u.UID equals o.UID
                        where o.CompanyID == request.CompanyID
                        select new UserInfoModel
                        {
                            KeyID = u.UID.EncryptKey(),
                            UserName = $"{u.PID}({u.EMail})",
                        };

            return users.ToList();
        }

        /// <summary>
        /// 修改合約分類及權限
        /// </summary>
        /// <param name="request"></param>
        /// <exception cref="Exception"></exception>
        public void ModifyContractCategroy(ContractCategoryModifyRequest request)
        {
            using var db = new DCDataContext();

            ContractCategory cc = db.ContractCategory.FirstOrDefault(c => c.ContractCategoryID == request.ContractCategoryID) 
                                  ?? throw new Exception("Contract category not found.");

            cc.CategoryName = request.CategoryName;
            cc.ModifyUID = request.ModifyUID;
            cc.ModifyDate = request.ModifyDate;
            cc.Code = request.Code;

            db.ContractCategoryPermission.DeleteAllOnSubmit(
                db.ContractCategoryPermission.Where(p => p.ContractCategoryID == request.ContractCategoryID));

            if (!request.Permissions.Any())
            {
                db.SubmitChanges();
                return;
            }

            db.ContractCategoryPermission.InsertAllOnSubmit(request.Permissions.Select(p => new ContractCategoryPermission
            {
                ContractCategoryID = cc.ContractCategoryID,
                CreateUID = cc.CreateUID,
                CreateDate = p.CreateDate,
                UID = p.UID,
            }));

            db.SubmitChanges();
        }
        /// <summary>
        /// 合約分類搜尋
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public IEnumerable<ContractCategoryInfoModel> QuertyContractCategory(ContractCategoryQueryModel request)
        {
            using var db = new DCDataContext();

            var queryInfos = from cc in db.ContractCategory
                        join o in db.Organization on cc.CompanyID equals o.CompanyID
                        where cc.CompanyID == request.CompanyID
                        select new ContractCategoryInfoModel
                        {
                            KeyID = cc.ContractCategoryID.EncryptKey(),
                            CategoryName = cc.CategoryName,
                            Code = cc.Code,
                            CompanyID = cc.CompanyID,
                            CompanyName = o.CompanyName
                        };

            if (!string.IsNullOrEmpty(request.Keyword))
            {
                queryInfos = queryInfos.Where(x => x.CategoryName.Contains(request.Keyword) || x.Code.Contains(request.Keyword));
            }

            var categoryInfos = queryInfos.ToList();

            var categoryIds = categoryInfos.Select(x => x.ContractCategoryID).ToList();

            var permissionQuery = from cp in db.ContractCategoryPermission
                              join u in db.UserProfile on cp.UID equals u.UID
                              where categoryIds.Contains(cp.ContractCategoryID)
                              select new
                              {
                                  cp.ContractCategoryID,
                                  Info = new ContractCategoryPermissionInfoModel
                                  {
                                      //ContractCategoryPermissionID = cp.ContractCategoryPermissionID,
                                      KeyID = cp.UID.EncryptKey(),
                                      UserName = $"{u.PID}({u.EMail})"
                                  }
                              };
            var permissionInfos = permissionQuery.ToList()
                .GroupBy(x => x.ContractCategoryID)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Info).ToList());

            foreach (var categoryInfo in categoryInfos)
            {
                if (permissionInfos.TryGetValue(categoryInfo.ContractCategoryID, out var permissions))
                {
                    categoryInfo.Permissions = permissions;

                }
            }

            return categoryInfos;
        }
    }
}
