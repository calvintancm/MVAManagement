using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MVAManagement.Data;
using MVAManagement.Models;
using MVAManagement.Models.MVA;
using MVAManagement.ViewModels.Account;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;


namespace MVAManagement.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;
        // Step 2: Replace the constructor with this:
        public AccountController(
            ILogger<AccountController> logger,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)                         // ← ADD THIS PARAMETER
        {
            _logger = logger;
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;                             // ← ADD THIS LINE
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
        // ═══════════════════════════════════════════════════════════════
        // LOGIN POST — full replacement
        // ═══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByNameAsync(model.UserName.Trim());

            if (user == null)
            {
                _logger.LogWarning("Login failed — unknown username: {User}", model.UserName);
                TempData["ErrorMessage"] = "Invalid username or password.";
                return View(model);
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Locked-out login attempt: {User}", model.UserName);
                TempData["ErrorMessage"] = "Account is temporarily locked. Please try again later.";
                return View(model);
            }

            var passwordOk = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordOk)
            {
                await _userManager.AccessFailedAsync(user);
                _logger.LogWarning("Login failed — wrong password for: {User}", model.UserName);
                TempData["ErrorMessage"] = "Invalid username or password.";
                return View(model);
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name,           user.UserName ?? model.UserName),
        new Claim(ClaimTypes.Email,          user.Email    ?? string.Empty),
    };
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProps = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddHours(8) : (DateTimeOffset?)null,
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

            // ── Update LastLoginAt ────────────────────────────────────
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // ── Write session audit log ───────────────────────────────
            var sessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("AuditSessionId", sessionId);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            var computerName = string.Empty;
            try
            {
                // Best-effort reverse DNS — may not resolve in all environments
                var ipAddr = HttpContext.Connection.RemoteIpAddress;
                if (ipAddr != null)
                {
                    var hostEntry = await System.Net.Dns.GetHostEntryAsync(ipAddr);
                    computerName = hostEntry.HostName;
                }
            }
            catch { /* non-critical */ }

            _db.AuditSessionLogs.Add(new AuditSessionLog
            {
                UserId = user.Id,
                Username = user.UserName ?? model.UserName,
                SessionId = sessionId,
                IpAddress = ip,
                ComputerName = computerName,
                UserAgent = userAgent,
                LoginTime = DateTime.UtcNow,
                IsActive = true,
                IsFlaggedSuspicious = false
            });
            await _db.SaveChangesAsync();

            HttpContext.Session.SetString("UserName", user.UserName ?? model.UserName);
            HttpContext.Session.SetString("UserEmail", user.Email ?? string.Empty);
            HttpContext.Session.SetString("UserRoles", string.Join(",", roles));

            _logger.LogInformation("User {User} logged in. Roles: [{Roles}]",
                user.UserName, string.Join(", ", roles));

            return RedirectToLocal(returnUrl);
        }


        // ═══════════════════════════════════════════════════════════════
// LOGOUT POST — full replacement
// ═══════════════════════════════════════════════════════════════
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize]
public async Task<IActionResult> Logout()
{
    var userName  = User.Identity?.Name;
    var sessionId = HttpContext.Session.GetString("AuditSessionId");
 
    // ── Close session audit log entry ─────────────────────────
    if (!string.IsNullOrEmpty(sessionId))
    {
        var session = await _db.AuditSessionLogs
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);
 
        if (session != null)
        {
            session.IsActive     = false;
            session.LogoutTime   = DateTime.UtcNow;
            session.LogoutReason = "User Logout";
            await _db.SaveChangesAsync();
        }
    }
 
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
