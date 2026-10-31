using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace POS.BackOffice.Controllers;

[Route("[controller]/[action]")]
public class CulturesController : Controller
{
    public IActionResult SetCulture(string culture, string redirectUri)
    {
        if (!string.IsNullOrEmpty(culture))
            HttpContext.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)));

        return LocalRedirect(redirectUri);
    }
}
