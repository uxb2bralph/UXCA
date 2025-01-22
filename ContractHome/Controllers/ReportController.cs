using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Report;
using ContractHome.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;
using CommonLib.Core.Utility;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Html;
using ContractHome.Models.Dto;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Drawing;

namespace ContractHome.Controllers
{
    public class ReportController : SampleController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TaskProcess _taskProcess;
        private readonly IViewRenderService _viewRenderService;
        private readonly string dateTimeFormat = "yyyy/MM/dd HH:mm:ss";
        private readonly BaseResponse _baseResponse;
        public ReportController(ILogger<HomeController> logger, IServiceProvider serviceProvider
            , IViewRenderService viewRenderService, BaseResponse baseResponse
            ) : base(serviceProvider)
        {
            _logger = logger;
            _taskProcess = new TaskProcess();
            _viewRenderService = viewRenderService;
            _baseResponse = baseResponse;
        }

        //[UserAuthorize]
        public async Task<IActionResult> TaskProcessAsync([FromBody]SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            if (viewModel.KeyID != null)
            {
                viewModel.ContractID = viewModel.DecryptKeyValue();
            }

            var contract = models.GetTable<Contract>()
                .Where(c => c.ContractID == viewModel.ContractID)
                .FirstOrDefault();

            if (contract==null) { return BadRequest(_baseResponse.ErrorMessage($"合約{viewModel.KeyID}不存在")); }

            var logList = contract.CDS_Document.DocumentProcessLog;

            _taskProcess.FileNo = contract.ContractNo;
            _taskProcess.FileName = contract.Title;
            _taskProcess.TaskProcessDateTime = DateTime.Now.ToString(dateTimeFormat);
            _taskProcess.CreateUser = contract.UserProfile;
            _taskProcess.PublishedDateTime = logList.Min(x => x.LogDate).ToString(dateTimeFormat);
            _taskProcess.FinishedDateTime = logList.Max(x => x.LogDate).ToString(dateTimeFormat);
            _taskProcess.Processes = logList.Select(x => 
                new TaskProcess.Process(
                    taskDateTime:x.LogDate.ToString(dateTimeFormat), 
                    taskDesc: CDS_Document.StepNaming[x.StepID], 
                    email: x.UserProfile.EMail,
                    clientIP: x.ClientIP,
                    clientDevice: x.ClientDevice));
            _taskProcess.Operators = contract.ContractingUser.Select(x => 
                    new TaskProcess.Operator(email: x.UserProfile.EMail, region: x.UserProfile.Region));

            var rptViewRenderString = await _viewRenderService.RenderToStringAsync(
                viewName: _taskProcess.TemplateItem,
                model: _taskProcess);

            var renderer = new ChromePdfRenderer();
            PdfDocument pdf = await renderer.RenderHtmlAsPdfAsync(rptViewRenderString);
            return File(pdf.BinaryData, "application/pdf", "viewToPdfMVCCore.pdf");
            //PdfDocument pdf = renderer.RenderHtmlAsPdf(rptViewRenderString).SaveAs("test.pdf");
        }

    }


}