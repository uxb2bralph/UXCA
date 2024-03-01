using ContractHome.Helper;
using ContractHome.Helper.Validation;
using ContractHome.Models.ViewModel;
using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace ContractHome.Models.Dto
{
    public class PostConfigRequest
    {
        public string ContractID { get; set; }
        public string Title { get; set; }
        public string ContractNo { get; set; }
        public IEnumerable<string> Signatories { get; set; }
        public string? EncUID { get; set; }


        public PostConfigRequest(string contractID, string title, string contractNo, 
            IEnumerable<string> signatories)
        {
            ContractID = contractID;
            Title = title;
            ContractNo = contractNo;
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
            }
        }
    }
}
