using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MVAManagement.Models;
using MVAManagement.ViewModels.Account;
using System.Security.Claims;

namespace MVAManagement.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            ILogger<AccountController> logger,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ─────────────────────────────────────────────────────────────────
        // GET /Account/Login
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        // ─────────────────────────────────────────────────────────────────
        // POST /Account/Login
        //
        // Flow:
        //   1. Validate model (UserName + Password required)
        //   2. Look up user in AspNetUsers via UserManager
        //   3. Verify password hash with PasswordHasher
        //   4. Collect roles from AspNetUserRoles
        //   5. Build ClaimsIdentity and sign-in with cookie auth
        //   6. Redirect to returnUrl or Dashboard
        // ─────────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            // 1. Find user by username (case-insensitive — UserManager normalises)
            var user = await _userManager.FindByNameAsync(model.UserName.Trim());

            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            if (user == null)
            {
                _logger.LogWarning("Login failed — unknown username: {User}", model.UserName);
                TempData["ErrorMessage"] = "Invalid username or password.";
                return View(model);
            }

            // 2. Check lockout before verifying password
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Locked-out login attempt: {User}", model.UserName);
                TempData["ErrorMessage"] = "Account is temporarily locked. Please try again later.";
                return View(model);
            }

            // 3. Verify password against AspNetUsers.PasswordHash
            var passwordOk = await _userManager.CheckPasswordAsync(user, model.Password);

            if (!passwordOk)
            {
                // Increment access-failed count (supports lockout if configured)
                await _userManager.AccessFailedAsync(user);

                _logger.LogWarning("Login failed — wrong password for: {User}", model.UserName);
                TempData["ErrorMessage"] = "Invalid username or password.";
                return View(model);
            }

            // 4. Password correct — reset failed count
            await _userManager.ResetAccessFailedCountAsync(user);

            // 5. Collect roles
            var roles = await _userManager.GetRolesAsync(user);

            // 6. Build claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name,           user.UserName ?? model.UserName),
                new Claim(ClaimTypes.Email,          user.Email    ?? string.Empty),
            };

            // Add a claim per role (works with [Authorize(Roles="...")] and policies)
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // 7. Sign in — honour "Remember Me" for 8-hour sliding window
            var authProps = new AuthenticationProperties
            {
                IsPersistent    = model.RememberMe,
                ExpiresUtc      = model.RememberMe
                                    ? DateTimeOffset.UtcNow.AddHours(8)
                                    : (DateTimeOffset?)null,
                AllowRefresh    = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProps);

            // Store display name in session for convenience
            HttpContext.Session.SetString("UserName",  user.UserName ?? model.UserName);
            HttpContext.Session.SetString("UserEmail", user.Email    ?? string.Empty);
            HttpContext.Session.SetString("UserRoles", string.Join(",", roles));

            _logger.LogInformation("User {User} logged in. Roles: [{Roles}]",
                user.UserName, string.Join(", ", roles));

            return RedirectToLocal(returnUrl);
        }

        // ─────────────────────────────────────────────────────────────────
        // POST /Account/Logout
        // ─────────────────────────────────────────────────────────────────
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

        // ─────────────────────────────────────────────────────────────────
        // GET /Account/AccessDenied
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ─────────────────────────────────────────────────────────────────
        // GET /Account/ChangePassword
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // ─────────────────────────────────────────────────────────────────
        // GET /Account/UserProfile
        // ─────────────────────────────────────────────────────────────────
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UserProfile()
        {
            var userName = User.Identity?.Name ?? "user";
            var user     = await _userManager.FindByNameAsync(userName);

            string firstName = userName;
            string lastName  = string.Empty;

            if (userName.Contains('.'))
            {
                var parts = userName.Split('.');
                firstName = parts[0];
                lastName  = parts.Length > 1 ? parts[1] : string.Empty;
            }

            var roles = user != null
                ? await _userManager.GetRolesAsync(user)
                : new List<string>();

            var model = new UserProfileView
            {
                UserName    = userName,
                Email       = user?.Email ?? $"{userName}@mvalegal.com.my",
                FirstName   = firstName,
                LastName    = lastName,
                PhoneNumber = user?.PhoneNumber ?? string.Empty
            };

            return View(model);
        }

        // ─────────────────────────────────────────────────────────────────
        // GET /Account/Preferences
        // ─────────────────────────────────────────────────────────────────
        [Authorize]
        public IActionResult Preferences()
        {
            var model = new UserPreferences
            {
                Theme                = Request.Cookies["UserTheme"]        ?? "light",
                Language             = Request.Cookies["UserLanguage"]     ?? "en",
                EmailNotifications   = Request.Cookies["EmailNotifications"] == "true",
                DefaultPageSize      = int.TryParse(Request.Cookies["DefaultPageSize"], out int size) ? size : 50
            };
            return View(model);
        }

        // ─────────────────────────────────────────────────────────────────
        // POST /Account/SavePreferences
        // ─────────────────────────────────────────────────────────────────
        [Authorize]
        [HttpPost]
        public IActionResult SavePreferences([FromBody] UserPreferences model)
        {
            try
            {
                var options = new CookieOptions
                {
                    Expires  = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = false
                };
                Response.Cookies.Append("UserTheme",            model.Theme               ?? "light", options);
                Response.Cookies.Append("UserLanguage",         model.Language            ?? "en",    options);
                Response.Cookies.Append("EmailNotifications",   model.EmailNotifications.ToString(),  options);
                Response.Cookies.Append("DefaultPageSize",      model.DefaultPageSize.ToString(),     options);

                _logger.LogInformation("Preferences saved for user {User}", User.Identity?.Name);
                return Ok(new { success = true, message = "Preferences saved." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SavePreferences failed");
                return BadRequest(new { success = false, message = "Failed to save preferences." });
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
