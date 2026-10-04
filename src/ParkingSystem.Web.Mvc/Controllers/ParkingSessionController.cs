using Microsoft.AspNetCore.Mvc;

namespace ParkingSystem.Web.Controllers
{
    public class ParkingSessionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
