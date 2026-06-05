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
    //[Authorize(Roles = "Admin")]
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
        // USER / ROLE MANAGEMENT
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult UserRoles() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> UserRolesRead(
            int skip = 0, int take = 20,
            string? userName = null, string? role = null,
            string? isActive = null,
            string? sort = null, string? dir = null)
        {
            try
            {
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

                var total = await q.CountAsync();

                q = (sort, dir) switch
                {
                    ("UserName", "desc") => q.OrderByDescending(u => u.UserName),
                    ("Email", "desc") => q.OrderByDescending(u => u.Email),
                    ("Email", _) => q.OrderBy(u => u.Email),
                    _ => q.OrderBy(u => u.UserName)
                };

                var users = await q.Skip(skip).Take(take).ToListAsync();

                var rows = new List<object>();
                foreach (var u in users)
                {
                    var roles = await _userManager.GetRolesAsync(u);
                    var isLocked = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow;

                    // Post-fetch role filter
                    if (!string.IsNullOrWhiteSpace(role) && !roles.Contains(role))
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
                        Roles = string.Join(", ", roles),
                        LastLoginAt = u.LastLoginAt?.ToString("dd MMM yyyy HH:mm") ?? ""
                    });
                }

                return GridResult(rows, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRolesRead failed");
                return GridError("Failed to load users.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> GetAllRoles()
        {
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();
            return Json(roles, new JsonSerializerOptions { PropertyNamingPolicy = null });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> AssignRoles([FromForm] string userId,
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

                var toRemove = currentRoles.Except(newRoles).ToList();
                if (toRemove.Any())
                    await _userManager.RemoveFromRolesAsync(user, toRemove);

                var toAdd = newRoles.Except(currentRoles).ToList();
                foreach (var r in toAdd)
                {
                    if (!await _roleManager.RoleExistsAsync(r))
                        await _roleManager.CreateAsync(new IdentityRole(r));
                }
                if (toAdd.Any())
                    await _userManager.AddToRolesAsync(user, toAdd);

                _logger.LogInformation("Roles updated for {User}: [{Roles}]",
                    user.UserName, string.Join(", ", newRoles));

                return GridResult(new[] { new { userId, roles } }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AssignRoles failed");
                return GridError("Failed to assign roles.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> ToggleLock([FromForm] string userId,
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

                _logger.LogInformation("User {User} was {Action} by {Admin}.",
                    user.UserName, lockUser ? "locked" : "unlocked", User.Identity?.Name);

                return GridResult(new[] { new { userId, isLocked = lockUser } }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ToggleLock failed");
                return GridError("Failed to toggle lock status.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> ResetFailedCount([FromForm] string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return GridError("User not found.");

                await _userManager.ResetAccessFailedCountAsync(user);

                _logger.LogInformation("Access failed count reset for {User} by {Admin}.",
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
        // AUDIT LOG  —  Session-based model
        //
        // AuditSessionLog tracks LOGIN SESSIONS only:
        //   UserId, Username, SessionId, IpAddress, ComputerName, UserAgent,
        //   LoginTime, LogoutTime, IsActive, LogoutReason,
        //   IsFlaggedSuspicious, SuspicionReason
        //
        // There is NO general-purpose Action/Details/PerformedBy column.
        // This view shows login history: who logged in, from where,
        // how long the session lasted, and whether it was flagged.
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult AuditLog() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> AuditLogRead(
            int skip = 0, int take = 20,
            string? username = null, string? isActive = null,
            string? isSuspicious = null,
            string? dateFrom = null, string? dateTo = null,
            string? sort = null, string? dir = null)
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

                // Project to anonymous type — excludes computed property
                // SessionDurationMinutes which EF cannot translate to SQL
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

        // ── Terminate an active session (admin forced logout) ─────────────
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

                _logger.LogWarning("Session {SessionId} for user {User} forcibly terminated by {Admin}.",
                    session.SessionId, session.Username, User.Identity?.Name);

                return GridResult(new[] { new { sessionId } }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TerminateSession failed");
                return GridError("Failed to terminate session.");
            }
        }

        // ── Purge old inactive sessions ────────────────────────────────────
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

                _logger.LogInformation("Audit session purge: {Count} entries removed.", old.Count);

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