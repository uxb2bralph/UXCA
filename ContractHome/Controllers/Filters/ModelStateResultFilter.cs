using CommonLib.Utility;
using ContractHome.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Net;

namespace ContractHome.Controllers.Filters
{
    public class ModelStateResultFilter : IResultFilter
    {
        public void OnResultExecuted(ResultExecutedContext context)
        {
            //throw new NotImplementedException();
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {

            if (!context.ModelState.IsValid)
            {
                SerializableError errors = new SerializableError(context.ModelState);
                BaseResponse baseResponse = new BaseResponse(true, errors);

                context.Result = new ContentResult
                {
                    Content = baseResponse.JsonStringify(),
                    ContentType = "application/json",
                    StatusCode = (int?)HttpStatusCode.BadRequest
                };
            }


        }
    }
}
