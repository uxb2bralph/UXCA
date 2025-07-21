namespace ContractHome.Services.ContractCategroyManage
{
    /// <summary>
    /// 合約分類Service介面
    /// </summary>
    public interface IContractCategoryService
    {
        /// <summary>
        /// 建立合約分類及授權
        /// </summary>
        /// <param name="request"></param>
        public void CreateContractCategroy(ContractCategoryCreateRequest request);

        /// <summary>
        /// 修改合約分類及授權
        /// </summary>
        /// <param name="request"></param>
        public void ModifyContractCategroy(ContractCategoryModifyRequest request);

        /// <summary>
        /// 刪除合約分類
        /// </summary>
        /// <param name="request"></param>
        public void DeleteContractCategroy(ContractCategoryDeleteRequest request);

        /// <summary>
        /// 搜尋合約分類
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public IEnumerable<ContractCategoryInfoModel> QuertyContractCategory(ContractCategoryQueryModel request);
    }
}
