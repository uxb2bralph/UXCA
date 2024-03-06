using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContractHome.Helper
{
    public static class VirtualPathUtility
    {
        public static String ToAbsolute(this String path)
        {
            return path.Replace("~", Properties.Settings.Default.ApplicationPath);
        }

        public static string DefaultWebUri(this HttpContext httpContext)
        {      
            return string.Concat(httpContext.Request.Scheme, "://",
                              httpContext.Request.Host.ToUriComponent(),
                              httpContext.Request.PathBase.ToUriComponent()); 
        }
    }

    
}
