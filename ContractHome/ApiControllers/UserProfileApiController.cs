using ContractHome.Models.Dto;
using ContractHome.Services.UserProfileManage;
using Microsoft.AspNetCore.Mvc;

namespace ContractHome.ApiControllers
{
    [Route("api/UserProfile")]
    [ApiController]
    public class UserProfileApiController(IUserProfileService userProfileService) : ControllerBase
    {
        private readonly IUserProfileService _userProfileService = userProfileService;

        [HttpPost]
        [Route("PIDAndPasswordUpdate")]
        public IActionResult PIDAndPasswordUpdate([FromBody] PIDAndPasswordUpdateRequest request)
        {
            _userProfileService.PIDAndPasswordUpdate(request);
            return Ok(new BaseResponse());
        }
    }
}
