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
using DocumentFormat.OpenXml.Spreadsheet;

namespace ContractHome.Controllers
{
    //[AuthorizedSysAdmin]
    public class UserProfileController : SampleController
    {
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(ILogger<UserProfileController> logger, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = logger;
        }

        public ActionResult InquireData(UserProfileViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            IQueryable<UserProfile> items = models.GetTable<UserProfile>();

            if (viewModel.DataItem != null && viewModel.DataItem.Length > 0)
            {
                items = items.BuildQuery(viewModel.DataItem);
            }

            int? companyID = viewModel.GetCompanyID();
            if (companyID.HasValue)
            {
                var orgUsers = models.GetTable<OrganizationUser>()
                                .Where(c => c.CompanyID == companyID);
                items = items.Where(u => orgUsers.Any(c => c.UID == u.UID));
            }

            var item = items.FirstOrDefault();
            return View("~/Views/UserProfile/Module/EditItem.cshtml", item);
        }

        public async Task<ActionResult> DataItemAsync(QueryViewModel viewModel)
        {
            if (ViewBag.ViewModel == null)
            {
                viewModel = await PrepareViewModelAsync(viewModel);
            }

            viewModel.EncKeyItem = viewModel.EncKeyItem.GetEfficientString();
            if(viewModel.EncKeyItem!=null)
            {
                viewModel.KeyItem = JsonConvert.DeserializeObject<DataTableColumn[]>(viewModel.EncKeyItem.DecryptData());
            }

            IQueryable<UserProfile> items = models.GetTable<UserProfile>();

            if (viewModel.KeyItem?.Any(k => k.Name != null && k.Value != null) == true)
            {
                items = items.BuildQuery(viewModel.KeyItem);
            }
            else
            {
                items = items.Where(" 1 = 0");
            }

            var dataItem = items.FirstOrDefault();
            return View("~/Views/UserProfile/Module/EditItem.cshtml", dataItem);
        }


        public async Task<ActionResult> CommitItemAsync(UserProfileViewModel viewModel)
        {
            viewModel = await PrepareViewModelAsync(viewModel);
            ViewResult result = (ViewResult)await DataItemAsync(viewModel);
            dynamic? dataItem = result.Model;

            int? companyID = viewModel.GetCompanyID();
            if (dataItem == null)
            {
                if (!companyID.HasValue)
                {
                    ModelState.AddModelError("EncCompanyID", "請選擇隸屬公司");
                }

                if(!viewModel.RoleID.HasValue)
                {
                    ModelState.AddModelError("RoleID", "請選擇角色");
                }

                if(String.IsNullOrEmpty(viewModel.Password))
                {
                    ModelState.AddModelError("Password", "請設定密碼");
                }
            }

            viewModel.PID = viewModel.PID.GetEfficientString();
            if (viewModel.PID == null)
            {
                ModelState.AddModelError("PID", "請輸入帳號");
            }

            viewModel.EMail = viewModel.EMail.GetEfficientString();
            if (viewModel.EMail == null)
            {
                ModelState.AddModelError("EMail", "請輸入EMail");
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Shared/ReportInputError.cshtml");
            }

            ITable dataTable = models.GetTable<UserProfile>();
            Type type = typeof(UserProfile);
            dataItem = ContractHome.Models.DataEntity.ExtensionMethods.PrepareDataItem(viewModel.DataItem, dataItem, dataTable, type);

            UserProfile item = (dataItem as UserProfile)!;
            if(companyID.HasValue)
            {
                if (item.OrganizationUser == null)
                {
                    item.OrganizationUser = new OrganizationUser
                    {

                    };
                }

                item.OrganizationUser.CompanyID = companyID.Value;
            }

            if(!String.IsNullOrEmpty(viewModel.Password))
            {
                item.Password = null;
                item.Password2 = viewModel.Password.HashPassword();
            }

            try
            {
                models.SubmitChanges();
                if(viewModel.RoleID.HasValue)
                {
                    models.ExecuteCommand(@"DELETE FROM UserRole WHERE (UID = {0})", item.UID);
                    models.ExecuteCommand(@"INSERT INTO UserRole (UID, RoleID)
                                            VALUES ({0},{1})", item.UID, (int?)viewModel.RoleID);
                }

                return Json(new { result = true });
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex);
                return Json(new { result = false, message = ex.Message });
            }
        }

        public async Task<ActionResult> DeleteItemAsync(QueryViewModel viewModel)
        {
            viewModel = await PrepareViewModelAsync(viewModel);
            ViewResult result = (ViewResult)await DataItemAsync(viewModel);
            dynamic? item = result.Model;

            ITable dataTable = models.GetTable<UserProfile>();
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


    }
}
