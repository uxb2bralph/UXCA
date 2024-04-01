using FluentValidation;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static ContractHome.Models.Dto.PostFieldSettingRequest;

namespace ContractHome.Models.Dto
{
    public class RevokeIdentityCertRequest
    {
        public string EUID { get; set; }
        public string ESeqNo { get; set; }
        public class Validator : AbstractValidator<RevokeIdentityCertRequest>
        {
            public Validator()
            {
                this.RuleFor(x => x.EUID)
                    .NotEmpty()
                    .Must(y => GeneralValidator.TryDecryptKeyValue(y));

                this.RuleFor(x => x.ESeqNo)
                    .NotEmpty()
                    .Must(y => GeneralValidator.TryDecryptKeyValue(y));
            }
        }
    }
}