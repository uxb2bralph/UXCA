using System.ComponentModel.DataAnnotations;
using ContractHome.Helper.Validation;

namespace ContractHome.Services.ContractService
{
    /// <summary>
    /// 建立合約Model
    /// </summary>
    public class ContractModel
    {
        /// <summary>
        /// 合約編號
        /// </summary>
        [Required(ErrorMessage = "合約編號為必填")]
        public string ContractNo { get; set; } = string.Empty;
        /// <summary>
        /// 合約名稱
        /// </summary>
        [Required(ErrorMessage = "合約名稱為必填")]
        public string Title { get; set; } = string.Empty;
        /// <summary>
        /// 是否直接簽屬 true:直接簽署 false:先用印再簽署
        /// </summary>
        [Required(ErrorMessage = "是否直接簽屬為必填")]
        public bool IsPassStamp { get; set; } = false;
        /// <summary>
        /// 合約到期日
        /// </summary>
        [ValidDate("yyyy/MM/dd", ErrorMessage = "合約到期日格式不正確")]
        public string ExpiryDateTime { get; set; } = "";
        /// <summary>
        /// 通知Mail
        /// </summary>
        [Required(ErrorMessage = "通知Mail為必填")]
        public string NotifyMail { get; set; } = string.Empty;
        /// <summary>
        /// 簽署人集合
        /// </summary>
        public IEnumerable<Signatory> Signatories { get; set; } = [];
    }
    /// <summary>
    /// 簽屬人
    /// </summary>
    public class Signatory
    {
        /// <summary>
        /// 公司統編
        /// </summary>
        public string Identifier { get; set; } = string.Empty;
        /// <summary>
        /// 簽署者名稱(公司名稱)
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 簽署人Mail
        /// </summary>
        public string Mail { get; set; } = string.Empty;
    }
}
