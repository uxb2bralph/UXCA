using CommonLib.DataAccess;
using ContractHome.Models.DataEntity;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ContractHome.Services.ContractService
{
    /// <summary>
    /// 客製合約服務介面
    /// </summary>
    public interface ICustomContractService
    {
        /// <summary>
        /// 合約結果代碼
        /// </summary>
        public static readonly string ResultCodeHeader = "UX_";

        ///// <summary>
        ///// 設定 dbContext
        ///// </summary>
        ///// <param name="dbContext"></param>
        //public void SetModels(DCDataContext dbContext);
        /// <summary>
        /// 建立合約
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ContractResultModel CreateContract(ContractModel model);

        /// <summary>
        /// 驗證合約Model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="modelState"></param>
        /// <returns></returns>
        public bool IsValid(ContractModel model, ModelStateDictionary modelState, out ContractResultModel result);

        /// <summary>
        /// 下載合約
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public Task<ContractResultModel> DownloadAsync(HttpRequest httpRequest);
    }
}
