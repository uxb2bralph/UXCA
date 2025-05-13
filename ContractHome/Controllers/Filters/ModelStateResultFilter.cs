using CommonLib.Utility;
using ContractHome.Helper;
using ContractHome.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

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
            // 判斷是否為 TryController 控制項
            if (context.Controller is TryController)
            {
                return;
            }


            if (!context.ModelState.IsValid)
            {
                string modelStateString = context.ModelState.ErrorMessage();
                BaseResponse baseResponse = new BaseResponse(true, modelStateString);
                var serializeOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                context.Result = new ContentResult
                {
                    Content = System.Text.Json.JsonSerializer.Serialize(baseResponse, serializeOptions),
                    ContentType = "application/json",
                    StatusCode = (int?)HttpStatusCode.OK
                };
            }


        }
    }
}
