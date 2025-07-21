using ContractHome.Models.DataEntity;
using ContractHome.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractHome.Controllers
{
    [RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin])]
    public class ContractGategoryController(IServiceProvider serviceProvider) : SampleController(serviceProvider)
    {
        /// <summary>
        /// 合約分類列表
        /// </summary>
        /// <returns></returns>
        public IActionResult ContractGategoryList()
        {
            return View();
        }
    }
}
