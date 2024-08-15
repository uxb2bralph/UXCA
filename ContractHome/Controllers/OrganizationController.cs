using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using ContractHome.Models;
using ContractHome.Models.DataEntity;
using ContractHome.Models.ViewModel;
using CommonLib.Utility;
using Newtonsoft.Json;
using ContractHome.Helper;
using ContractHome.Properties;
using CommonLib.Core.Utility;
using System.Xml;
using GemBox.Document;
using System.Net;
using Microsoft.Extensions.Primitives;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.AspNetCore.WebUtilities;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Data.Linq;
using ContractHome.Security.Authorization;
using Irony.Parsing;
using DocumentFormat.OpenXml.Presentation;
using System.Web;
using ContractHome.Models.Dto;

namespace ContractHome.Controllers
{
    [AuthorizedSysAdmin]
    public class OrganizationController : SampleController
    {
        private readonly ILogger<OrganizationController> _logger;
        private readonly BaseResponse _baseResponse;

        public OrganizationController(
            ILogger<OrganizationController> logger,
            IServiceProvider serviceProvider,
            BaseResponse baseResponse) : base(serviceProvider)
        {
            _logger = logger;
            _baseResponse = baseResponse;
        }

        [RoleAuthorize(roleID: new int[] { (int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin })]
        public ActionResult MaintainData(QueryViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            return View("~/Views/Organization/MaintainData.cshtml");
        }

        public ActionResult VueInquireJson([FromBody] OrganizationViewModel viewModel)
        {
            ViewResult result = (ViewResult)VueInquireData(viewModel);
            result.ViewName = "~/Views/Organization/VueModule/OrganizationItems.cshtml";
            return result;
        }

        public ActionResult VueInquireData([FromBody] OrganizationViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            IQueryable<Organization> items = models.GetTable<Organization>();

            viewModel.ReceiptNo = viewModel.ReceiptNo.GetEfficientString();
            if (viewModel.ReceiptNo != null)
            {
                items = items.Where(o => o.ReceiptNo.StartsWith(viewModel.ReceiptNo));
            }

            viewModel.CompanyName = viewModel.CompanyName.GetEfficientString();
            if (viewModel.CompanyName != null)
            {
                items = items.Where(o => o.CompanyName.StartsWith(viewModel.CompanyName));
            }

            viewModel.Phone = viewModel.Phone.GetEfficientString();
            if (viewModel.Phone != null)
            {
                items = items.Where(o => o.Phone.StartsWith(viewModel.Phone));
            }

            viewModel.Addr = viewModel.Addr.GetEfficientString();
            if (viewModel.Addr != null)
            {
                items = items.Where(o => o.Addr.StartsWith(viewModel.Addr));
            }

            viewModel.ContactEmail = viewModel.ContactEmail.GetEfficientString();
            if (viewModel.ContactEmail != null)
            {
                items = items.Where(o => o.ContactEmail.StartsWith(viewModel.ContactEmail));
            }

            viewModel.UndertakerName = viewModel.UndertakerName.GetEfficientString();
            if (viewModel.UndertakerName != null)
            {
                items = items.Where(o => o.UndertakerName.StartsWith(viewModel.UndertakerName));
            }

            viewModel.Fax = viewModel.Fax.GetEfficientString();
            if (viewModel.Fax != null)
            {
                items = items.Where(o => o.Fax.StartsWith(viewModel.Fax));
            }

            //if (viewModel.DataItem != null && viewModel.DataItem.Length > 0)
            //{
            //    items = items.BuildQuery(viewModel.DataItem);
            //}

            if (viewModel.PageIndex.HasValue)
            {
                viewModel.PageIndex--;
            }
            else
            {
                viewModel.PageIndex = 0;
            }

            return View("~/Views/Organization/VueModule/OrganizationList.cshtml", items);
        }

        public ActionResult InquireData(OrganizationViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            IQueryable<Organization> items = models.GetTable<Organization>();
            //viewModel.ReceiptNo = viewModel.ReceiptNo.GetEfficientString();
            //if(viewModel.ReceiptNo != null) 
            //{ 
            //    items = items.Where(o=>o.ReceiptNo.StartsWith(viewModel.ReceiptNo));
            //}

            //viewModel.CompanyName = viewModel.CompanyName.GetEfficientString();
            //if(viewModel.CompanyName != null)
            //{
            //    items = items.Where(o=>o.CompanyName.StartsWith(viewModel.CompanyName));    
            //}

            //viewModel.Phone = viewModel.Phone.GetEfficientString();
            //if(viewModel.Phone != null)
            //{
            //    items = items.Where(o=>o.Phone.StartsWith(viewModel.Phone));
            //}

            //viewModel.Addr = viewModel.Addr.GetEfficientString();
            //if(viewModel.Addr != null) 
            //{
            //    items = items.Where(o => o.Addr.StartsWith(viewModel.Addr));
            //}

            //viewModel.ContactEmail = viewModel.ContactEmail.GetEfficientString();
            //if(viewModel.ContactEmail != null)
            //{
            //    items = items.Where(o=>o.ContactEmail.StartsWith(viewModel.ContactEmail));
            //}

            //viewModel.UndertakerName = viewModel.UndertakerName.GetEfficientString();
            //if (viewModel.UndertakerName != null)
            //{
            //    items = items.Where(o => o.UndertakerName.StartsWith(viewModel.UndertakerName));
            //}

            //viewModel.Fax = viewModel.Fax.GetEfficientString();
            //if(viewModel.Fax != null)
            //{
            //    items = items.Where(o => o.Fax.StartsWith(viewModel.Fax));
            //}

            if (viewModel.DataItem != null && viewModel.DataItem.Length > 0)
            {
                items = items.BuildQuery(viewModel.DataItem);
            }

            var item = items.FirstOrDefault();
            return View("~/Views/Organization/Module/EditItem.cshtml", item);
        }

        public async Task<ActionResult> DataItemAsync(QueryViewModel viewModel)
        {
            if (ViewBag.ViewModel == null)
            {
                viewModel = await PrepareViewModelAsync(viewModel);
            }

            viewModel.EncKeyItem = viewModel.EncKeyItem.GetEfficientString();
            if (viewModel.EncKeyItem != null)
            {
                viewModel.KeyItem = JsonConvert.DeserializeObject<DataTableColumn[]>(viewModel.EncKeyItem.DecryptData());
            }

            IQueryable<Organization> items = models.GetTable<Organization>();

            if (viewModel.KeyItem?.Any(k => k.Name != null && k.Value != null) == true)
            {
                items = items.BuildQuery(viewModel.KeyItem);
            }
            else
            {
                items = items.Where(" 1 = 0");
            }

            var dataItem = items.FirstOrDefault();
            return View("~/Views/Organization/Module/EditItem.cshtml", dataItem);
        }

        public ActionResult VueCommitItem([FromBody] OrganizationViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            viewModel.OrganizationValueCheck(ModelState);


            if (viewModel.KeyID != null)
            {
                viewModel.CompanyID = viewModel.DecryptKeyValue();
            }

            var item = models.GetTable<Organization>().Where(o => o.CompanyID == viewModel.CompanyID)
                .FirstOrDefault();

            if ((viewModel.ReceiptNo != null) 
                && (item == null || item.ReceiptNo != viewModel.ReceiptNo)
                    && (models.GetTable<Organization>().Any(o => o.ReceiptNo == viewModel.ReceiptNo)))
            {
                ModelState.AddModelError("ReceiptNo", "相同的企業統編已存在!!");
            }

            if (viewModel.BelongToCompany != null)
            {
                try
                {
                    int belongToCompany = viewModel.BelongToCompany.DecryptKeyValue();
                }
                catch (Exception)
                {
                    ModelState.AddModelError("BelongToCompany", "所屬起約公司不存在!!");
                }

            }

            if (!ModelState.IsValid)
            {
                return Json(new { result = false, message = ModelState.ErrorMessage() });
            }

            if (item == null)
            {
                item = Organization.PrepareNewItem(models.DataContext);
            }

            item.ReceiptNo = viewModel.ReceiptNo;
            item.CompanyName = viewModel.CompanyName;
            item.Addr = viewModel.Addr;
            item.Phone = viewModel.Phone;
            item.Fax = viewModel.Fax;
            item.UndertakerName = viewModel.UndertakerName;
            item.ContactName = viewModel.ContactName;
            item.ContactTitle = viewModel.ContactTitle;
            item.ContactPhone = viewModel.ContactPhone;
            item.ContactMobilePhone = viewModel.ContactMobilePhone;
            item.ContactEmail = viewModel.ContactEmail;
            item.CanCreateContract = (viewModel.CreateContract != null) ? viewModel.CreateContract : false;
            item.CompanyBelongTo = (viewModel.BelongToCompany != null) ? viewModel.BelongToCompany.DecryptKeyValue() : null;
            try
            {
                models.SubmitChanges();
                return Json(_baseResponse);
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex);
                return Json(_baseResponse.ErrorMessage());
            }
        }


        public async Task<ActionResult> CommitItemAsync(OrganizationViewModel viewModel)
        {
            viewModel = await PrepareViewModelAsync(viewModel);
            ViewResult result = (ViewResult)await DataItemAsync(viewModel);
            dynamic? dataItem = result.Model;

            ITable dataTable = models.GetTable<Organization>();
            Type type = typeof(Organization);
            dataItem = ContractHome.Models.DataEntity.ExtensionMethods.PrepareDataItem(viewModel.DataItem, dataItem, dataTable, type);

            try
            {
                models.SubmitChanges();
                return Json(new { result = true });
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex);
                return Json(new { result = false, message = ex.Message });
            }
        }

        public ActionResult VueDeleteItemAsync([FromBody] OrganizationViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            if (viewModel.KeyID != null)
            {
                viewModel.CompanyID = viewModel.DecryptKeyValue();
            }

            var item = models.GetTable<Organization>().Where(o => o.CompanyID == viewModel.CompanyID)
                .FirstOrDefault();

            ITable dataTable = models.GetTable<Organization>();
            if (item != null)
            {
                dataTable.DeleteOnSubmit(item);
                try
                {
                    models.SubmitChanges();
                    return Json(new { result = true });
                }
                catch (Exception ex)
                {
                    FileLogger.Logger.Error(ex);
                    return Json(new { result = false, message = ex.Message });
                }
            }

            return Json(new { result = false, message = "資料錯誤！" });
        }

        public async Task<ActionResult> DeleteItemAsync(QueryViewModel viewModel)
        {
            viewModel = await PrepareViewModelAsync(viewModel);
            ViewResult result = (ViewResult)await DataItemAsync(viewModel);
            dynamic? item = result.Model;

            ITable dataTable = models.GetTable<Organization>();
            if (item != null)
            {
                dataTable.DeleteOnSubmit(item);
                try
                {
                    models.SubmitChanges();
                    return Json(new { result = true });
                }
                catch (Exception ex)
                {
                    FileLogger.Logger.Error(ex);
                    return Json(new { result = false, message = ex.Message });
                }
            }

            return Json(new { result = false, message = "資料錯誤！" });
        }

        public ActionResult signHistoryPaper()
        {
            return View();
        }


    }
}
