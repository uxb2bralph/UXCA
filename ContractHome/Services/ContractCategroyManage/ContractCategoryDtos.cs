using ContractHome.Helper;
using System.Text.Json.Serialization;

namespace ContractHome.Services.ContractCategroyManage
{
    public class ContractCategoryBaseModel
    {
        public string KeyID { get; set; } = string.Empty;
    }
    /// <summary>
    /// 合約分類基本Model
    /// </summary>
    public class ContractCategoryInfoModel : ContractCategoryBaseModel
    {
        /// <summary>
        /// 分類流水號
        /// </summary>
        [JsonIgnore]
        public int ContractCategoryID => KeyID.DecryptKeyValue();
        /// <summary>
        /// 分類名稱
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;
        /// <summary>
        /// 分類代碼
        /// </summary>
        public string Code { get; set; } = string.Empty;
        /// <summary>
        /// 公司ID
        /// </summary>
        public int CompanyID { get; set; }
        /// <summary>
        /// 公司名稱
        /// </summary>
        public string CompanyName { get; set; } = string.Empty;
        /// <summary>
        /// 授權人員資訊
        /// </summary>
        public IEnumerable<ContractCategoryPermissionInfoModel> Permissions { get; set; } = [];
    }
    /// <summary>
    /// 授權人員資訊Model
    /// </summary>
    public class ContractCategoryPermissionInfoModel : ContractCategoryBaseModel
    {
        /// <summary>
        /// 合約分類權限流水號
        /// </summary>
        [JsonIgnore]
        public int ContractCategoryPermissionID { get; set; }
        /// <summary>
        /// 使用者UID
        /// </summary>
        [JsonIgnore]
        public int UID => KeyID.DecryptKeyValue();
        /// <summary>
        /// 使用者名稱
        /// </summary>
        public string UserName { get; set; } = string.Empty;
    }
    /// <summary>
    /// 合約分類搜尋Model
    /// </summary>
    public class ContractCategoryQueryModel : ContractCategoryBaseModel
    {
        /// <summary>
        /// 公司ID
        /// </summary>
        public int CompanyID => KeyID.DecryptKeyValue();
        /// <summary>
        /// 關鍵字
        /// </summary>
        public string Keyword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 合約分類Model
    /// </summary>
    public class ContractCategoryModel : ContractCategoryBaseModel
    {
        /// <summary>
        /// 分類流水號
        /// </summary>
        [JsonIgnore]
        public int ContractCategoryID => (!string.IsNullOrEmpty(KeyID)) ? KeyID.DecryptKeyValue() : -1;
        /// <summary>
        /// 公司ID
        /// </summary>
        public int CompanyID { get; set; }
        /// <summary>
        /// 分類名稱
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;
        /// <summary>
        /// 分類代碼
        /// </summary>
        public string Code { get; set; } = string.Empty;
        /// <summary>
        /// 建立人UID
        /// </summary>
        public int CreateUID { get; set; }
        /// <summary>
        /// 建立日期
        /// </summary>
        public DateTime CreateDate { get; set; } = DateTime.Now;
        /// <summary>
        /// 修改人UID
        /// </summary>
        public int ModifyUID { get; set; }
        /// <summary>
        /// 修改日期
        /// </summary>
        public DateTime ModifyDate { get; set; } = DateTime.Now;
    }
    /// <summary>
    /// 合約分類權限Model
    /// </summary>
    public class ContractCategoryPermissionModel : ContractCategoryBaseModel
    {
        /// <summary>
        /// 合約分類權限流水號
        /// </summary>
        public int ContractCategoryPermissionID { get; set; }
        /// <summary>
        /// 合約分類流水號
        /// </summary>
        public int ContractCategoryID { get; set; }
        /// <summary>
        /// 使用者UID
        /// </summary>
        public int UID => KeyID.DecryptKeyValue();
        /// <summary>
        /// 建立人UID
        /// </summary>
        public int CreateUID { get; set; }
        /// <summary>
        /// 建立日期
        /// </summary>
        public DateTime CreateDate { get; set; } = DateTime.Now;
    }

    public class UserInfoModel : ContractCategoryBaseModel
    {
        /// <summary>
        /// 使用者名稱
        /// </summary>
        public string UserName { get; set; } = string.Empty;
    }
}
