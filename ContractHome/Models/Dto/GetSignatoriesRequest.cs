using ContractHome.Helper;
using ContractHome.Models.ViewModel;
using FluentValidation;

namespace ContractHome.Models.Dto
{
    public class GetSignatoriesRequest
    {
        public string ContractID { get; set; }
        public string? EncUID { get; set; }
        public class Validator : AbstractValidator<GetSignatoriesRequest>
        {
            public Validator()
            {
                this.RuleFor(x => x.ContractID)
                    .NotEmpty()
                    .Must(y => GeneralValidator.TryDecryptKeyValue(y));

            }
        }
    }
}
