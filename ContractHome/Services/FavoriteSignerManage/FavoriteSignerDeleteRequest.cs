using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using FluentValidation;
using static ContractHome.Services.FavoriteSignerManage.FavoriteSignerDtos;

namespace ContractHome.Services.FavoriteSignerManage
{
    public class FavoriteSignerDeleteRequest : FavoriteSignerInfoModel
    {
        public class Validator : AbstractValidator<FavoriteSignerDeleteRequest>
        {
            private readonly IHttpContextAccessor _context;
            private readonly DCDataContext _db;

            public Validator(IHttpContextAccessor context, DCDataContext db)
            {
                _context = context;
                _db = db;

                RuleFor(x => x)
                    .NotEmpty()
                    .Must(IsValidFavoriteSignerID)
                    .WithMessage("常用簽署人ID不存在");
            }

            private bool IsValidFavoriteSignerID(FavoriteSignerDeleteRequest request)
            {
                var creatorUID = GetCreatorUID();

                if (creatorUID == -1)
                {
                    return false;
                }

                request.CreatorUID = creatorUID;

                var cc = _db.FavoriteSigner
                        .Where(x => x.FavoriteSignerID == request.FavoriteSignerID && x.CreateUID == request.CreatorUID)
                        .FirstOrDefault();
                return cc != null;
            }

            private int GetCreatorUID()
            {
                var user = _context.HttpContext.GetUser();
                return user?.UID ?? -1;
            }
        }
    }
}
