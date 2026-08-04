using Microsoft.AspNetCore.Mvc;

namespace ContosoUniversity.Controllers
{
    public class InstructorsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
