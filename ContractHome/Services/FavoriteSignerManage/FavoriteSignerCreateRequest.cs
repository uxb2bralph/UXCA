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
            public Validator()
            {
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
                    .NotEmpty()
                    .MaximumLength(20);

                RuleFor(x => x.CreatorKeyID)
                    .NotEmpty()
                    .Must(IsValidUID)
                    .WithMessage("建立人ID不存在");
            }

            private bool IsValidUID(string uid)
            {
                var db = new DCDataContext();

                var cc = db.UserProfile
                        .Where(x => x.UID.Equals(uid.DecryptKeyValue()))
                        .FirstOrDefault();

                return cc != null;
            }

            private bool IsValidSigner(FavoriteSignerCreateRequest request)
            {
                var db = new DCDataContext();
                var result = from u in db.UserProfile
                             join f in db.FavoriteSigner on u.UID equals f.SignerUID
                             where u.EMail == request.Email && f.CreateUID == request.CreatorUID
                             select f;
                return result == null;
            }
        }
    }
}
