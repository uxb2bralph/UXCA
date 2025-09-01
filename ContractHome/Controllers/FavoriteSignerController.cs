using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractHome.Controllers
{
    [Authorize]
    public class FavoriteSignerController(IServiceProvider serviceProvider) : SampleController(serviceProvider)
    {
        public IActionResult FavoriteSignerList()
        {
            return View();
        }
    }
}
