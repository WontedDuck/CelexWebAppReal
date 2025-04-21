using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CelexWebApp.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult SignIn()
        {
            var redirectUrl = Url.Action(nameof(HomeController.Index), "Home");
            return Challenge(new AuthenticationProperties { RedirectUri = redirectUrl }, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> SignOut()
        {
            //Borrar los datos de la cookie local
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // Redirige al logout global de Azure y luego vuelve a SignIn 
            var postLogoutRedirect = Url.Action("SignIn", "Account", null, Request.Scheme);
            var signOutUrl = $"https://login.microsoftonline.com/common/oauth2/logout?post_logout_redirect_uri={postLogoutRedirect}";

            return Redirect(signOutUrl);
        }
    }
}