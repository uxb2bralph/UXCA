using CommonLib.Core.Utility;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Data;

namespace ContractHome.Helper
{
    public static class MvcExtensions
    {

        public static String ErrorMessage(this ModelStateDictionary modelState)
        {
            return String.Join("、", modelState.Keys.Where(k => modelState[k].Errors.Count > 0)
                    .Select(k => /*k + " : " +*/ String.Join("/", modelState[k].Errors.Select(r => r.ErrorMessage))));
        }

        public static String ErrorMessage(this ModelStateDictionary modelState, String key)
        {
            return String.Join("、", modelState[key].Errors.Select(r => r.ErrorMessage/*.ToZhTwMessage()*/));
        }

        static String ToZhTwMessage(this String message)
        {
            if (message != null && message.StartsWith("The value"))
            {
                return "格式錯誤";
            }
            return message;
        }

        public static String HtmlBreakLine(this String strVal)
        {
            if (strVal != null)
            {
                return strVal.Replace("\r\n", "<br/>").Replace("\n", "<br/>");
            }
            return null;
        }

        public static async Task SaveAsExcelAsync(this DataSet ds, HttpResponse response, String disposition, String fileDownloadToken = null)
        {
            if (fileDownloadToken != null)
            {
                response.Cookies.Append("fileDownloadToken", fileDownloadToken);
            }
            response.Headers.Add("Cache-control", "max-age=1");
            response.ContentType = "application/vnd.ms-excel";
            response.Headers.Add("Content-Disposition", disposition);

            using (var xls = ds.ConvertToExcel())
            {
                String tmpPath = Path.Combine(FileLogger.Logger.LogDailyPath, $"{DateTime.Now.Ticks}.tmp");
                using (FileStream tmp = System.IO.File.Create(tmpPath))
                {
                    xls.SaveAs(tmp);
                    tmp.Flush();
                    tmp.Position = 0;

                    await tmp.CopyToAsync(response.Body);
                }
                await response.Body.FlushAsync();

                System.IO.File.Delete(tmpPath);
            }

        }

    }

}
