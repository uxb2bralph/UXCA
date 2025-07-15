using ContractHome.Models.DataEntity;
using ContractHome.Services.ContractCategroyManage;
using FluentValidation;

namespace ContractHome.Services.ContractCategroy
{
    public class ContractCategroyPermissionCreateRequest : ContractCategroyPermissionModel
    {
        public class Validator : AbstractValidator<ContractCategroyPermissionCreateRequest>
        {
            public Validator()
            {
                RuleFor(x => x.UID)
                    .NotEmpty()
                    .GreaterThan(0)
                    .Must(IsValidUID).WithMessage("查無此UID");

                RuleFor(x => x.CreateUID)
                    .NotEmpty()
                    .GreaterThan(0);
            }

            private bool IsValidUID(int UID)
            {
                var db = new DCDataContext();

                var u = db.UserProfile
                        .FirstOrDefault(x => x.UID == UID);

                return u != null;
            }
        }
    }
}
