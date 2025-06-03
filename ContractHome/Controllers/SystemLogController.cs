using ContractHome.Models.DataEntity;
using ContractHome.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractHome.Controllers
{
    /// <summary>
    /// 系統Log檔案控制器
    /// </summary>
    /// <param name="serviceProvider"></param>
    [Authorize]
    [RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin])]
    public class SystemLogController(IServiceProvider serviceProvider) : SampleController(serviceProvider)
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
