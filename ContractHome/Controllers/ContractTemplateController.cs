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

namespace ContractHome.Controllers
{
    public class ContractTemplateController : SampleController
    {
        private readonly ILogger<HomeController> _logger;

        public ContractTemplateController(ILogger<HomeController> logger, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public async Task<ActionResult> ThumbnailAsync(TemplateResourceViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            if(viewModel.KeyID!= null) 
            {
                viewModel.FilePath = viewModel.KeyID.DecryptData();
            }

            if (string.IsNullOrEmpty(viewModel.FilePath) || viewModel.FilePath.Contains("..") || 
                viewModel.FilePath.Contains('/') || viewModel.FilePath.Contains('\\') || Path.IsPathRooted(viewModel.FilePath))
            {
                return new NotFoundResult();
            }

            if (!System.IO.File.Exists(viewModel.FilePath))
            {
                return new NotFoundResult();
            }

            //var file = new FileInfo(viewModel.FilePath);
            if(viewModel.FilePath.EndsWith(".jpg")
                || viewModel.FilePath.EndsWith(".jpeg")
                || viewModel.FilePath.EndsWith(".png")
                || viewModel.FilePath.EndsWith(".gif")
                || viewModel.FilePath.EndsWith(".bmp"))
            {
                return PhysicalFile(viewModel.FilePath, "application/octet-stream");
            }

            Icon icon = Icon.ExtractAssociatedIcon(viewModel.FilePath) ?? SystemIcons.Application; ;
            //using var image = Image.FromFile(viewModel.FilePath);
            //var thumbnail = image.GetThumbnailImage(100, 100, null, IntPtr.Zero);

            //var ms = new MemoryStream();
            //thumbnail.Save(ms, ImageFormat.Jpeg);
            //ms.Position = 0;
            Response.Clear();
            Response.ContentType = "image/jpeg";
            using (FileBufferingWriteStream output = new FileBufferingWriteStream())
            {
                icon.ToBitmap().Save(output, ImageFormat.Jpeg);
                await output.DrainBufferAsync(Response.Body);
            }

            await Response.Body.FlushAsync();
            return new EmptyResult { };
        }

        public ActionResult DownloadResource(TemplateResourceViewModel viewModel)
        {
            ViewBag.ViewModel = viewModel;
            if (viewModel.KeyID != null)
            {
                viewModel.FilePath = viewModel.KeyID.DecryptData();
            }

            if (string.IsNullOrEmpty(viewModel.FilePath) || viewModel.FilePath.Contains("..") ||
                viewModel.FilePath.Contains('/') || viewModel.FilePath.Contains('\\') || Path.IsPathRooted(viewModel.FilePath))
            {
                return new NotFoundResult();
            }

            if (!System.IO.File.Exists(viewModel.FilePath))
            {
                return new NotFoundResult();
            }

            return PhysicalFile(viewModel.FilePath, "application/octet-stream", Path.GetFileName(viewModel.FilePath));

        }

    }


}