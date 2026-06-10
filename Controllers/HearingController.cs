using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Data;
using MVAManagement.Models.MVA;
using System.Text.Json;

namespace MVAManagement.Controllers
{
    [Authorize]
    public class HearingController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<HearingController> _logger;

        public HearingController(ApplicationDbContext db, ILogger<HearingController> logger)
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

        // ─── Helper: project HearingRecord to flat DTO ────────────────────
        private static object ProjectToDto(HearingRecord h) => new
        {
            h.Id,
            h.CaseFileId,
            FileNumber           = h.CaseFile != null ? h.CaseFile.FileNumber : null,
            ClaimantName         = h.CaseFile != null ? h.CaseFile.PrimaryClaimantName : null,
            h.HearingStageId,
            StageCode            = h.HearingStage != null ? h.HearingStage.StageCode : null,
            StageDescription     = h.HearingStage != null ? h.HearingStage.StageDescription : null,
            h.CourtVenueId,
            VenueName            = h.CourtVenue != null ? h.CourtVenue.VenueName : null,
            VenueCode            = h.CourtVenue != null ? h.CourtVenue.VenueCode : null,
            h.ScheduledDate,
            h.HearingTime,
            h.CourtroomNumber,
            h.PresidingJudge,
            h.IsCompleted,
            h.HearingOutcome,
            h.AdjournedToDate,
            h.ProgressNotes,
            h.CreatedAt
        };

        // ─── Load dropdowns ────────────────────────────────────────────────
        private async Task LoadViewBagsAsync()
        {
            ViewBag.HearingStages = await _db.HearingStages
                .OrderBy(x => x.StageCode)
                .Select(x => new { x.Id, x.StageCode, x.StageDescription })
                .ToListAsync();

            ViewBag.CourtVenues = await _db.CourtVenues
                .OrderBy(x => x.VenueName)
                .Select(x => new { x.Id, x.VenueName, x.VenueCode, x.Jurisdiction })
                .ToListAsync();

            ViewBag.CaseFiles = await _db.CaseFiles
                .Where(x => x.IsActive && !x.IsClosed)
                .OrderBy(x => x.FileNumber)
                .Select(x => new { x.Id, x.FileNumber, x.PrimaryClaimantName })
                .ToListAsync();
        }

        // ══════════════════════════════════════════════════════════════════
        // VIEWS
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> AllHearings()
        {
            await LoadViewBagsAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Upcoming()
        {
            await LoadViewBagsAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ScheduleHearing()
        {
            await LoadViewBagsAsync();
            return View(new HearingRecord { ScheduledDate = DateTime.Today.AddDays(14) });
        }

        [HttpGet]
        public async Task<IActionResult> EditHearing(int id)
        {
            var record = await _db.HearingRecords
                .Include(x => x.CaseFile)
                .Include(x => x.HearingStage)
                .Include(x => x.CourtVenue)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (record == null) return NotFound();
            await LoadViewBagsAsync();
            return View("ScheduleHearing", record);
        }

        // ══════════════════════════════════════════════════════════════════
        // READ  — scope: "all" | "upcoming"
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> HearingRead(
            int skip = 0, int take = 15,
            string? search = null,
            string? stageId = null, string? venueId = null,
            string? isCompleted = null,
            string? dateFrom = null, string? dateTo = null,
            string? scope = "all",
            string? sort = null, string? dir = null)
        {
            try
            {
                var q = _db.HearingRecords
                    .Include(x => x.CaseFile)
                    .Include(x => x.HearingStage)
                    .Include(x => x.CourtVenue)
                    .AsNoTracking()
                    .AsQueryable();

                // ── Scope gate ────────────────────────────────────────────
                if (scope == "upcoming")
                {
                    q = q.Where(x => !x.IsCompleted && x.ScheduledDate >= DateTime.Today);
                }

                // ── Filters ───────────────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(search))
                    q = q.Where(x =>
                        (x.CaseFile != null && x.CaseFile.FileNumber.Contains(search)) ||
                        (x.CaseFile != null && x.CaseFile.PrimaryClaimantName.Contains(search)) ||
                        (x.PresidingJudge != null && x.PresidingJudge.Contains(search)) ||
                        (x.CourtroomNumber != null && x.CourtroomNumber.Contains(search)));

                if (int.TryParse(stageId, out var sid) && sid > 0)
                    q = q.Where(x => x.HearingStageId == sid);

                if (int.TryParse(venueId, out var vid) && vid > 0)
                    q = q.Where(x => x.CourtVenueId == vid);

                if (bool.TryParse(isCompleted, out var completed))
                    q = q.Where(x => x.IsCompleted == completed);

                if (DateTime.TryParse(dateFrom, out var df))
                    q = q.Where(x => x.ScheduledDate >= df);

                if (DateTime.TryParse(dateTo, out var dt))
                    q = q.Where(x => x.ScheduledDate <= dt);

                var total = await q.CountAsync();

                q = (sort, dir) switch
                {
                    ("ScheduledDate",  "desc") => q.OrderByDescending(x => x.ScheduledDate),
                    ("ScheduledDate",  _)      => q.OrderBy(x => x.ScheduledDate),
                    ("FileNumber",     "desc") => q.OrderByDescending(x => x.CaseFile!.FileNumber),
                    ("FileNumber",     _)      => q.OrderBy(x => x.CaseFile!.FileNumber),
                    ("StageCode",      "desc") => q.OrderByDescending(x => x.HearingStage!.StageCode),
                    ("StageCode",      _)      => q.OrderBy(x => x.HearingStage!.StageCode),
                    _                          => q.OrderBy(x => x.ScheduledDate)
                };

                var data = await q.Skip(skip).Take(take).ToListAsync();
                return GridResult(data.Select(ProjectToDto).ToList(), total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HearingRead failed");
                return GridError("Failed to load hearing records.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // CREATE
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> HearingCreate(HearingRecord model)
        {
            try
            {
                ModelState.Remove(nameof(HearingRecord.CaseFile));
                ModelState.Remove(nameof(HearingRecord.HearingStage));
                ModelState.Remove(nameof(HearingRecord.CourtVenue));

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    await LoadViewBagsAsync();
                    return View("ScheduleHearing", model);
                }

                model.CreatedAt = DateTime.UtcNow;
                _db.HearingRecords.Add(model);

                // Keep CaseFile.NextHearingDate in sync if this is in the future
                if (!model.IsCompleted && model.ScheduledDate >= DateTime.Today)
                {
                    var caseFile = await _db.CaseFiles.FindAsync(model.CaseFileId);
                    if (caseFile != null &&
                        (caseFile.NextHearingDate == null || model.ScheduledDate < caseFile.NextHearingDate))
                    {
                        caseFile.NextHearingDate = model.ScheduledDate;
                        caseFile.UpdatedAt       = DateTime.UtcNow;
                    }
                }

                await _db.SaveChangesAsync();
                TempData["Success"] = "Hearing scheduled successfully.";
                return RedirectToAction("AllHearings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HearingCreate failed");
                TempData["Error"] = "Failed to schedule hearing. Please try again.";
                await LoadViewBagsAsync();
                return View("ScheduleHearing", model);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // UPDATE
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> HearingUpdate(HearingRecord model)
        {
            try
            {
                ModelState.Remove(nameof(HearingRecord.CaseFile));
                ModelState.Remove(nameof(HearingRecord.HearingStage));
                ModelState.Remove(nameof(HearingRecord.CourtVenue));

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    await LoadViewBagsAsync();
                    return View("ScheduleHearing", model);
                }

                var existing = await _db.HearingRecords.FindAsync(model.Id);
                if (existing == null)
                {
                    TempData["Error"] = "Hearing record not found.";
                    return RedirectToAction("AllHearings");
                }

                existing.CaseFileId      = model.CaseFileId;
                existing.HearingStageId  = model.HearingStageId;
                existing.CourtVenueId    = model.CourtVenueId;
                existing.ScheduledDate   = model.ScheduledDate;
                existing.HearingTime     = model.HearingTime;
                existing.CourtroomNumber = model.CourtroomNumber;
                existing.PresidingJudge  = model.PresidingJudge;
                existing.IsCompleted     = model.IsCompleted;
                existing.HearingOutcome  = model.HearingOutcome;
                existing.AdjournedToDate = model.AdjournedToDate;
                existing.ProgressNotes   = model.ProgressNotes;

                await _db.SaveChangesAsync();
                TempData["Success"] = "Hearing record updated.";
                return RedirectToAction("AllHearings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HearingUpdate failed");
                TempData["Error"] = "Failed to update hearing record.";
                await LoadViewBagsAsync();
                return View("ScheduleHearing", model);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // DELETE
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> HearingDestroy(int Id)
        {
            try
            {
                var existing = await _db.HearingRecords.FindAsync(Id);
                if (existing == null) return GridError("Record not found.");

                if (existing.IsCompleted)
                    return GridError("Cannot delete a completed hearing record — it forms part of the litigation audit trail.");

                _db.HearingRecords.Remove(existing);
                await _db.SaveChangesAsync();
                return GridResult(Array.Empty<object>(), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HearingDestroy failed");
                return GridError("Failed to delete hearing record.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // QUICK COMPLETE (AJAX) — mark hearing done from grid
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> HearingQuickComplete(int id, string? outcome)
        {
            try
            {
                var existing = await _db.HearingRecords.FindAsync(id);
                if (existing == null) return GridError("Record not found.");

                existing.IsCompleted    = true;
                existing.HearingOutcome = outcome;

                await _db.SaveChangesAsync();
                return GridResult(new[] { ProjectToDto(
                    await _db.HearingRecords
                        .Include(x => x.CaseFile)
                        .Include(x => x.HearingStage)
                        .Include(x => x.CourtVenue)
                        .FirstAsync(x => x.Id == id)) }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HearingQuickComplete failed");
                return GridError("Failed to mark hearing complete.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // SUMMARY STATS (AJAX)
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> HearingSummaryStats()
        {
            try
            {
                var today   = DateTime.Today;
                var week    = today.AddDays(7);
                var total   = await _db.HearingRecords.CountAsync();
                var pending = await _db.HearingRecords.CountAsync(x => !x.IsCompleted && x.ScheduledDate >= today);
                var thisWeek= await _db.HearingRecords.CountAsync(x => !x.IsCompleted && x.ScheduledDate >= today && x.ScheduledDate <= week);
                var today_c = await _db.HearingRecords.CountAsync(x => !x.IsCompleted && x.ScheduledDate == today);
                var done    = await _db.HearingRecords.CountAsync(x => x.IsCompleted);

                return Json(new { total, pending, thisWeek, today = today_c, done },
                            new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HearingSummaryStats failed");
                return GridError("Failed to load stats.");
            }
        }
    }
}
