using ContractHome.Services.ContractCategroy;
using FluentValidation;

namespace ContractHome.Services.ContractCategroyManage
{
    /// <summary>
    /// 合約分類建立
    /// </summary>
    public class ContractCategroyCreateRequest : ContractCategroyModel
    {
        public IEnumerable<ContractCategroyPermissionCreateRequest> Permissions { get; set; } = [];

        public class Validator : AbstractValidator<ContractCategroyCreateRequest>
        {
            public Validator()
            {
                RuleFor(x => x.CompanyID)
                    .NotEmpty()
                    .GreaterThan(0);

                RuleFor(x => x.CategoryName)
                    .NotEmpty()
                    .MaximumLength(50);

                RuleFor(x => x.CreateUID)
                    .NotEmpty()
                    .GreaterThan(0);

                // 假如 Permissions 為空，則不需要驗證 Permissions 的內容 但如果有資料則需要驗證
                When(x => x.Permissions != null && x.Permissions.Any(), () =>
                {
                    RuleForEach(x => x.Permissions)
                    .SetValidator(new ContractCategroyPermissionCreateRequest.Validator());
                });
            }
        }
    }
}
