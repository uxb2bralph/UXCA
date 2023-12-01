using CommonLib.Core.Utility;
using ContractHome.Helper;

namespace ContractHome.Models.Email.Template
{
    public class TemplateShopExpired 
    {


        public TemplateShopExpired()
        {

        }
        public string ShopHost { get; set; }
        public string ShopHostAdmin { get; set; }
        public string ShopSaveDays { get; set; }
        public string ShopCloseDate { get; set; }
        public string CdnHost { get; set; }
        public string ViewName => @"~/Views/Shared/EmailTemplate/EmailTemplateShopExpired.cshtml";
        //public async Task<string> GetRenderToStringAsync()
        //{
        //    //return await _razorViewToStringRenderer.RenderViewToStringAsync(
        //    //viewName: this.ViewName
        //    //model: this);

        //    return await _viewRenderService.RenderToStringAsync(
        //        viewName: this.ViewName,
        //        model: this);
        //}
    }
}
