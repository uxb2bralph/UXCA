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
using ContractHome.Models.Helper;

namespace ContractHome.Controllers
{
    public class EmptyController : SampleController
    {
        private readonly ILogger<HomeController> _logger;

        public EmptyController(ILogger<HomeController> logger, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = logger;
        }

    }


}