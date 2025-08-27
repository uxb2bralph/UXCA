using ContractHome.Models.DataEntity;
using FluentValidation;
using static ContractHome.Services.FavoriteSignerManage.FavoriteSignerDtos;

namespace ContractHome.Services.FavoriteSignerManage
{
    public class FavoriteSignerDeleteRequest : FavoriteSignerInfoModel
    {
        public class Validator : AbstractValidator<FavoriteSignerDeleteRequest>
        {
            public Validator()
            {
                RuleFor(x => x)
                    .NotEmpty()
                    .Must(IsValidFavoriteSignerID)
                    .WithMessage("常用簽署人ID不存在");
            }

            private bool IsValidFavoriteSignerID(FavoriteSignerDeleteRequest request)
            {
                var db = new DCDataContext();
                var cc = db.FavoriteSigner
                        .Where(x => x.FavoriteSignerID == request.FavoriteSignerID && x.CreateUID == request.CreatorUID)
                        .FirstOrDefault();
                return cc != null;
            }
        }
    }
}
