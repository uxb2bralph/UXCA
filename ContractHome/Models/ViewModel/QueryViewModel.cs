using ContractHome.Helper;
using ContractHome.Helper.Validation;
using ContractHome.Models.DataEntity;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Web;

namespace ContractHome.Models.ViewModel
{
    public partial class QueryViewModel
    {
        public int? PageSize { get; set; } = 10;
        public int? PageIndex { get; set; }
        public int? PageOffset { get; set; } = 0;
        public string[]? SortName { get; set; }
        public int?[]? SortType { get; set; }
        public bool? Paging { get; set; }
        public String? KeyID { get; set; }
        public String? FileDownloadToken { get; set; }
        public String? PageMainTitle { get; set; }
        public int? RecordCount { get; set; }
        public String[]? KeyItems { get; set; }
        [JsonIgnore]
        public bool? InitQuery { get; set; }
        public String? Message { get; set; }
        public String? UrlAction { get; set; }
        [JsonIgnore]
        public String? AlertMessage { get; set; }
        public String? EncodedAlertMessage
        {
            get => AlertMessage != null ? Convert.ToBase64String(Encoding.Default.GetBytes(AlertMessage)) : null;
            set
            {
                if (value != null)
                {
                    AlertMessage = Encoding.Default.GetString(Convert.FromBase64String(value));
                }
            }
        }

        public String? AlertTitle { get; set; }
        public String? DialogID { get; set; }
        public bool? ReuseModal { get; set; }
        public DataTableColumn[]? KeyItem { get; set; }
        public String? EncKeyItem { get; set; }
        public DataTableColumn[]? DataItem { get; set; }
        public String? Term { get; set; }
        public DataResultMode? ResultMode { get; set; }

    }

    public enum DataResultMode
    {
        Display = 0,
        Print = 1,
        Download = 2,
        DataContent = 3,
        ForExcel = 4,
    }

    public partial class SignContractViewModel : QueryViewModel
    {
        public String? SignDate { get; set; }
        public String? BuyerIdNo { get; set; }
        public String? BuyerAddress { get; set; }
        public String? BuyerName { get; set; }
        public String? PayWeekDate { get; set; }
        public String? EndDate { get; set; }
        public String? CreditDate { get; set; }
        public String? Amount { get; set; }
        public String? ContractNo { get; set; }
        public String? BuyerSeal { get; set; }
        public String? SellerSeal { get; set; }
        public bool? UseTemplate { get; set; }
        public int? ContractID { get; set; }
        public int? InitiatorID => (string.IsNullOrEmpty(Initiator)) ? null : Initiator.DecryptKeyValue();
        public String? Initiator { get; set; }
        public int? InitiatorIntent { get; set; }
        public int? ContractorID => (string.IsNullOrEmpty(Contractor)) ? null : Contractor.DecryptKeyValue();
        //public String[]? MultiContractor { get; set; }
        public String? Contractor { get; set; }
        public int? ContractorIntent { get; set; }
        public bool? Preview { get; set; }
        public String? Title { get; set; }
        public bool? IgnoreSeal { get; set; }
        public bool? IsJointContracting { get; set; }

        //public ContractorSignaturePosition[]? SignaturePositions { get; set; }
        public ContractorObj[]? Contractors { get; set; }
        public string? EncUID { get; set; }
        //public int? UID => Int32.Parse(HttpUtility.UrlDecode(this.EncUID!.).DecryptData());
        //public string? EncContractorID { get; set; }
        public int? ContractQueryStep { get; set; }

    }
    //wait to remove
    public class ContractorObj
    {
        public string? Contractor { get; set; }
        public int? ContractorID => (string.IsNullOrEmpty(this.Contractor)) ? null : this.Contractor?.DecryptKeyValue();
        public SignaturePosition[]? SignaturePositions { get; set; }
    }

    //wait to replace by field
    public class SignaturePosition
    {
        public string ID { get; set; }
        public double ScaleWidth { get; set; }
        public double ScaleHeight { get; set; }
        public double MarginTop { get; set; }
        public double MarginLeft { get; set; }
        public int PageIndex { get; set; }
        //0:簽章 1:文字
        public int Type { get; set; }
    }

    public class Field
    {
        public string ID { get; set; }
        public double ScaleWidth { get; set; }
        public double ScaleHeight { get; set; }
        public double MarginTop { get; set; }
        public double MarginLeft { get; set; }
        public int PageIndex { get; set; }
        //0:default 1:文字 2.地址 3.電話 4.日期 5.公司Title 6.印章 7.簽名 8.圖片 ... 擴充?
        public string Type { get; set; }
    }

    public class ContractQueryViewModel : SignContractViewModel
    {
        public string? ContractDateFrom { get; set; }
        public string? ContractDateTo { get; set; }
        public CDS_Document.StepEnum[]? QueryStep { get; set; }
    }

    public class SealRequestViewModel : SignContractViewModel
    {
        public int? SealID { get; set; }
        public double? SealScale { get; set; }
        public double? MarginTop { get; set; }
        public double? MarginLeft { get; set; }
    }

    public class SignatureRequestViewModel : SealRequestViewModel
    {
        public int? CompanyID { get; set; }
        public String? Note { get; set; }
        public bool? DoAllPages { get; set; }
        public string? Signature { get; set; }
        public bool? IsTrust { get; set; }
    }

    public class TemplateResourceViewModel : QueryViewModel
    {
        public String? FilePath { get; set; }
    }

    public partial class DataTableQueryViewModel : QueryViewModel
    {
        public String? TableName { get; set; }
        public String? CommandText { get; set; }
        public String? ConnectionString { get; set; }
        //public KeyValuePair<String,Object>[] DataItem { get; set; }
    }

    public partial class DataTableColumn
    {
        public String? Name { get; set; }
        public String? Value { get; set; }
    }

    public partial class DataItemValue
    {
        public String? Name { get; set; }
        public Object? Value { get; set; }
    }

    public class LoginViewModel : QueryViewModel
    {
        [Required(ErrorMessage = "Please enter {0}")]
        [Display(Name = "PID")]
        //[EmailAddress]
        public string? PID { get; set; }

        [Required(ErrorMessage = "Please enter {0}")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool? RememberMe { get; set; }

        public String? ReturnUrl { get; set; }

        [Display(Name = "ValidCode")]
        [CaptchaValidation("EncryptedCode", ErrorMessage = "驗證碼錯誤！")]
        public string? ValidCode { get; set; }

        [Display(Name = "EncryptedCode")]
        public string? EncryptedCode { get; set; }
    }

    public class OrganizationViewModel : QueryViewModel
    {
        public String? ContactName { get; set; }
        public String? Fax { get; set; }
        public String? LogoURL { get; set; }
        public String? CompanyName { get; set; }
        public int? CompanyID { get; set; }
        public String? ReceiptNo { get; set; }
        public String? Phone { get; set; }
        public String? ContactFax { get; set; }
        public String? ContactPhone { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime? CreationDate { get; set; }
        public String? ContactMobilePhone { get; set; }
        public String? BusinessContactPhone { get; set; }

        public String? RegAddr { get; set; }
        public String? UndertakerName { get; set; }
        public String? Addr { get; set; }
        public String? EnglishName { get; set; }
        public String? EnglishAddr { get; set; }
        public String? EnglishRegAddr { get; set; }
        public String? ContactEmail { get; set; }
        public String? UndertakerPhone { get; set; }
        public String? UndertakerFax { get; set; }
        public String? UndertakerMobilePhone { get; set; }
        public String? UndertakerID { get; set; }
        public String? ContactTitle { get; set; }
        public int? CurrentLevel { get; set; }
        public String? AuthorizationNo { get; set; }
        public String[]? ItemName { get; set; }
        public bool? CreateContract { get; set; }
        public string? BelongToCompany { get; set; }
    }

    public partial class UserProfileViewModel : QueryViewModel
    {
        public String? PID { get; set; }
        public String? UserName { get; set; }
        public String? Password { get; set; }
        public String? EMail { get; set; }
        public String? Address { get; set; }
        public String? Phone { get; set; }
        public String? Region { get; set; }

        public String? MobilePhone { get; set; }
        public bool? WaitForCheck { get; set; }
        public Guid? ResetID { get; set; }
        public bool? ResetPass { get; set; }
        public String? EncCompanyID { get; set; }
        public UserRoleDefinition.RoleEnum? RoleID { get; set; }
        public int? UID { get; set; }
        public String? SealData { get; set; }
    }

    public class UserPasswordChangeViewModel : QueryViewModel
    {
        public string? OldPassword { get; set; }

        public string? NewPassword { get; set; }

        public string? EncPID { get; set; }
        [JsonIgnore]
        public string? PID => HttpUtility.UrlDecode(this.EncPID);
    }

    public class GetContractorsViewModel : QueryViewModel
    {
        [Required]
        public string EncPID { get; set; }
        [JsonIgnore]
        public string PID => HttpUtility.UrlDecode(this.EncPID);
    }

    // 使用者授權通知
    public class AuthNotify
    {
        /// <summary>
        /// 操作結果代碼，1 代表成功，非 1 代表失敗，例如：用戶點了失效的連結
        /// </summary>
        public int result { get; set; } = 0;
        /// <summary>
        /// 操作結果訊息
        /// </summary>
        public string resultMessage { get; set; } = string.Empty;
        /// <summary>
        /// 使用者的電子郵件
        /// </summary>
        public string email { get; set; } = string.Empty;
        /// <summary>
        /// 允許簽章的最後期限 GRA server 時間 失敗時為 空值
        /// </summary>
        public string expDate { get; set; } = string.Empty;
        /// <summary>
        /// 交易編號
        /// </summary>
        public string tid { get; set; } = string.Empty;
    }

    /// <summary>
    /// 憑證申請完成通知
    /// </summary>
    public class CertApply
    {
        /// <summary>
        /// email
        /// </summary>
        public string email { get; set; } = string.Empty;
        /// <summary>
        /// 折扣碼
        /// </summary>
        public string discountCode { get; set; } = string.Empty;
        /// <summary>
        /// 何時建立
        /// </summary>
        public string whenCreated { get; set; } = string.Empty;
        /// <summary>
        /// 憑證序號
        /// </summary>
        public string certSerial { get; set; } = string.Empty;
    }
}
