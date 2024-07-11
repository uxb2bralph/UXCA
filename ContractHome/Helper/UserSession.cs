using Microsoft.AspNetCore.Mvc;

namespace ContractHome.Helper
{
    public class UserSession
    { 
        public UserSession()
        {
            IsTrust = true;
        }

        public bool IsTrust { get; }
        //public int CompanyId { get; set; } = 0;

        public static UserSession Create(IHttpContextAccessor httpContextAccessor)
        {
            var mySession = Get(httpContextAccessor);
            if (mySession == null)
            {
                mySession = new UserSession();
                httpContextAccessor.HttpContext.Session.SetObject("__UserSession__", mySession);
            }
            return mySession;
        }

        public static UserSession Get(IHttpContextAccessor httpContextAccessor)
        {
            return httpContextAccessor.HttpContext.Session.GetObject<UserSession>("__UserSession__");
        }

        public static void Remove(IHttpContextAccessor httpContextAccessor)
        {
            httpContextAccessor.HttpContext.Session.Remove("__UserSession__");
        }
    }
}
