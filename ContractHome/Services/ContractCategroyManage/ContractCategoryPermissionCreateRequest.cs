using ContractHome.Models.DataEntity;
using ContractHome.Services.ContractCategroyManage;
using FluentValidation;

namespace ContractHome.Services.ContractCategroy
{
    public class ContractCategoryPermissionCreateRequest : ContractCategoryPermissionModel
    {
        public class Validator : AbstractValidator<ContractCategoryPermissionCreateRequest>
        {
            public Validator()
            {
                RuleFor(x => x.UID)
                    .NotEmpty()
                    .GreaterThan(0)
                    .Must(IsValidUID)
                    .WithMessage("查無授權帳號");

                //RuleFor(x => x.CreateUID)
                //    .NotEmpty()
                //    .GreaterThan(0);
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
