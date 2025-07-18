using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace ContractHome.Security.Authorization
{
    public class AuthorizedSysAdminAttribute : TypeFilterAttribute
    {
        public AuthorizedSysAdminAttribute() : base(typeof(RoleRequirementFilter))
        {
            Arguments = [(Func<UserProfile, string, bool>)((u, x) => u.IsAuthorized(x, [(int)UserRoleDefinition.RoleEnum.SystemAdmin]))];
        }
    }

    public class UserAuthorizeAttribute : TypeFilterAttribute
    {
        public UserAuthorizeAttribute() : base(typeof(RoleRequirementFilter))
        {
            Arguments = [(Func<UserProfile, string, bool>)((u, x) => u.IsAuthorized(x, [(int)UserRoleDefinition.RoleEnum.User, (int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin]))];
        }
    }


    public class RoleAuthorizeAttribute : TypeFilterAttribute
    {
        public RoleAuthorizeAttribute(int[] roleID) : base(typeof(RoleRequirementFilter))
        {
            Arguments = [(Func<UserProfile, string, bool>)((u, x) => u.IsAuthorized(x, roleID))];
        }
    }

    public class RoleRequirementFilter : IAuthorizationFilter
    {
        protected Func<UserProfile, string, bool> _auth;

        public RoleRequirementFilter(Func<UserProfile, string, bool> checkAuth)
        {
            _auth = checkAuth;
        }
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context.HttpContext.User.Identity.IsAuthenticated)
            {
                var user = context.HttpContext.User;
                var roleIDs = user.FindFirst("RoleIDs")?.Value ?? "";

                var result = context.HttpContext.GetUserAsync();
                result.Wait();
                if (!_auth(result.Result, roleIDs))
                {
                    context.Result = new RedirectToRouteResult(
                        new RouteValueDictionary
                        {
                                { "controller", "Account" },
                                { "action", "Login" },
                                { "id", string.Empty }
                        });
                }
            }
            else
            {
                // 未登入，轉至登入頁面
                string rtURL = "";
                rtURL = UriHelper.GetEncodedPathAndQuery(context.HttpContext.Request);
                context.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "controller", "Account" },
                    { "action", "Login" },
                    { "ReturnUrl", rtURL }
                });
            }

        }
    }

}