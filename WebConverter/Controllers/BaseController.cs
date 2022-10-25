using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebConverter.Controllers
{
    public class BaseController : Controller
    {
        protected string UserId
        {
            get
            {
                return GetOrCreateUserId();
            }
        }

        protected string GetOrCreateUserId()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "id");
            if (idClaim == null || string.IsNullOrEmpty(idClaim.Value))
            {
                var claims = new List<Claim>();
                idClaim = new Claim("id", Guid.NewGuid().ToString());
                claims.Add(idClaim);
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                HttpContext.SignInAsync(claimsPrincipal);
            }
            return idClaim.Value;
        }
    }
}
