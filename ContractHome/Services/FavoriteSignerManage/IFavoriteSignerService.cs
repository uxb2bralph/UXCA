using static ContractHome.Services.FavoriteSignerManage.FavoriteSignerDtos;

namespace ContractHome.Services.FavoriteSignerManage
{
    public interface IFavoriteSignerService
    {
        public (int, int) CreateFavoriteSigner(FavoriteSignerCreateRequest request);

        public IEnumerable<FavoriteSignerInfoModel> QueryFavoriteSigner(int creatorUID);

        public void DeleteFavoriteSigner(FavoriteSignerDeleteRequest request);

        public IEnumerable<CompanyInfoModel> SearchCompany(QueryInfoModel query);

        public IEnumerable<SignerInfoModel> SearchSigner(QueryInfoModel query);
    }
}
