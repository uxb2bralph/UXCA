using ContractHome.Helper;
using ContractHome.Models.DataEntity;

namespace ContractHome.Services.ContractCategroyManage
{
    /// <summary>
    /// 合約分類service
    /// </summary>
    public class ContractCategroyService : IContractCategroyService
    {
        /// <summary>
        /// 建立合約分類及權限
        /// </summary>
        /// <param name="request"></param>
        public void CreateContractCategroy(ContractCategroyCreateRequest request)
        {
            using var db = new DCDataContext();

            ContractCategory cc = new()
            {
                CategoryName = request.CategoryName,
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
        public void DeleteContractCategroy(ContractCategroyDeleteRequest request)
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
        /// 修改合約分類及權限
        /// </summary>
        /// <param name="request"></param>
        /// <exception cref="Exception"></exception>
        public void ModifyContractCategroy(ContractCategroyModifyRequest request)
        {
            using var db = new DCDataContext();

            ContractCategory cc = db.ContractCategory.FirstOrDefault(c => c.ContractCategoryID == request.ContractCategoryID) 
                                  ?? throw new Exception("Contract category not found.");

            cc.CategoryName = request.CategoryName;
            cc.ModifyUID = request.ModifyUID;
            cc.ModifyDate = request.ModifyDate;

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
    }
}
