using Microsoft.AspNetCore.Mvc;

namespace CelexWebApp.Controllers.Profesor
{
    public class LibrosController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
