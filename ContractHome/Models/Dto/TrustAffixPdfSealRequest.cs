using ContractHome.Helper;
using ContractHome.Models.ViewModel;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentValidation;

namespace ContractHome.Models.Dto
{
    public class TrustAffixPdfSealRequest: QueryViewModel
    {
        public string KeyID { get; set; }
        public int? ContractID { get; set; }
        public int UID { get; set; }

        public class Validator:AbstractValidator<TrustAffixPdfSealRequest>
        {
            public Validator() 
            {
                this.RuleFor(x => x.KeyID)
                    .NotNull()
                    .Must(y=>GeneralValidator.TryDecryptKeyValue(y));
                this.RuleFor(x => x.UID)
                    .NotNull();
            }
        }

        public override string ToString()
        {
            return $"{this.GetType().Name}: KeyID=" + KeyID + " ContractID=" + ContractID + " UID="+ UID;
        }
    }
}
