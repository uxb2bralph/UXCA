using ContractHome.Helper;
using ContractHome.Models.ViewModel;
using FluentValidation;

namespace ContractHome.Models.Dto
{
    public class GetFieldSettingRequest
    {
        public string ContractID { get; set; }
        public string? CompanyID { get; set; }

        public string? OperatorID { get; set; }

        public class Validator:AbstractValidator<GetFieldSettingRequest>
        {
            public Validator() 
            {
                this.RuleFor(x => x.ContractID)
                    .NotEmpty()
                    .Must(y=>GeneralValidator.TryDecryptKeyValue(y));
                this.RuleFor(x => x.CompanyID)
                    .NotEmpty()
                    .Must(y=>GeneralValidator.TryDecryptKeyValue(y));
            }
        }
    }
}
