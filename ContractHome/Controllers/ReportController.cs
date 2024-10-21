using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Rpt;
using ContractHome.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Dynamic.Core;
using CommonLib.Core.Utility;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Html;

namespace ContractHome.Controllers
{
    public class ReportController : SampleController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TaskProcess _taskProcess;
        private readonly IViewRenderService _viewRenderService;
        public ReportController(ILogger<HomeController> logger, IServiceProvider serviceProvider, TaskProcess taskProcess
            , IViewRenderService viewRenderService
            ) : base(serviceProvider)
        {
            _logger = logger;
            _taskProcess = taskProcess;
            _viewRenderService = viewRenderService;
        }

        //[UserAuthorize]
        public async Task<ActionResult> TaskProcessAsync([FromBody]SignContractViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            if (viewModel.KeyID != null)
            {
                viewModel.ContractID = viewModel.DecryptKeyValue();
            }

            var contract = models.GetTable<Contract>()
                .Where(c => c.ContractID == viewModel.ContractID)
                .FirstOrDefault();

            _taskProcess.FileNo = contract.ContractNo;
            _taskProcess.FileName = contract.Title;
            //_taskProcess.Email = Contract新增起約人
            _taskProcess.Processes = contract.CDS_Document.DocumentProcessLog.Select(x => new TaskProcess.Process() { Name = x.ActorID.ToString()! });
            _taskProcess.Operators = contract.ContractingParty.Select(x => new TaskProcess.Operator() { Name = x.ContractID.ToString() });

            var rptViewRenderString = await _viewRenderService.RenderToStringAsync(
                viewName: _taskProcess.TemplateItem,
                model: _taskProcess);

            var renderer = new ChromePdfRenderer();
            //PdfDocument pdf = renderer.RenderHtmlAsPdf(rptViewRenderString).SaveAs("test.pdf");
            PdfDocument pdf = await renderer.RenderHtmlAsPdfAsync(rptViewRenderString);
            return File(pdf.BinaryData, "application/pdf", "viewToPdfMVCCore.pdf");
        }

    }


}