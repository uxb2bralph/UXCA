using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using WebHome.Helper;

namespace WebHome.Controllers.Filters
{
    public class ExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            var actionContext = filterContext.HttpContext.RequestServices.GetRequiredService<IActionContextAccessor>().ActionContext;
            if(actionContext != null)
            {
                var urlHelper = new UrlHelper(actionContext);
                //IUrlHelper urlHelper = new UrlHelper(new ActionContext(filterContext.HttpContext, filterContext.RouteData, filterContext.ActionDescriptor));
                //var urlHelper = filterContext.HttpContext.RequestServices.GetRequiredService<IUrlHelper>();
            }

            if (filterContext.Exception != null)
            {
                ApplicationLogging.CreateLogger<ExceptionFilter>().LogError(filterContext.Exception, filterContext.Exception.Message);

                ViewDataDictionary viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = filterContext.Exception
                };
                filterContext.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/Error.cshtml",
                    ViewData = viewData,
                };
            }
        }
    }
}
