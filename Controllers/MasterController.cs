using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Data;
using MVAManagement.Models.MVA;
using System.Text.Json;

namespace MVAManagement.Controllers
{
    //[Authorize(Roles = "Admin")]
    public class MasterController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<MasterController> _logger;

        public MasterController(ApplicationDbContext db, ILogger<MasterController> logger)
        {
            _db     = db;
            _logger = logger;
        }

        // ─── Helper: standard JSON response ───────────────────────────────
        private JsonResult GridResult(object data, int total) =>
            Json(new { Data = data, Total = total, Errors = (object?)null },
                 new JsonSerializerOptions { PropertyNamingPolicy = null });

        private JsonResult GridError(string message) =>
            Json(new { Data = Array.Empty<object>(), Total = 0,
                       Errors = new Dictionary<string, object> { ["error"] = new { errors = new[] { message } } } },
                 new JsonSerializerOptions { PropertyNamingPolicy = null });

        // ══════════════════════════════════════════════════════════════════
        // 1. CASE STATUS
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult CaseStatus() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseStatusRead(
            int skip = 0, int take = 15,
            string? statusName = null,
            string? sort = null, string? dir = null)
        {
            try
            {
                var q = _db.CaseStatuses.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(statusName))
                    q = q.Where(x => x.StatusName.Contains(statusName)
                                   || (x.StatusCode != null && x.StatusCode.Contains(statusName)));

                var total = await q.CountAsync();

                q = (sort, dir) switch
                {
                    ("StatusCode", "desc") => q.OrderByDescending(x => x.StatusCode),
                    ("StatusCode", _) => q.OrderBy(x => x.StatusCode),
                    ("StatusName", "desc") => q.OrderByDescending(x => x.StatusName),
                    ("StatusName", _) => q.OrderBy(x => x.StatusName),
                    ("DisplayOrder", "desc") => q.OrderByDescending(x => x.DisplayOrder),
                    _ => q.OrderBy(x => x.DisplayOrder)
                };

                var data = await q.Skip(skip).Take(take).ToListAsync();
                return GridResult(data, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseStatusRead failed");
                return GridError("Failed to load case statuses.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseStatusCreate(CaseStatus model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return GridError(string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                if (!string.IsNullOrWhiteSpace(model.StatusCode) &&
                    await _db.CaseStatuses.AnyAsync(x => x.StatusCode == model.StatusCode))
                    return GridError($"Status code '{model.StatusCode}' already exists.");

                _db.CaseStatuses.Add(model);
                await _db.SaveChangesAsync();
                return GridResult(new[] { model }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseStatusCreate failed");
                return GridError("Failed to create case status.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseStatusUpdate(CaseStatus model)
        {
            try
            {
                var existing = await _db.CaseStatuses.FindAsync(model.Id);
                if (existing == null) return GridError("Record not found.");

                existing.StatusName = model.StatusName;
                existing.StatusCode = model.StatusCode;
                existing.DisplayOrder = model.DisplayOrder;

                await _db.SaveChangesAsync();
                return GridResult(new[] { existing }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseStatusUpdate failed");
                return GridError("Failed to update case status.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseStatusDestroy(int Id)
        {
            try
            {
                var existing = await _db.CaseStatuses.FindAsync(Id);
                if (existing == null) return GridError("Record not found.");

                var inUse = await _db.CaseFiles.AnyAsync(c => c.CaseStatusId == Id);
                if (inUse) return GridError("Cannot delete — this status is used by one or more case files.");

                _db.CaseStatuses.Remove(existing);
                await _db.SaveChangesAsync();
                return GridResult(Array.Empty<object>(), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseStatusDestroy failed");
                return GridError("Failed to delete case status.");
            }
        }



        // ══════════════════════════════════════════════════════════════════
        // 2. COURT VENUE
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult CourtVenue() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CourtVenueRead(
            int skip = 0, int take = 15,
            string? venueName = null, string? jurisdiction = null,
            string? sort = null, string? dir = null)
        {
            try
            {
                var q = _db.CourtVenues.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(venueName))
                    q = q.Where(x => x.VenueName.Contains(venueName)
                                   || (x.VenueCode != null && x.VenueCode.Contains(venueName)));

                if (!string.IsNullOrWhiteSpace(jurisdiction))
                    q = q.Where(x => x.Jurisdiction != null && x.Jurisdiction.Contains(jurisdiction));

                var total = await q.CountAsync();

                q = (sort, dir) switch
                {
                    ("VenueCode", "desc") => q.OrderByDescending(x => x.VenueCode),
                    ("VenueCode", _) => q.OrderBy(x => x.VenueCode),
                    ("Jurisdiction", "desc") => q.OrderByDescending(x => x.Jurisdiction),
                    ("Jurisdiction", _) => q.OrderBy(x => x.Jurisdiction),
                    ("VenueName", "desc") => q.OrderByDescending(x => x.VenueName),
                    _ => q.OrderBy(x => x.VenueName)
                };

                var data = await q.Skip(skip).Take(take).ToListAsync();
                return GridResult(data, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CourtVenueRead failed");
                return GridError("Failed to load court venues.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CourtVenueCreate(CourtVenue model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return GridError(string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                _db.CourtVenues.Add(model);
                await _db.SaveChangesAsync();
                return GridResult(new[] { model }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CourtVenueCreate failed");
                return GridError("Failed to create court venue.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CourtVenueUpdate(CourtVenue model)
        {
            try
            {
                var existing = await _db.CourtVenues.FindAsync(model.Id);
                if (existing == null) return GridError("Record not found.");

                existing.VenueName = model.VenueName;
                existing.VenueCode = model.VenueCode;
                existing.Jurisdiction = model.Jurisdiction;
                existing.Address = model.Address;
                existing.ContactNumber = model.ContactNumber;

                await _db.SaveChangesAsync();
                return GridResult(new[] { existing }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CourtVenueUpdate failed");
                return GridError("Failed to update court venue.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CourtVenueDestroy(int Id)
        {
            try
            {
                var existing = await _db.CourtVenues.FindAsync(Id);
                if (existing == null) return GridError("Record not found.");

                var inUse = await _db.HearingRecords.AnyAsync(h => h.CourtVenueId == Id);
                if (inUse) return GridError("Cannot delete — this venue is referenced in hearing records.");

                _db.CourtVenues.Remove(existing);
                await _db.SaveChangesAsync();
                return GridResult(Array.Empty<object>(), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CourtVenueDestroy failed");
                return GridError("Failed to delete court venue.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // 3. INSURER REGISTRY
        // Model: Id, InsurerName, InsurerCode, ClaimsContactNumber,
        //        ClaimsEmail, IsActive
        // NO: ContactPerson, ContactPhone, ContactEmail, Address,
        //     CreatedAt, UpdatedAt
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult InsurerRegistry() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> InsurerRead(
            int skip = 0, int take = 15,
            string? insurerName = null, string? isActive = null,
            string? sort = null, string? dir = null)
        {
            try
            {
                var q = _db.InsurerRegistries.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(insurerName))
                    q = q.Where(x => x.InsurerName.Contains(insurerName)
                                   || (x.InsurerCode != null && x.InsurerCode.Contains(insurerName)));

                if (bool.TryParse(isActive, out var active))
                    q = q.Where(x => x.IsActive == active);

                var total = await q.CountAsync();

                q = (sort, dir) switch
                {
                    ("InsurerCode", "desc") => q.OrderByDescending(x => x.InsurerCode),
                    ("InsurerCode", _) => q.OrderBy(x => x.InsurerCode),
                    ("InsurerName", "desc") => q.OrderByDescending(x => x.InsurerName),
                    _ => q.OrderBy(x => x.InsurerName)
                };

                var data = await q.Skip(skip).Take(take).ToListAsync();
                return GridResult(data, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InsurerRead failed");
                return GridError("Failed to load insurer registry.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> InsurerCreate(InsurerRegistry model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return GridError(string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                if (!string.IsNullOrWhiteSpace(model.InsurerCode) &&
                    await _db.InsurerRegistries.AnyAsync(x => x.InsurerCode == model.InsurerCode))
                    return GridError($"Insurer code '{model.InsurerCode}' already exists.");

                _db.InsurerRegistries.Add(model);
                await _db.SaveChangesAsync();
                return GridResult(new[] { model }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InsurerCreate failed");
                return GridError("Failed to create insurer.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> InsurerUpdate(InsurerRegistry model)
        {
            try
            {
                var existing = await _db.InsurerRegistries.FindAsync(model.Id);
                if (existing == null) return GridError("Record not found.");

                existing.InsurerName = model.InsurerName;
                existing.InsurerCode = model.InsurerCode;
                existing.ClaimsContactNumber = model.ClaimsContactNumber;
                existing.ClaimsEmail = model.ClaimsEmail;
                existing.IsActive = model.IsActive;

                await _db.SaveChangesAsync();
                return GridResult(new[] { existing }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InsurerUpdate failed");
                return GridError("Failed to update insurer.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> InsurerDestroy(int Id)
        {
            try
            {
                var existing = await _db.InsurerRegistries.FindAsync(Id);
                if (existing == null) return GridError("Record not found.");

                _db.InsurerRegistries.Remove(existing);
                await _db.SaveChangesAsync();
                return GridResult(Array.Empty<object>(), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InsurerDestroy failed");
                return GridError("Failed to delete insurer.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // 4. CASEWORKER PROFILE
        // Model: Id, IdentityUserId, FullName, Username, JobRole,
        //        SystemRole, Email, ContactNumber, JoinedDate, IsActive
        // NO: PhoneNumber, BarNumber, AvatarClass, MaxCaseLoad,
        //     CreatedAt, UpdatedAt
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult Caseworker() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseworkerRead(
            int skip = 0, int take = 15,
            string? name = null, string? jobRole = null,
            string? isActive = null, string? sort = null, string? dir = null)
        {
            try
            {
                var q = _db.CaseworkerProfiles.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(name))
                    q = q.Where(x => x.FullName.Contains(name)
                                   || x.Username.Contains(name)
                                   || (x.Email != null && x.Email.Contains(name)));

                if (!string.IsNullOrWhiteSpace(jobRole))
                    q = q.Where(x => x.JobRole == jobRole);

                if (bool.TryParse(isActive, out var active))
                    q = q.Where(x => x.IsActive == active);

                var total = await q.CountAsync();

                q = (sort, dir) switch
                {
                    ("JobRole", "desc") => q.OrderByDescending(x => x.JobRole),
                    ("JobRole", _) => q.OrderBy(x => x.JobRole),
                    ("SystemRole", "desc") => q.OrderByDescending(x => x.SystemRole),
                    ("SystemRole", _) => q.OrderBy(x => x.SystemRole),
                    ("FullName", "desc") => q.OrderByDescending(x => x.FullName),
                    _ => q.OrderBy(x => x.FullName)
                };

                var data = await q.Skip(skip).Take(take).ToListAsync();
                return GridResult(data, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseworkerRead failed");
                return GridError("Failed to load caseworkers.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseworkerCreate(CaseworkerProfile model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return GridError(string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                _db.CaseworkerProfiles.Add(model);
                await _db.SaveChangesAsync();
                return GridResult(new[] { model }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseworkerCreate failed");
                return GridError("Failed to create caseworker.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseworkerUpdate(CaseworkerProfile model)
        {
            try
            {
                var existing = await _db.CaseworkerProfiles.FindAsync(model.Id);
                if (existing == null) return GridError("Record not found.");

                existing.FullName = model.FullName;
                existing.Username = model.Username;
                existing.JobRole = model.JobRole;
                existing.SystemRole = model.SystemRole;
                existing.Email = model.Email;
                existing.ContactNumber = model.ContactNumber;
                existing.JoinedDate = model.JoinedDate;
                existing.IsActive = model.IsActive;

                await _db.SaveChangesAsync();
                return GridResult(new[] { existing }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseworkerUpdate failed");
                return GridError("Failed to update caseworker.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseworkerDestroy(int Id)
        {
            try
            {
                var existing = await _db.CaseworkerProfiles.FindAsync(Id);
                if (existing == null) return GridError("Record not found.");

                var inUse = await _db.CaseFiles.AnyAsync(c => c.AssignedCaseworkerId == Id);
                if (inUse) return GridError("Cannot delete — this caseworker has active case files assigned.");

                _db.CaseworkerProfiles.Remove(existing);
                await _db.SaveChangesAsync();
                return GridResult(Array.Empty<object>(), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseworkerDestroy failed");
                return GridError("Failed to delete caseworker.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // 5. DISBURSEMENT CATEGORY
        //    NOTE: DisbursementCategory is not a separate EF model yet —
        //    it maps to the DisbursementCategory string column on
        //    CaseDisbursement. This module manages a lookup table for it.
        //    Add DbSet<DisbursementCategory> to ApplicationDbContext and
        //    create the SQL table to activate. Scaffold below is ready.
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult DisbursementCategory() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> DisbursementCategoryRead(
            int skip = 0, int take = 15,
            string? categoryName = null, string? isActive = null,
            string? sort = null, string? dir = null)
        {
            try
            {
                var q = _db.DisbursementCategories.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(categoryName))
                    q = q.Where(x => x.CategoryName.Contains(categoryName));

                if (bool.TryParse(isActive, out var active))
                    q = q.Where(x => x.IsActive == active);

                var total = await q.CountAsync();

                q = (sort, dir) switch
                {
                    ("CategoryName",  "desc") => q.OrderByDescending(x => x.CategoryName),
                    ("CategoryName",  _)      => q.OrderBy(x => x.CategoryName),
                    ("DisplayOrder",  "desc") => q.OrderByDescending(x => x.DisplayOrder),
                    _                         => q.OrderBy(x => x.DisplayOrder)
                };

                var data = await q.Skip(skip).Take(take).ToListAsync();
                return GridResult(data, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DisbursementCategoryRead failed");
                return GridError("Failed to load disbursement categories.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> DisbursementCategoryCreate(DisbursementCategory model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return GridError(string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                if (await _db.DisbursementCategories.AnyAsync(x => x.CategoryName == model.CategoryName))
                    return GridError($"Category '{model.CategoryName}' already exists.");

                model.HexColor  ??= "#94A3B8";
                model.CreatedAt   = DateTime.UtcNow;
                model.UpdatedAt   = DateTime.UtcNow;
                _db.DisbursementCategories.Add(model);
                await _db.SaveChangesAsync();
                return GridResult(new[] { model }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DisbursementCategoryCreate failed");
                return GridError("Failed to create category.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> DisbursementCategoryUpdate(DisbursementCategory model)
        {
            try
            {
                var existing = await _db.DisbursementCategories.FindAsync(model.Id);
                if (existing == null) return GridError("Record not found.");

                existing.CategoryName = model.CategoryName;
                existing.Description  = model.Description;
                existing.HexColor     = model.HexColor ?? "#94A3B8";
                existing.DisplayOrder = model.DisplayOrder;
                existing.IsActive     = model.IsActive;
                existing.UpdatedAt    = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return GridResult(new[] { existing }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DisbursementCategoryUpdate failed");
                return GridError("Failed to update category.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> DisbursementCategoryDestroy(int Id)
        {
            try
            {
                var existing = await _db.DisbursementCategories.FindAsync(Id);
                if (existing == null) return GridError("Record not found.");

                _db.DisbursementCategories.Remove(existing);
                await _db.SaveChangesAsync();
                return GridResult(Array.Empty<object>(), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DisbursementCategoryDestroy failed");
                return GridError("Failed to delete category.");
            }
        }
    }
}
