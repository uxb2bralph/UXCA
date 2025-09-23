using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using FluentValidation;
using static ContractHome.Services.FavoriteSignerManage.FavoriteSignerDtos;

namespace ContractHome.Services.FavoriteSignerManage
{
    public class FavoriteSignerCreateRequest : FavoriteSignerInfoModel
    {
        public class Validator : AbstractValidator<FavoriteSignerCreateRequest>
        {
            private readonly IHttpContextAccessor _context;
            private readonly DCDataContext _db;

            public Validator(IHttpContextAccessor context, DCDataContext db)
            {
                _context = context;
                _db = db;

                RuleFor(x => x.Email)
                    .NotEmpty()
                    .EmailAddress()
                    .MaximumLength(100);

                RuleFor(x => x)
                    .NotEmpty()
                    .Must(IsValidSigner)
                    .WithMessage("簽署人Email已存在");

                RuleFor(x => x.CompanyName)
                    .NotEmpty()
                    .MaximumLength(100);

                RuleFor(x => x.ReceiptNo)
                    .NotEmpty().WithMessage("統編不可為空")
                    .Matches(@"^\d{8}$").WithMessage("統編必須是 8 位數字");

                //RuleFor(x => x.CreatorKeyID)
                //    .NotEmpty()
                //    .Must(IsValidUID)
                //    .WithMessage("建立人ID不存在");
            }

            //private bool IsValidUID(string uid)
            //{
            //    var db = new DCDataContext();

            //    var cc = db.UserProfile
            //            .Where(x => x.UID.Equals(uid.DecryptKeyValue()))
            //            .FirstOrDefault();

            //    return cc != null;
            //}

            private bool IsValidSigner(FavoriteSignerCreateRequest request)
            {
                var creatorUID = GetCreatorUID();

                if (creatorUID == -1)
                {
                    return false;
                }

                request.CreatorUID = creatorUID;

                var result = (from u in _db.UserProfile
                              join f in _db.FavoriteSigner on u.UID equals f.SignerUID
                              where u.EMail == request.Email && f.CreateUID == request.CreatorUID
                              select f).FirstOrDefault();
                return result == null;
            }

            private int GetCreatorUID()
            {
                var user = _context.HttpContext.GetUser();
                return user?.UID ?? -1;
            }
        }
    }
}
