using Microsoft.AspNetCore.Mvc;

namespace CelexWebApp.Controllers.Profesor
{
    public class ProfesorController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
