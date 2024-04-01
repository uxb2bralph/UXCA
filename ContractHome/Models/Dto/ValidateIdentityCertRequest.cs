using ContractHome.Helper;
using FluentValidation;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ContractHome.Models.Dto
{
    public class ValidateIdentityCertRequest 
    {
        //public string B64Cert { get; set; }
        //public string BindingEmail { get; set; }
        public string Signature { get; set; }
        //public string EUID { get; set; }

        //private int UID => EUID.DecryptKeyValue();

        public class Validator : AbstractValidator<ValidateIdentityCertRequest>
        {
            public Validator()
            {

                this.RuleFor(x => x.Signature)
                    .NotEmpty()
                    .Must(y => GeneralValidator.TryFromBase64String(y));


                //this.RuleFor(x => x.EUID)
                //    .NotEmpty()
                //    .Must(y => GeneralValidator.TryDecryptKeyValue(y));

            }
        }
    }
}