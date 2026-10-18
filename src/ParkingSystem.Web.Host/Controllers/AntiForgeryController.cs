using Abp.Web.Security.AntiForgery;
using ParkingSystem.Controllers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace ParkingSystem.Web.Host.Controllers
{
    public class AntiForgeryController : ParkingSystemControllerBase
    {
        private readonly IAntiforgery _antiforgery;
        private readonly IAbpAntiForgeryManager _antiForgeryManager;

        public AntiForgeryController(IAntiforgery antiforgery, IAbpAntiForgeryManager antiForgeryManager)
        {
            _antiforgery = antiforgery;
            _antiForgeryManager = antiForgeryManager;
        }

        [HttpGet]
        public IActionResult GetToken()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized();
            }

            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

            Response.Cookies.Append(
                "XSRF-TOKEN",
                tokens.RequestToken,
                new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/"
                }
            );

            Response.Headers["X-XSRF-TOKEN"] = tokens.RequestToken;

            return Ok(new { xsrfToken = tokens.RequestToken });
        }

        [HttpGet]
        public void SetCookie()
        {
            _antiForgeryManager.SetCookie(HttpContext);
        }
    }
}
