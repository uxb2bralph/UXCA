using ContractHome.Models.DataEntity;
using FluentValidation;

namespace ContractHome.Services.ContractCategroyManage
{
    /// <summary>
    /// 刪除合約分類請求
    /// </summary>
    public class ContractCategoryDeleteRequest : ContractCategoryModel
    {
        public class Validator : AbstractValidator<ContractCategoryDeleteRequest>
        {
            public Validator()
            {
                RuleFor(x => x.ContractCategoryID)
                    .NotEmpty()
                    .GreaterThan(0)
                    .Must(IsValidContractCategoryID)
                    .WithMessage("合約分類ID不存在")
                    .Must(IsExecutingContractCategoryID)
                    .WithMessage("此合約分類已在合約上做設定");
            }

            private bool IsValidContractCategoryID(int contractCategroyID)
            {
                var db = new DCDataContext();

                var cc = db.ContractCategory.Where(x => x.ContractCategoryID == contractCategroyID).FirstOrDefault();

                return cc != null;
            }

            private bool IsExecutingContractCategoryID(int contractCategroyID)
            {
                var db = new DCDataContext();

                var cc = db.Contract.Where(x => x.ContractCategoryID == contractCategroyID).FirstOrDefault();

                return cc != null;
            }
        }
    }
}
