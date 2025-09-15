using ContractHome.Helper;
using ContractHome.Helper.Validation;
using ContractHome.Models.ViewModel;
using FluentValidation;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ContractHome.Models.Dto
{
    /// <summary>
    /// 簽署人資訊
    /// </summary>
    public class SignerInfoModel
    {
        /// <summary>
        /// 公司名稱
        /// </summary>
        public string CompanyName { get; set; } = string.Empty;
        /// <summary>
        /// 公司統編
        /// </summary>
        public string ReceiptNo { get; set; } = string.Empty;
        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; } = string.Empty;

        public bool IsInitiator { get; set; } = false;
    }

    public class PostConfigRequest
    {
        public string ContractID { get; set; }
        public string ContractCategory { get; set; }
        [JsonIgnore]
        public int ContractCategoryID => (!string.IsNullOrEmpty(ContractCategory)) ? ContractCategory.DecryptKeyValue() : 0;

        public string Title { get; set; }
        public string ContractNo { get; set; }
        public string? ExpiryDateTime { get; set; }
        public bool IsPassStamp { get; set; }
        public IEnumerable<SignerInfoModel> Signatories { get; set; }
        public string? EncUID { get; set; }

        public class Validator:AbstractValidator<PostConfigRequest>
        {

            public Validator() 
            {
                this.RuleFor(x => x.ContractID)
                    .NotEmpty()
                    .Must(y => GeneralValidator.TryDecryptKeyValue(y));

                this.RuleFor(x => x.Title)
                    .NotEmpty();

                this.RuleFor(x => x.ContractNo)
                    .NotEmpty();

                this.RuleFor(x => x)
                    .Must(y => CheckSignerInfo(y.Signatories))
                    .WithMessage("簽署人資訊不完整或Email格式錯誤");

                this.RuleFor(x => x)
                    .Must(y => CheckDuplicateSignerInfo(y.Signatories))
                    .WithMessage("簽署人公司統編或Email重複");

                this.RuleFor(x => x.ExpiryDateTime)
                    .Must(date => CheckExpiryDateTime(date))
                    .WithMessage("若有填寫期限，則必須是今天或之後的日期");

                this.RuleFor(x => x.IsPassStamp)
                    .NotNull();
            }

            private bool CheckSignerInfo(IEnumerable<SignerInfoModel> signatories)
            {
                foreach (var signer in signatories)
                {
                    if (string.IsNullOrEmpty(signer.CompanyName) ||
                        string.IsNullOrEmpty(signer.ReceiptNo) ||
                        string.IsNullOrEmpty(signer.Email) ||
                        !new EmailAddressAttribute().IsValid(signer.Email))
                    {
                        return false;
                    }
                }

                return true;
            }

            // 檢查公司統編及Email是否重複
            private bool CheckDuplicateSignerInfo(IEnumerable<SignerInfoModel> signatories)
            {
                var companySet = new HashSet<string>();
                var emailSet = new HashSet<string>();
                foreach (var signer in signatories)
                {
                    if (!companySet.Add(signer.ReceiptNo) || !emailSet.Add(signer.Email))
                    {
                        return false;
                    }
                }
                return true;
            }

            private bool CheckExpiryDateTime(string date)
            {
                return string.IsNullOrEmpty(date) || GeneralValidator.MustAfterOrIsToday(date);
            }
        }
    }
}
