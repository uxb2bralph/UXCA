using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TWCACAPIAdapter.Helper
{
    public static class VirtualPathUtility
    {
        public static String ToAbsolute(this String path)
        {
            return path.Replace("~", Properties.Settings.Default.ApplicationPath);
        }
    }
}
