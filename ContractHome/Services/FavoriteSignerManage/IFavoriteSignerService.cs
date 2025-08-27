using static ContractHome.Services.FavoriteSignerManage.FavoriteSignerDtos;

namespace ContractHome.Services.FavoriteSignerManage
{
    public interface IFavoriteSignerService
    {
        public void CreateFavoriteSigner(FavoriteSignerCreateRequest request);

        public IEnumerable<FavoriteSignerInfoModel> QueryFavoriteSigner(int creatorUID);

        public void DeleteFavoriteSigner(FavoriteSignerDeleteRequest request);
    }
}
