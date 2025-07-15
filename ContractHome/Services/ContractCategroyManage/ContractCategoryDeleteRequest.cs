using ContractHome.Models.DataEntity;
using FluentValidation;

namespace ContractHome.Services.ContractCategroyManage
{
    public class ContractCategroyDeleteRequest : ContractCategoryModel
    {
        public class Validator : AbstractValidator<ContractCategroyDeleteRequest>
        {
            public Validator()
            {
                RuleFor(x => x.ContractCategoryID)
                    .NotEmpty()
                    .GreaterThan(0)
                    .Must(IsValidContractCategroyID);
            }

            private bool IsValidContractCategroyID(int contractCategroyID)
            {
                var db = new DCDataContext();

                var cc = db.ContractCategory.Where(x => x.ContractCategoryID == contractCategroyID).FirstOrDefault();

                return cc != null;
            }
        }
}
