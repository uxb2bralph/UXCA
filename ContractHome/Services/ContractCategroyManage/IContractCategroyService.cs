namespace ContractHome.Services.ContractCategroyManage
{
    /// <summary>
    /// 合約分類Service介面
    /// </summary>
    public interface IContractCategroyService
    {
        /// <summary>
        /// 建立合約分類及授權
        /// </summary>
        /// <param name="request"></param>
        public void CreateContractCategroy(ContractCategroyCreateRequest request);

        /// <summary>
        /// 修改合約分類及授權
        /// </summary>
        /// <param name="request"></param>
        public void ModifyContractCategroy(ContractCategroyModifyRequest request);
    }
}
