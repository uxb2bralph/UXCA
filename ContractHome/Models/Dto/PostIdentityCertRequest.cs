using ContractHome.Helper;
using FluentValidation;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ContractHome.Models.Dto
{
    public class PostIdentityCertRequest: ValidateIdentityCertRequest
    {
        public string B64Cert { get; set; }

        public class Validator : AbstractValidator<PostIdentityCertRequest>
        {
            public Validator()
            {
                this.RuleFor(x => x.B64Cert)
                    .NotEmpty()
                    .Must(y=> GeneralValidator.TryFromBase64String(y));

            }
        }
    }
}