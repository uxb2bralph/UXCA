using CommonLib.Utility;
using ContractHome.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Net;

namespace ContractHome.Controllers.Filters
{
    public class ModelStateFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                IDictionary<string, string[]> errorMess = context.ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value.Errors.Select(x => x.ErrorMessage).ToArray());
                BaseResponse baseResponse = new BaseResponse(true, errorMess);

                context.Result = new ContentResult
                {
                    Content = baseResponse.JsonStringify(),
                    ContentType = "application/json",
                    StatusCode = (int?)HttpStatusCode.BadRequest
                };

            }
        }
        public void OnActionExecuted(ActionExecutedContext context) { }

    }
}
