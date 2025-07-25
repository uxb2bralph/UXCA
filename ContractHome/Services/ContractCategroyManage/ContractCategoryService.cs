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
                        orderby u.UID
                        select new UserInfoModel
                        {
                            KeyID = u.UID.EncryptKey(),
                            Name = $"{u.PID}({u.EMail})",
                        };

            return users.ToList();
        }

        /// <summary>
        /// 取得合約分類資訊
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ContractCategoryInfoModel GetContractCategoryInfo(ContractCategoryQueryModel request)
        {
            using var db = new DCDataContext();

            var cc = from c in db.ContractCategory
                     join o in db.Organization on c.CompanyID equals o.CompanyID
                     where c.ContractCategoryID == request.ContractCategoryID
                     select new ContractCategoryInfoModel
                     {
                         KeyID = c.ContractCategoryID.EncryptKey(),
                         CategoryName = c.CategoryName,
                         Code = c.Code,
                         CompanyID = c.CompanyID,
                         CompanyName = o.CompanyName,
                     };

            var categoryInfo = cc.FirstOrDefault() 
                               ?? throw new Exception("Contract category not found.");

            var permissions = from u in db.UserProfile
                              join o in db.OrganizationUser on u.UID equals o.UID
                              join cp in db.ContractCategoryPermission 
                              on new { o.UID, ContractCategoryID = categoryInfo.ContractCategoryID } equals new { cp.UID, cp.ContractCategoryID } into userPermissions
                              from up in userPermissions.DefaultIfEmpty()
                              where o.CompanyID == categoryInfo.CompanyID
                              orderby u.UID
                              select new ContractCategoryPermissionInfoModel
                              {
                                  KeyID = u.UID.EncryptKey(),
                                  Name = $"{u.PID}({u.EMail})",
                                  Selected = up != null // 如果有對應的權限則為選中狀態
                              };

            categoryInfo.Permissions = permissions.ToList();
            return categoryInfo;
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
                                      Name = $"{u.PID}({u.EMail})",
                                      PID = u.PID,
                                      Email = u.EMail,
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

            if (!string.IsNullOrEmpty(request.Name))
            {
                var name = request.Name.Trim();
                categoryInfos = categoryInfos.Where(x => x.Permissions.Any(p => p.Name.Contains(name) || p.Email.Contains(name))).ToList();
            }

            return categoryInfos;
        }
    }
}
