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
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace ContractHome.Controllers
{
    public class DataExchangeController : SampleController
    {
        private readonly ILogger<DataExchangeController> _logger;

        public DataExchangeController(ILogger<DataExchangeController> logger, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = logger;
        }

        public ActionResult GetResource(String path)
        {
            if (path != null)
            {
                String filePath = path.DecryptData();
                if (System.IO.File.Exists(filePath))
                {
                    return File(filePath, "application/octet-stream", Path.GetFileName(filePath));
                }
            }
            return new EmptyResult { };
        }


        public ActionResult MaintainData(DataTableQueryViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            return View("~/Views/DataExchange/MaintainData.cshtml");
        }

        public ActionResult SqlData(DataTableQueryViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            return View("~/Views/DataExchange/SqlData.cshtml");
        }

        async Task<Type?> PrepareDataTableAsync(DataTableQueryViewModel viewModel)
        {
            if (Request.ContentType?.Contains("application/json", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                var data = await Request.GetRequestBodyAsync();
                viewModel = JsonConvert.DeserializeObject<DataTableQueryViewModel>(data);
            }

            ViewBag.ViewModel = viewModel;
            viewModel.TableName = viewModel.TableName.GetEfficientString();
            if (viewModel.TableName == null)
            {
                ModelState.AddModelError("Message", "請選擇資料表!!");
                return null;
            }

            String typeName = typeof(DCDataContext).AssemblyQualifiedName.Replace("DCDataContext", viewModel.TableName);
            var type = Type.GetType(typeName);
            if (type == null)
            {
                ModelState.AddModelError("Message", "資料表錯誤!!");
                return null;
            }

            return type;
        }


        public async Task<ActionResult> ShowDataTableAsync(DataTableQueryViewModel viewModel)
        {
            var type = await PrepareDataTableAsync(viewModel);
            if (type == null)
            {
                return Json(new { result = false, message = ModelState.ErrorMessage() });
            }

            viewModel = (DataTableQueryViewModel)ViewBag.ViewModel;
            ViewBag.TableType = type;

            IQueryable items = models.DataContext.GetTable(type);
            if (viewModel.DataItem != null && viewModel.DataItem.Length > 0)
            {
                items = BuildQuery(viewModel.DataItem, type, items);
            }
            viewModel.RecordCount = items?.Count();

            if (viewModel.PageIndex.HasValue)
            {
                viewModel.PageIndex--;
                return View("~/Views/DataExchange/Module/DataItemList.cshtml", items);
            }
            else
            {
                viewModel.PageIndex = 0;
                return View("~/Views/DataExchange/Module/DataTableQueryResult.cshtml", items);
            }

        }

        public async Task<ActionResult> InquireSqlAsync(DataTableQueryViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;

            viewModel.CommandText = viewModel.CommandText.GetEfficientString();
            if (viewModel.CommandText == null)
            {
                return Json(new { result = false, message = "請輸入查詢指令!!" });
            }

            String conn = null;
            viewModel.ConnectionString = viewModel.ConnectionString.GetEfficientString();   
            if (viewModel.ConnectionString != null)
            {
                conn = viewModel.ConnectionString.DecryptData();
            }

            DataContext db;
            bool useDB = false;
            if (conn == null)
            {
                db = models.DataContext;
            }
            else 
            { 
                db = new DataContext(conn);
                useDB = true;
            }

            viewModel.CommandText = Encoding.UTF8.GetString(Convert.FromBase64String(viewModel.CommandText));

            viewModel.RecordCount = db.ExecuteQuery<dynamic>(viewModel.CommandText).Count();

            int pageIndex = viewModel.PageIndex ?? 0;
            int pageSize = viewModel.PageSize ?? 10;

            DataSet ds = new DataSet();
            using (SqlCommand sqlCmd = new SqlCommand(viewModel.CommandText))
            {
                sqlCmd.Connection = (SqlConnection)db.Connection;

                using (SqlDataAdapter adapter = new SqlDataAdapter(sqlCmd))
                {
                    adapter.Fill(ds, pageIndex * pageSize, pageSize, "Table");
                }
            }

            if(useDB)
            {
                db.Dispose();
            }

            if (viewModel.PageIndex.HasValue)
            {
                viewModel.PageIndex--;
                return View("~/Views/DataExchange/Module/SqlDataItemList.cshtml", ds);
            }
            else
            {
                viewModel.PageIndex = 0;
                return View("~/Views/DataExchange/Module/SqlDataQueryResult.cshtml", ds);
            }

        }

        private static IQueryable BuildQuery(DataTableColumn[] fields, Type type, IQueryable items)
        {
            foreach (DataTableColumn field in fields)
            {
                PropertyInfo propertyInfo = type.GetProperty(field.Name);
                var columnAttribute = propertyInfo?.GetColumnAttribute();
                if (columnAttribute != null)
                {
                    String fieldValue = field.Value.GetEfficientString();
                    if (fieldValue == null)
                    {
                        continue;
                    }
                    var t = propertyInfo.PropertyType;
                    if (t == typeof(String))
                    {
                        items = items.Where($"{propertyInfo.Name}.StartsWith(@0)", fieldValue);
                    }
                    else
                    {
                        items = items.Where($"{propertyInfo.Name} == @0", Convert.ChangeType(fieldValue, propertyInfo.PropertyType));
                    }
                }
            }

            return items;
        }

        public async Task<ActionResult> DataItemAsync(DataTableQueryViewModel viewModel)
        {
            var type = await PrepareDataTableAsync(viewModel);
            if (type == null)
            {
                return View("~/Views/Shared/AlertMessage.cshtml", model: ModelState.ErrorMessage());
            }

            viewModel = (DataTableQueryViewModel)ViewBag.ViewModel;

            ViewBag.TableType = type;
            IQueryable items = ViewBag.DataTable = models.DataContext.GetTable(type);
            //var items = dataTable.Cast<dynamic>(); 

            dynamic dataItem;
            if (viewModel.KeyItem?.Any(k => k.Name != null && k.Value != null) == true)
            {
                items = BuildQuery(viewModel.KeyItem, type, items);
            }
            else
            {
                items = items.Where(" 1 = 0");
            }
            //var key = viewModel.KeyItem?.Where(k => k.Name != null && k.Value != null);
            //if (key?.Any() == true)
            //{
            //    int idx = 0;
            //    String sqlCmd = String.Concat(items.ToString(),
            //                        " where ",
            //                        String.Join(" and ", key.Select(k => $"{k.Name} = {{{idx++}}}")));
            //    var paramValues = key.Select(k => k.Value).ToArray();
            //    dataItem = ((IEnumerable<dynamic>)models.DataContext.ExecuteQuery(type, sqlCmd, paramValues))
            //                    .FirstOrDefault();
            //}
            //else
            //{
            //    dataItem = items.FirstOrDefault();
            //}

            dataItem = items.FirstOrDefault();

            return View("~/Views/DataExchange/Module/DataItem.cshtml", dataItem);
        }

        public async Task<ActionResult> EditItemAsync(DataTableQueryViewModel viewModel)
        {
            ViewResult result = (ViewResult)(await DataItemAsync(viewModel));
            result.ViewName = "~/Views/DataExchange/Module/EditItem.cshtml";
            return result;
        }

        public async Task<ActionResult> CommitItemAsync(DataTableQueryViewModel viewModel)
        {
            ViewResult result = (ViewResult)await DataItemAsync(viewModel);
            Type type = ViewBag.TableType as Type;
            if (type == null)
            {
                return result;
            }

            viewModel = (DataTableQueryViewModel)ViewBag.ViewModel;

            ITable dataTable = ViewBag.DataTable as ITable;
            dynamic dataItem = result.Model;

            if (dataItem == null)
            {
                dataItem = Activator.CreateInstance(type);
                dataTable.InsertOnSubmit(dataItem);
            }

            if (viewModel.DataItem != null)
            {
                foreach (DataTableColumn field in viewModel.DataItem)
                {
                    PropertyInfo propertyInfo = type.GetProperty(field.Name);
                    if (propertyInfo != null && propertyInfo.CanWrite)
                    {
                        object value = field.Value != null
                                        ? Convert.ChangeType(field.Value, propertyInfo.PropertyType)
                                        : null;
                        propertyInfo.SetValue(dataItem, value, null);
                    }
                }
            }

            models.SubmitChanges();
            return View("~/Views/DataExchange/Module/DataItem.cshtml", dataItem);
        }

        public async Task<ActionResult> DeleteItemAsync(DataTableQueryViewModel viewModel)
        {
            var type = await PrepareDataTableAsync(viewModel);
            if (type == null)
            {
                return View("~/Views/Shared/AlertMessage.cshtml", model: ModelState.ErrorMessage());
            }

            viewModel = (DataTableQueryViewModel)ViewBag.ViewModel;

            ViewBag.TableType = type;
            ITable dataTable = ViewBag.DataTable = models.DataContext.GetTable(type);

            if (viewModel.KeyItems != null)
            {
                foreach (var keyItem in viewModel.KeyItems)
                {
                    DataTableColumn[]? keyData = JsonConvert.DeserializeObject<DataTableColumn[]>(keyItem.DecryptData());
                    dynamic item = BuildQuery(keyData, type, (IQueryable)dataTable).FirstOrDefault();
                    if (item != null)
                    {
                        dataTable.DeleteOnSubmit(item);
                        models.SubmitChanges();
                    }
                }
            }

            return Json(new { result = true });
        }

    }
}
