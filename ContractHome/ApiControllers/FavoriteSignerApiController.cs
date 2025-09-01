using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Services.FavoriteSignerManage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static ContractHome.Services.FavoriteSignerManage.FavoriteSignerDtos;

namespace ContractHome.ApiControllers
{
    [Route("api/FavoriteSigner")]
    [ApiController]
    [Authorize]
    public class FavoriteSignerApiController(IFavoriteSignerService favoriteSignerService) : ControllerBase
    {
        private readonly IFavoriteSignerService _favoriteSignerService = favoriteSignerService;

        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> CreateFavoriteSignerAsync([FromBody] FavoriteSignerCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            _favoriteSignerService.CreateFavoriteSigner(request);
            return Ok(new BaseResponse());
        }

        [HttpGet]
        [Route("Query")]
        public async Task<IActionResult> QueryFavoriteSignerAsync()
        {
            var profile = await HttpContext.GetUserAsync();
            var result = _favoriteSignerService.QueryFavoriteSigner(profile.UID);
            return Ok(new BaseResponse()
            {
                Data = result
            });
        }

        [HttpPost]
        [Route("Delete")]
        public IActionResult DeleteFavoriteSigner([FromBody] FavoriteSignerDeleteRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            _favoriteSignerService.DeleteFavoriteSigner(request);
            return Ok(new BaseResponse());
        }

        [HttpPost]
        [Route("SearchCompany")]
        public IActionResult SearchCompany([FromBody] QueryInfoModel query)
        {
            var result = _favoriteSignerService.SearchCompany(query);

            return Ok(new BaseResponse()
            {
                Data = result
            });
        }

        [HttpPost]
        [Route("SearchSigner")]
        public IActionResult SearchSigner([FromBody] QueryInfoModel query)
        {
            var result = _favoriteSignerService.SearchSigner(query);

            return Ok(new BaseResponse()
            {
                Data = result
            });
        }
    }
}
