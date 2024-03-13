using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication;

namespace ContractHome.Services.Jobs
{
    public class DashboardAccessAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            //依據來源IP、登入帳號決定可否存取
            //例如：已登入者可存取
            //wait to do...SysAdmin可存取
            var userId = context.GetHttpContext().User.Identity;
            var isAuthed = userId?.IsAuthenticated ?? false;
            if (!isAuthed)
            {
                // 未設 options.FallbackPolicy = options.DefaultPolicy 的話要加這段
                // 發 Challenge 程序，ex: 回傳 401 觸發登入視窗、導向登入頁面..
                context.GetHttpContext().ChallengeAsync()
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                return false;
            }
            // 檢查登入者
            return true;
        }
        public static bool IsReadOnly(DashboardContext context)
        {
            var clientIp = context.Request.RemoteIpAddress.ToString();
            var isLocal = "127.0.0.1,::1".Split(',').Contains(clientIp);
            //依據來源IP、登入帳號決定可否存取
            //例如：非本機存取只能讀取
            return !isLocal;
        }
    }
}
