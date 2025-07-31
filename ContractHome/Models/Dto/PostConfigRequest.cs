using ContractHome.Helper;
using ContractHome.Helper.Validation;
using ContractHome.Models.ViewModel;
using FluentValidation;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ContractHome.Models.Dto
{
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
        public IEnumerable<string> Signatories { get; set; }
        public string? EncUID { get; set; }


        public PostConfigRequest(string contractID, string title, string contractNo, string expiryDateTime,
            bool isPassStamp,
            IEnumerable<string> signatories)
        {
            ContractID = contractID;
            Title = title;
            ContractNo = contractNo;
            ExpiryDateTime = expiryDateTime;
            IsPassStamp = isPassStamp;
            Signatories = signatories;
        }


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

                this.RuleForEach(x => x.Signatories)
                    .NotEmpty()
                    .Must(y => GeneralValidator.TryDecryptKeyValue(y));

                //this.RuleFor(x => x.ExpiryDateTime)
                //    .NotNull()
                //    .Must(y => DateTime.TryParseExact(y, "yyyy/MM/dd", null,
                //            DateTimeStyles.None, out DateTime result))
                //    .Must(y => GeneralValidator.MustAfterOrIsToday(y));

                this.RuleFor(x => x.ExpiryDateTime)
                    .Must(date => CheckExpiryDateTime(date))
                    .WithMessage("若有填寫期限，則必須是今天或之後的日期");

                this.RuleFor(x => x.IsPassStamp)
                    .NotNull();
            }

            private bool CheckExpiryDateTime(string date)
            {
                if (string.IsNullOrEmpty(date))
                {
                    return true;
                }

                if (GeneralValidator.MustAfterOrIsToday(date))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
