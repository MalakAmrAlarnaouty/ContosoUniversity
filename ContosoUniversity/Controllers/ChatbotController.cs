using Microsoft.AspNetCore.Mvc;

namespace ContosoUniversity.Controllers
{
    public class ChatbotController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
