using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.RegularExpressions;
using CommonLib.Utility;

namespace ContractHome.Models.ViewModel
{
    public static class MvcValidationExtensions
    {
        public static void OrganizationValueCheck(this OrganizationViewModel viewModel, ModelStateDictionary modelState)
        {
            viewModel.CompanyName = viewModel.CompanyName.GetEfficientString();
            if (viewModel.CompanyName == null)
            {
                //檢查名稱
                modelState.AddModelError("CompanyName", "請輸入公司名稱!!");

            }
            viewModel.ReceiptNo = viewModel.ReceiptNo.GetEfficientString();
            if (String.IsNullOrEmpty(viewModel.ReceiptNo))
            {
                //檢查名稱
                modelState.AddModelError("ReceiptNo", "請輸入公司統編!!");

            }
            viewModel.Addr = viewModel.Addr.GetEfficientString();
            if (String.IsNullOrEmpty(viewModel.Addr))
            {
                //檢查名稱
                modelState.AddModelError("Addr", "請輸入公司地址!!");
            }
            viewModel.Phone = viewModel.Phone.GetEfficientString();
            if (String.IsNullOrEmpty(viewModel.Phone))
            {
                //檢查名稱
                modelState.AddModelError("Phone", "請輸入公司電話!!");
            }

            viewModel.ContactEmail = viewModel.ContactEmail.GetEfficientString();
            if (viewModel.ContactEmail == null)
            {
                modelState.AddModelError("ContactEmail", "電子信箱尚未輸入或輸入錯誤!!");
            }
            else
            {
                Regex reg = new Regex("\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*");
                if (!reg.IsMatch(viewModel.ContactEmail))
                {
                    //檢查email
                    modelState.AddModelError("ContactEmail", "電子信箱尚未輸入或輸入錯誤!!");
                }
            }
        }

    }
}
