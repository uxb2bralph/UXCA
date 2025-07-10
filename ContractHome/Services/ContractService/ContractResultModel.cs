namespace ContractHome.Services.ContractService
{
    /// <summary>
    /// 合約結果Model
    /// </summary>
    public class ContractResultModel
    {
        public MsgRes msgRes { get; set; } = new MsgRes();
    }

    public class MsgRes
    {
        public string type { get; set; } = ContractResultType.S.ToString();
        public string code { get; set; } = string.Empty;
        public string desc { get; set; } = string.Empty;
    }

    public enum ContractResultType
    {
        /// <summary>
        /// 失敗
        /// </summary>
        F,
        /// <summary>
        /// 錯誤
        /// </summary>
        E,
        /// <summary>
        /// 成功
        /// </summary>
        S
    }

    public enum ContractResultCode
    {
        /// <summary>
        /// 合約資訊驗證
        /// </summary>
        ContractInfoVerify = 1,
        /// <summary>
        /// 合約建立
        /// </summary>
        ContractCreate = 2,
        /// <summary>
        /// 合約上傳
        /// </summary>
        ContractUpdate = 3,
        /// <summary>
        /// 合約下載
        /// </summary>
        ContractDownload = 4,
        /// <summary>
        /// UXSIGN系統錯誤
        /// </summary>
        SystemError = 99,
    }

    public static class ContractResultCodeExtension
    {

        /// <summary>
        /// 取得合約結果代碼
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetFullCode(this ContractResultCode value)
        {
            return ICustomContractService.ResultCodeHeader + ((int)value).ToString("D2");
        }
    }
}
