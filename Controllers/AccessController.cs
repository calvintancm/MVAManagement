using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Data;
using MVAManagement.Models;
using MVAManagement.Models.MVA;
using System.Text.Json;

namespace MVAManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AccessController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccessController> _logger;

        public AccessController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccessController> logger)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // ─── Helpers ──────────────────────────────────────────────────────
        private JsonResult GridResult(object data, int total) =>
            Json(new { Data = data, Total = total, Errors = (object?)null },
                 new JsonSerializerOptions { PropertyNamingPolicy = null });

        private JsonResult GridError(string message) =>
            Json(new
            {
                Data = Array.Empty<object>(),
                Total = 0,
                Errors = new Dictionary<string, object>
                { ["error"] = new { errors = new[] { message } } }
            },
                 new JsonSerializerOptions { PropertyNamingPolicy = null });

        // ══════════════════════════════════════════════════════════════════
        // VIEW
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult UserRoles() => View();

        // ══════════════════════════════════════════════════════════════════
        // READ
        //
        // WHY fetch all then page manually:
        //   Roles live in AspNetUserRoles (separate table), not a column
        //   on AspNetUsers. EF cannot filter/sort by role in a single SQL
        //   query without a JOIN. So we:
        //     1. Apply username/email/lockout filters in SQL  (fast)
        //     2. Fetch matching users into memory
        //     3. Load roles per user  (Identity call)
        //     4. Apply role filter in-memory
        //     5. Page the final list manually
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> UserRolesRead(
            int skip = 0,
            int take = 20,
            string? userName = null,
            string? role = null,
            string? isActive = null,
            string? sort = null,
            string? dir = null)
        {
            try
            {
                // ── Step 1: SQL filters ───────────────────────────────────
                var q = _userManager.Users.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(userName))
                    q = q.Where(u => u.UserName!.Contains(userName)
                                  || (u.Email != null && u.Email.Contains(userName)));

                if (bool.TryParse(isActive, out var active))
                {
                    var now = DateTimeOffset.UtcNow;
                    q = active
                        ? q.Where(u => u.LockoutEnd == null || u.LockoutEnd <= now)
                        : q.Where(u => u.LockoutEnd != null && u.LockoutEnd > now);
                }

                // ── Step 2: Sort in SQL ───────────────────────────────────
                q = (sort, dir) switch
                {
                    ("UserName", "desc") => q.OrderByDescending(u => u.UserName),
                    ("Email", "desc") => q.OrderByDescending(u => u.Email),
                    ("Email", _) => q.OrderBy(u => u.Email),
                    _ => q.OrderBy(u => u.UserName)
                };

                // ── Step 3: Fetch all matching users into memory ──────────
                var users = await q.ToListAsync();

                // ── Step 4: Load roles and apply role filter in-memory ────
                var rows = new List<object>();
                foreach (var u in users)
                {
                    var userRoles = await _userManager.GetRolesAsync(u);
                    var isLocked = u.LockoutEnd.HasValue
                                 && u.LockoutEnd > DateTimeOffset.UtcNow;

                    // Only skip if a specific role was requested and user doesn't have it
                    if (!string.IsNullOrWhiteSpace(role) && !userRoles.Contains(role))
                        continue;

                    rows.Add(new
                    {
                        u.Id,
                        u.UserName,
                        u.Email,
                        u.PhoneNumber,
                        u.EmailConfirmed,
                        u.TwoFactorEnabled,
                        u.AccessFailedCount,
                        IsLocked = isLocked,
                        LockoutEnd = u.LockoutEnd?.ToString("dd MMM yyyy HH:mm") ?? "",
                        Roles = string.Join(", ", userRoles),
                        LastLoginAt = u.LastLoginAt?.ToString("dd MMM yyyy HH:mm") ?? ""
                    });
                }

                // ── Step 5: Page the filtered list ────────────────────────
                var filteredTotal = rows.Count;
                var pagedRows = rows.Skip(skip).Take(take).ToList();

                return GridResult(pagedRows, filteredTotal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRolesRead failed");
                return GridError("Failed to load users.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // ASSIGN ROLES
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> AssignRoles(
            [FromForm] string userId,
            [FromForm] string roles)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return GridError("User not found.");

                var newRoles = (roles ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrEmpty(r))
                    .ToList();

                var currentRoles = await _userManager.GetRolesAsync(user);

                // Remove roles no longer in the list
                var toRemove = currentRoles.Except(newRoles).ToList();
                if (toRemove.Any())
                    await _userManager.RemoveFromRolesAsync(user, toRemove);

                // Add new roles — create if they don't exist yet
                var toAdd = newRoles.Except(currentRoles).ToList();
                foreach (var r in toAdd)
                {
                    if (!await _roleManager.RoleExistsAsync(r))
                        await _roleManager.CreateAsync(new IdentityRole(r));
                }
                if (toAdd.Any())
                    await _userManager.AddToRolesAsync(user, toAdd);

                _logger.LogInformation(
                    "Roles updated for '{User}': [{Roles}] by admin '{Admin}'.",
                    user.UserName, string.Join(", ", newRoles), User.Identity?.Name);

                return GridResult(new[] { new { userId, roles } }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AssignRoles failed");
                return GridError("Failed to assign roles.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // RESET PASSWORD
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> ResetPassword(
            [FromForm] string userId,
            [FromForm] string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return GridError("Password must be at least 6 characters.");

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return GridError("User not found.");

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                    return GridError(errors);
                }

                // Also unlock and clear failed count on password reset
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);

                _logger.LogInformation(
                    "Password reset for '{User}' by admin '{Admin}'.",
                    user.UserName, User.Identity?.Name);

                return GridResult(new[] { new { userId } }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResetPassword failed");
                return GridError("Failed to reset password.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // LOCK / UNLOCK
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> ToggleLock(
            [FromForm] string userId,
            [FromForm] bool lockUser)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return GridError("User not found.");

                var currentUserId = _userManager.GetUserId(User);
                if (userId == currentUserId)
                    return GridError("You cannot lock your own account.");

                if (lockUser)
                {
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    await _userManager.SetLockoutEndDateAsync(user,
                        DateTimeOffset.UtcNow.AddYears(100));
                }
                else
                {
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    await _userManager.ResetAccessFailedCountAsync(user);
                }

                _logger.LogInformation(
                    "User '{User}' {Action} by admin '{Admin}'.",
                    user.UserName, lockUser ? "locked" : "unlocked", User.Identity?.Name);

                return GridResult(new[] { new { userId, isLocked = lockUser } }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ToggleLock failed");
                return GridError("Failed to toggle lock status.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // RESET FAILED COUNT
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> ResetFailedCount([FromForm] string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return GridError("User not found.");

                await _userManager.ResetAccessFailedCountAsync(user);

                _logger.LogInformation(
                    "Failed count reset for '{User}' by admin '{Admin}'.",
                    user.UserName, User.Identity?.Name);

                return GridResult(new[] { new { userId } }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResetFailedCount failed");
                return GridError("Failed to reset failed count.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // DELETE USER
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> UserDelete([FromForm] string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return GridError("User not found.");

                var currentUserId = _userManager.GetUserId(User);
                if (userId == currentUserId)
                    return GridError("You cannot delete your own account.");

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                    return GridError(errors);
                }

                _logger.LogWarning(
                    "User '{User}' deleted by admin '{Admin}'.",
                    user.UserName, User.Identity?.Name);

                return GridResult(Array.Empty<object>(), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserDelete failed");
                return GridError("Failed to delete user.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // AUDIT LOG
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult AuditLog() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> AuditLogRead(
            int skip = 0,
            int take = 20,
            string? username = null,
            string? isActive = null,
            string? isSuspicious = null,
            string? dateFrom = null,
            string? dateTo = null,
            string? sort = null,
            string? dir = null)
        {
            try
            {
                var q = _db.AuditSessionLogs.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(username))
                    q = q.Where(x => x.Username.Contains(username));

                if (bool.TryParse(isActive, out var active))
                    q = q.Where(x => x.IsActive == active);

                if (bool.TryParse(isSuspicious, out var suspicious))
                    q = q.Where(x => x.IsFlaggedSuspicious == suspicious);

                if (DateTime.TryParse(dateFrom, out var from))
                    q = q.Where(x => x.LoginTime >= from);

                if (DateTime.TryParse(dateTo, out var to))
                    q = q.Where(x => x.LoginTime <= to.AddDays(1));

                var total = await q.CountAsync();

                q = (sort, dir) switch
                {
                    ("Username", "desc") => q.OrderByDescending(x => x.Username),
                    ("Username", _) => q.OrderBy(x => x.Username),
                    ("LoginTime", "asc") => q.OrderBy(x => x.LoginTime),
                    ("IpAddress", "desc") => q.OrderByDescending(x => x.IpAddress),
                    ("IpAddress", _) => q.OrderBy(x => x.IpAddress),
                    _ => q.OrderByDescending(x => x.LoginTime)
                };

                var data = await q.Skip(skip).Take(take)
                    .Select(x => new
                    {
                        x.Id,
                        x.UserId,
                        x.Username,
                        x.SessionId,
                        x.IpAddress,
                        x.ComputerName,
                        x.UserAgent,
                        LoginTime = x.LoginTime.ToString("dd MMM yyyy HH:mm:ss"),
                        LogoutTime = x.LogoutTime.HasValue
                                     ? x.LogoutTime.Value.ToString("dd MMM yyyy HH:mm:ss")
                                     : "",
                        SessionDurationMinutes = x.LogoutTime.HasValue
                                     ? (int)(x.LogoutTime.Value - x.LoginTime).TotalMinutes
                                     : (int?)null,
                        x.IsActive,
                        x.LogoutReason,
                        x.IsFlaggedSuspicious,
                        x.SuspicionReason
                    })
                    .ToListAsync();

                return GridResult(data, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuditLogRead failed");
                return GridError("Failed to load audit log.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> TerminateSession([FromForm] int sessionId)
        {
            try
            {
                var session = await _db.AuditSessionLogs.FindAsync(sessionId);
                if (session == null) return GridError("Session not found.");
                if (!session.IsActive) return GridError("Session is already inactive.");

                session.IsActive = false;
                session.LogoutTime = DateTime.UtcNow;
                session.LogoutReason = "Forced Termination";

                await _db.SaveChangesAsync();

                _logger.LogWarning(
                    "Session '{Id}' for user '{User}' terminated by admin '{Admin}'.",
                    session.SessionId, session.Username, User.Identity?.Name);

                return GridResult(new[] { new { sessionId } }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TerminateSession failed");
                return GridError("Failed to terminate session.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> PurgeOldLogs()
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-90);
                var old = await _db.AuditSessionLogs
                    .Where(x => !x.IsActive && x.LoginTime < cutoff)
                    .ToListAsync();

                _db.AuditSessionLogs.RemoveRange(old);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Audit purge: {Count} old sessions removed.", old.Count);
                return GridResult(new[] { new { purged = old.Count } }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PurgeOldLogs failed");
                return GridError("Failed to purge old session logs.");
            }
        }
    }
}