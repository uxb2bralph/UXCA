using ContractHome.Models.DataEntity;
using ContractHome.Services.ContractCategroy;
using FluentValidation;

namespace ContractHome.Services.ContractCategroyManage
{
    public class ContractCategoryModifyRequest : ContractCategoryModel
    {
        public IEnumerable<ContractCategoryPermissionCreateRequest> Permissions { get; set; } = [];

        public class Validator : AbstractValidator<ContractCategoryModifyRequest>
        {
            public Validator()
            {
                RuleFor(x => x.ContractCategoryID)
                    .NotEmpty()
                    .GreaterThan(0)
                    .Must(IsValidContractCategoryID);

                RuleFor(x => x.CategoryName)
                    .NotEmpty()
                    .MaximumLength(50);

                RuleFor(x => x.Code)
                    .NotEmpty()
                    .MaximumLength(50);

                RuleFor(x => x).Must(x => IsValidCode(x))
                    .WithMessage("分類代碼已存在");

                // 假如 Permissions 為空，則不需要驗證 Permissions 的內容 但如果有資料則需要驗證
                When(x => x.Permissions != null && x.Permissions.Any(), () =>
                {
                    RuleForEach(x => x.Permissions)
                    .SetValidator(new ContractCategoryPermissionCreateRequest.Validator());
                });
            }

            private bool IsValidContractCategoryID(int contractCategroyID)
            {
                var db = new DCDataContext();

                var cc = db.ContractCategory.Where(x => x.ContractCategoryID == contractCategroyID).FirstOrDefault();

                return cc != null;
            }

            private bool IsValidCode(ContractCategoryModifyRequest c)
            {
                var db = new DCDataContext();

                var cc = db.ContractCategory.Where(x => x.Code == c.Code && x.ContractCategoryID != c.ContractCategoryID).FirstOrDefault();

                return cc == null;
            }
        }
    }
}
