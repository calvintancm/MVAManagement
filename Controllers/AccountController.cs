using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MVAManagement.Models;
using MVAManagement.ViewModels.Account;
using System.Security.Claims;
using System.Text;

namespace MVAManagement.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        // ← No more UserManager / SignInManager needed
        public AccountController(
            IHttpClientFactory httpClientFactory,
            ILogger<AccountController> logger,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,   // ← add
            RoleManager<IdentityRole> roleManager)       // ← add
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

      

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity?.Name;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            _logger.LogInformation("User {User} logged out.", userName);
            return RedirectToAction("Login", "Account");
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public IActionResult UserProfile()
        {
            var userName = User.Identity?.Name ?? "demo";
         
            string firstName = userName;
            string lastName = "";

            if (userName.Contains('.'))
            {
                var parts = userName.Split('.');
                firstName = parts[0];
                lastName = parts.Length > 1 ? parts[1] : "";
            }
            else
            {
                firstName = userName;
                lastName = "";
            }

            var email = $"{userName}@ptclogistics.com.sg";

            var model = new UserProfileView
            {
                UserName = userName,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = "12345678"   
            };
            return View(model);
        }

        [Authorize]
        public IActionResult Preferences()
        {
            var model = new UserPreferences
            {
                Theme = Request.Cookies["UserTheme"] ?? "light",
                Language = Request.Cookies["UserLanguage"] ?? "en",
                EmailNotifications = Request.Cookies["EmailNotifications"] == "true",
                DefaultPageSize = int.TryParse(Request.Cookies["DefaultPageSize"], out int size) ? size : 50
            };
            return View(model);
        }

     
        [Authorize]
        [HttpPost]
        public IActionResult SavePreferences([FromBody] UserPreferences model)
        {
            try
            {
                var options = new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), HttpOnly = false };
                Response.Cookies.Append("UserTheme", model.Theme ?? "light", options);
                Response.Cookies.Append("UserLanguage", model.Language ?? "en", options);
                Response.Cookies.Append("EmailNotifications", model.EmailNotifications.ToString(), options);
                Response.Cookies.Append("DefaultPageSize", model.DefaultPageSize.ToString(), options);

                _logger.LogInformation("Preferences saved for user {User}", User.Identity?.Name);
                return Ok(new { success = true, message = "Preferences saved." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SavePreferences failed");
                return BadRequest(new { success = false, message = "Failed to save preferences." });
            }
        }
    }
}