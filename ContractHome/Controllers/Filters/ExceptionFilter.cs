using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ContractHome.Helper;
using ContractHome.Models;
using System.Diagnostics;
using CommonLib.Core.Utility;
using ContractHome.Models.Dto;
using ContractHome.Properties;
using DocumentFormat.OpenXml.InkML;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ContractHome.Controllers.Filters
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

                //wait to merge into LogError
                FileLogger.Logger.Error($"{Activity.Current?.Id ?? filterContext.HttpContext.TraceIdentifier}  {filterContext.Exception.Message}");

                var baseresponse = new BaseResponse()
                {
                    HasError = true,
                    Message = $"ErrorID-{Activity.Current?.Id ?? filterContext.HttpContext.TraceIdentifier}",
                    //wait to do...UrlHelper
                    Url = $"{Settings.Default.WebAppDomain}"
                };

                if (filterContext.Exception.GetType() == typeof(JsonResponseException))
                {
                    filterContext.Result = new JsonResult(baseresponse);
                }
                else
                {

                    //ViewDataDictionary viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                    //{
                    //    Model = new ErrorViewModel
                    //    {
                    //        RequestId = Activity.Current?.Id ?? filterContext.HttpContext.TraceIdentifier,
                    //        Exception = filterContext.Exception,
                    //    }
                    //};
                    //filterContext.Result = new ViewResult
                    //{
                    //    ViewName = "~/Views/Shared/Error.cshtml",
                    //    ViewData = viewData,
                    //};

                    ViewDataDictionary viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                    {
                        Model = baseresponse
                    };
                    filterContext.Result = new ViewResult
                    {
                        ViewName = "~/Views/Shared/CustomMessage.cshtml",
                        ViewData = viewData,
                    };
                }
            }
        }
    }
}
