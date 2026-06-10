using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Data;
using MVAManagement.Models.MVA;
using System.Security.Claims;
using System.Text.Json;

namespace MVAManagement.Controllers
{
    [Authorize]
    public class CaseController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CaseController> _logger;

        public CaseController(ApplicationDbContext db, ILogger<CaseController> logger)
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

        // ─── Helper: apply shared filters to query ─────────────────────────
        private IQueryable<CaseFile> ApplyFilters(
            IQueryable<CaseFile> q,
            string? search, string? statusId, string? caseworkerId,
            string? isActive, string? isInLitigation,
            string? accidentDateFrom, string? accidentDateTo)
        {
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(x => x.FileNumber.Contains(search)
                               || x.PrimaryClaimantName.Contains(search)
                               || (x.CourtCaseNumber != null && x.CourtCaseNumber.Contains(search))
                               || (x.NationalId != null && x.NationalId.Contains(search))
                               || (x.MobileNumber != null && x.MobileNumber.Contains(search)));

            if (int.TryParse(statusId, out var sid) && sid > 0)
                q = q.Where(x => x.CaseStatusId == sid);

            if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                q = q.Where(x => x.AssignedCaseworkerId == cwid);

            if (bool.TryParse(isActive, out var active))
                q = q.Where(x => x.IsActive == active);

            if (bool.TryParse(isInLitigation, out var lit))
                q = q.Where(x => x.IsInLitigation == lit);

            if (DateTime.TryParse(accidentDateFrom, out var adf))
                q = q.Where(x => x.AccidentDate >= adf);

            if (DateTime.TryParse(accidentDateTo, out var adt))
                q = q.Where(x => x.AccidentDate <= adt);

            return q;
        }

        // ─── Helper: apply sort ───────────────────────────────────────────
        private IQueryable<CaseFile> ApplySort(IQueryable<CaseFile> q, string? sort, string? dir)
        {
            return (sort, dir) switch
            {
                ("FileNumber",           "desc") => q.OrderByDescending(x => x.FileNumber),
                ("FileNumber",           _)      => q.OrderBy(x => x.FileNumber),
                ("PrimaryClaimantName",  "desc") => q.OrderByDescending(x => x.PrimaryClaimantName),
                ("PrimaryClaimantName",  _)      => q.OrderBy(x => x.PrimaryClaimantName),
                ("AccidentDate",         "desc") => q.OrderByDescending(x => x.AccidentDate),
                ("AccidentDate",         _)      => q.OrderBy(x => x.AccidentDate),
                ("ClaimedAmount",        "desc") => q.OrderByDescending(x => x.ClaimedAmount),
                ("ClaimedAmount",        _)      => q.OrderBy(x => x.ClaimedAmount),
                ("NextHearingDate",      "desc") => q.OrderByDescending(x => x.NextHearingDate),
                ("NextHearingDate",      _)      => q.OrderBy(x => x.NextHearingDate),
                ("CreatedAt",            "desc") => q.OrderByDescending(x => x.CreatedAt),
                _                                => q.OrderByDescending(x => x.CreatedAt)
            };
        }

        // ─── Helper: project to grid DTO (avoids circular nav serialisation) ──
        private static object ProjectToDto(CaseFile x) => new
        {
            x.Id,
            x.FileNumber,
            x.CourtCaseNumber,
            x.PrimaryClaimantName,
            x.Gender,
            x.NationalId,
            x.MobileNumber,
            x.AccidentDate,
            x.ClientVehicleRole,
            x.IsInLitigation,
            x.IsActive,
            x.IsClosed,
            x.ClaimedAmount,
            x.CurrentOffer,
            x.MinimumSettlementTarget,
            x.TotalDisbursementAmount,
            x.NextHearingDate,
            x.CaseStatusId,
            CaseStatusName = x.CaseStatus != null ? x.CaseStatus.StatusName : null,
            CaseStatusCode = x.CaseStatus != null ? x.CaseStatus.StatusCode : null,
            x.AssignedCaseworkerId,
            AssignedCaseworkerName = x.AssignedCaseworker != null ? x.AssignedCaseworker.FullName : null,
            x.CreatedAt,
            x.UpdatedAt,
            x.Remarks,
            StatuteOfLimitationsDeadline = x.AccidentDate.HasValue ? (DateTime?)x.AccidentDate.Value.AddYears(6) : null
        };

        // ══════════════════════════════════════════════════════════════════
        // VIEWS
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> AllCases()
        {
            await LoadViewBagsAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> MyCases()
        {
            await LoadViewBagsAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ClosedCases()
        {
            await LoadViewBagsAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> OpenNewCase()
        {
            await LoadViewBagsAsync();
            // Generate next file number suggestion
            var year = DateTime.Now.Year;
            var count = await _db.CaseFiles.CountAsync(x => x.CreatedAt.Year == year);
            ViewBag.SuggestedFileNumber = $"PTC/MVA/{year}/{(count + 1):D4}";
            return View(new CaseFile { IsActive = true, ClientVehicleRole = "Driver" });
        }

        [HttpGet]
        public async Task<IActionResult> EditCase(int id)
        {
            var caseFile = await _db.CaseFiles
                .Include(x => x.CaseStatus)
                .Include(x => x.AssignedCaseworker)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (caseFile == null) return NotFound();

            await LoadViewBagsAsync();
            return View("OpenNewCase", caseFile);
        }

        [HttpGet]
        public async Task<IActionResult> ViewCase(int id)
        {
            var caseFile = await _db.CaseFiles
                .Include(x => x.CaseStatus)
                .Include(x => x.AssignedCaseworker)
                .Include(x => x.AccidentVehicles).ThenInclude(v => v.InsuranceProvider)
                .Include(x => x.InjuryRecords)
                .Include(x => x.HearingRecords).ThenInclude(h => h.CourtVenue)
                .Include(x => x.HearingRecords).ThenInclude(h => h.HearingStage)
                .Include(x => x.SettlementOffers)
                .Include(x => x.Disbursements)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (caseFile == null) return NotFound();

            await LoadViewBagsAsync();
            return View(caseFile);
        }

        // ── Load dropdowns ────────────────────────────────────────────────
        private async Task LoadViewBagsAsync()
        {
            ViewBag.CaseStatuses = await _db.CaseStatuses
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new { x.Id, x.StatusName, x.StatusCode })
                .ToListAsync();

            ViewBag.Caseworkers = await _db.CaseworkerProfiles
                .Where(x => x.IsActive)
                .OrderBy(x => x.FullName)
                .Select(x => new { x.Id, x.FullName, x.JobRole })
                .ToListAsync();

            ViewBag.HearingStages = await _db.HearingStages
                .OrderBy(x => x.StageCode)
                .Select(x => new { x.Id, x.StageCode, x.StageDescription })
                .ToListAsync();
        }

        // ══════════════════════════════════════════════════════════════════
        // READ  — shared endpoint, scoped by view parameter
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseRead(
            int skip = 0, int take = 15,
            string? search = null, string? statusId = null,
            string? caseworkerId = null, string? isActive = null,
            string? isInLitigation = null,
            string? accidentDateFrom = null, string? accidentDateTo = null,
            string? scope = "all",         // "all" | "mine" | "closed"
            string? sort = null, string? dir = null)
        {
            try
            {
                var q = _db.CaseFiles
                    .Include(x => x.CaseStatus)
                    .Include(x => x.AssignedCaseworker)
                    .AsNoTracking()
                    .AsQueryable();

                // ── Scope gates ───────────────────────────────────────────
                switch (scope?.ToLower())
                {
                    case "mine":
                        // Match by IdentityUserId or by username claim
                        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        q = q.Where(x => x.AssignedCaseworker != null
                                      && x.AssignedCaseworker.IdentityUserId == userId);
                        break;

                    case "closed":
                        q = q.Where(x => x.IsClosed);
                        break;

                    default: // "all" — no extra gate
                        break;
                }

                // ── Shared filters ────────────────────────────────────────
                q = ApplyFilters(q, search, statusId, caseworkerId,
                                 isActive, isInLitigation, accidentDateFrom, accidentDateTo);

                var total = await q.CountAsync();

                q = ApplySort(q, sort, dir);

                var data = await q.Skip(skip).Take(take).ToListAsync();
                return GridResult(data.Select(ProjectToDto).ToList(), total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseRead failed");
                return GridError("Failed to load case files.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // CREATE
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CaseCreate(CaseFile model)
        {
            try
            {
                // Remove computed/nav fields from validation
                ModelState.Remove(nameof(CaseFile.CaseStatus));
                ModelState.Remove(nameof(CaseFile.AssignedCaseworker));
                ModelState.Remove(nameof(CaseFile.CurrentHearingStage));
                ModelState.Remove(nameof(CaseFile.AccidentVehicles));
                ModelState.Remove(nameof(CaseFile.InjuryRecords));
                ModelState.Remove(nameof(CaseFile.CaseDocuments));
                ModelState.Remove(nameof(CaseFile.HearingRecords));
                ModelState.Remove(nameof(CaseFile.SettlementOffers));
                ModelState.Remove(nameof(CaseFile.CaseAppointments));
                ModelState.Remove(nameof(CaseFile.MedicalExaminations));
                ModelState.Remove(nameof(CaseFile.Correspondences));
                ModelState.Remove(nameof(CaseFile.Disbursements));
                ModelState.Remove(nameof(CaseFile.JournalEntries));

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    TempData["Error"] = string.Join("; ", errors);
                    await LoadViewBagsAsync();
                    return View("OpenNewCase", model);
                }

                if (await _db.CaseFiles.AnyAsync(x => x.FileNumber == model.FileNumber))
                {
                    TempData["Error"] = $"File number '{model.FileNumber}' already exists.";
                    await LoadViewBagsAsync();
                    return View("OpenNewCase", model);
                }

                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;
                _db.CaseFiles.Add(model);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Case file {model.FileNumber} created successfully.";
                return RedirectToAction("AllCases");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseCreate failed");
                TempData["Error"] = "Failed to create case file. Please try again.";
                await LoadViewBagsAsync();
                return View("OpenNewCase", model);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // UPDATE
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CaseUpdate(CaseFile model)
        {
            try
            {
                ModelState.Remove(nameof(CaseFile.CaseStatus));
                ModelState.Remove(nameof(CaseFile.AssignedCaseworker));
                ModelState.Remove(nameof(CaseFile.CurrentHearingStage));
                ModelState.Remove(nameof(CaseFile.AccidentVehicles));
                ModelState.Remove(nameof(CaseFile.InjuryRecords));
                ModelState.Remove(nameof(CaseFile.CaseDocuments));
                ModelState.Remove(nameof(CaseFile.HearingRecords));
                ModelState.Remove(nameof(CaseFile.SettlementOffers));
                ModelState.Remove(nameof(CaseFile.CaseAppointments));
                ModelState.Remove(nameof(CaseFile.MedicalExaminations));
                ModelState.Remove(nameof(CaseFile.Correspondences));
                ModelState.Remove(nameof(CaseFile.Disbursements));
                ModelState.Remove(nameof(CaseFile.JournalEntries));

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    TempData["Error"] = string.Join("; ", errors);
                    await LoadViewBagsAsync();
                    return View("OpenNewCase", model);
                }

                var existing = await _db.CaseFiles.FindAsync(model.Id);
                if (existing == null)
                {
                    TempData["Error"] = "Case file not found.";
                    return RedirectToAction("AllCases");
                }

                // Preserve immutable fields
                model.FileNumber = existing.FileNumber;
                model.CreatedAt  = existing.CreatedAt;

                // Apply updates
                existing.CourtCaseNumber          = model.CourtCaseNumber;
                existing.SolicitorFileReference   = model.SolicitorFileReference;
                existing.PrimaryClaimantName      = model.PrimaryClaimantName;
                existing.SecondaryClaimantName    = model.SecondaryClaimantName;
                existing.Gender                   = model.Gender;
                existing.DateOfBirth              = model.DateOfBirth;
                existing.NationalId               = model.NationalId;
                existing.TelephoneNumber          = model.TelephoneNumber;
                existing.MobileNumber             = model.MobileNumber;
                existing.FullAddress              = model.FullAddress;
                existing.ReferralSource           = model.ReferralSource;
                existing.AccidentDate             = model.AccidentDate;
                existing.ClientVehicleRole        = model.ClientVehicleRole;
                existing.AssignedCaseworkerId     = model.AssignedCaseworkerId;
                existing.ClaimedAmount            = model.ClaimedAmount;
                existing.MinimumSettlementTarget  = model.MinimumSettlementTarget;
                existing.CurrentOffer             = model.CurrentOffer;
                existing.TotalDisbursementAmount  = model.TotalDisbursementAmount;
                existing.CaseStatusId             = model.CaseStatusId;
                existing.CurrentHearingStageId    = model.CurrentHearingStageId;
                existing.NextHearingDate          = model.NextHearingDate;
                existing.CaseClosedDate           = model.CaseClosedDate;
                existing.IsInLitigation           = model.IsInLitigation;
                existing.IsActive                 = model.IsActive;
                existing.IsClosed                 = model.IsClosed;
                existing.SummonsDraftedDate       = model.SummonsDraftedDate;
                existing.SummonsSealedDate        = model.SummonsSealedDate;
                existing.Remarks                  = model.Remarks;
                existing.UpdatedAt                = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                TempData["Success"] = $"Case file {existing.FileNumber} updated successfully.";
                return RedirectToAction("AllCases");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseUpdate failed");
                TempData["Error"] = "Failed to update case file.";
                await LoadViewBagsAsync();
                return View("OpenNewCase", model);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // DELETE
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseDestroy(int Id)
        {
            try
            {
                var existing = await _db.CaseFiles.FindAsync(Id);
                if (existing == null) return GridError("Record not found.");

                // Safety: only allow deletion if not in litigation
                if (existing.IsInLitigation)
                    return GridError("Cannot delete a case that is in active litigation. Close the case first.");

                _db.CaseFiles.Remove(existing);
                await _db.SaveChangesAsync();
                return GridResult(Array.Empty<object>(), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseDestroy failed");
                return GridError("Failed to delete case file.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // QUICK STATUS UPDATE (AJAX) — used from grid action button
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseQuickClose(int id)
        {
            try
            {
                var existing = await _db.CaseFiles.FindAsync(id);
                if (existing == null) return GridError("Record not found.");

                existing.IsClosed       = true;
                existing.IsActive       = false;
                existing.CaseClosedDate = DateTime.UtcNow;
                existing.UpdatedAt      = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return GridResult(new[] { ProjectToDto(existing) }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseQuickClose failed");
                return GridError("Failed to close case.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // SUMMARY STATS (AJAX) — used by dashboard strip in AllCases header
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseSummaryStats()
        {
            try
            {
                var total      = await _db.CaseFiles.CountAsync();
                var active     = await _db.CaseFiles.CountAsync(x => x.IsActive && !x.IsClosed);
                var litigation = await _db.CaseFiles.CountAsync(x => x.IsInLitigation && !x.IsClosed);
                var closed     = await _db.CaseFiles.CountAsync(x => x.IsClosed);
                var thisMonth  = await _db.CaseFiles.CountAsync(x =>
                                    x.CreatedAt.Month == DateTime.UtcNow.Month &&
                                    x.CreatedAt.Year  == DateTime.UtcNow.Year);

                return Json(new { total, active, litigation, closed, thisMonth },
                            new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseSummaryStats failed");
                return GridError("Failed to load stats.");
            }
        }
    }
}
