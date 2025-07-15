namespace ContractHome.Services.ContractCategroyManage
{
    public class ContractCategoryBaseModel
    {
        public string Key { get; set; } = string.Empty;
    }

    public class ContractCategoryQueryModel : ContractCategoryBaseModel
    {
        /// <summary>
        /// 公司ID
        /// </summary>
        public int CompanyID { get; set; }
        /// <summary>
        /// 分類名稱
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 合約分類Model
    /// </summary>
    public class ContractCategoryModel : ContractCategoryBaseModel
    {
        /// <summary>
        /// 分類流水號
        /// </summary>
        public int ContractCategoryID { get; set; }
        /// <summary>
        /// 公司ID
        /// </summary>
        public int CompanyID { get; set; }
        /// <summary>
        /// 分類名稱
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;
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
    public class ContractCategroyPermissionModel
    {
        /// <summary>
        /// 合約分類權限流水號
        /// </summary>
        public int ContractCategroyPermissionID { get; set; }
        /// <summary>
        /// 合約分類流水號
        /// </summary>
        public int ContractCategroyID { get; set; }
        /// <summary>
        /// 使用者UID
        /// </summary>
        public int UID { get; set; }
        /// <summary>
        /// 建立人UID
        /// </summary>
        public int CreateUID { get; set; }
        /// <summary>
        /// 建立日期
        /// </summary>
        public DateTime CreateDate { get; set; } = DateTime.Now;
    }
}
