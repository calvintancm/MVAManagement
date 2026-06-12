using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Data;
using MVAManagement.Models.MVA;
using System.Text.Json;

namespace MVAManagement.Controllers
{
    [Authorize]
    public class MedicalController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<MedicalController> _logger;

        public MedicalController(ApplicationDbContext db, ILogger<MedicalController> logger)
        {
            _db     = db;
            _logger = logger;
        }

        // ─── JSON helpers ──────────────────────────────────────────────────
        private JsonResult GridResult(object data, int total) =>
            Json(new { Data = data, Total = total, Errors = (object?)null },
                 new JsonSerializerOptions { PropertyNamingPolicy = null });

        private JsonResult GridError(string message) =>
            Json(new { Data = Array.Empty<object>(), Total = 0,
                       Errors = new Dictionary<string, object> { ["error"] = new { errors = new[] { message } } } },
                 new JsonSerializerOptions { PropertyNamingPolicy = null });

        // ─── DTO projection ────────────────────────────────────────────────
        private static object ProjectInjuryDto(InjuryRecord x) => new
        {
            x.Id,
            x.CaseFileId,
            FileNumber          = x.CaseFile != null ? x.CaseFile.FileNumber          : null,
            ClaimantName        = x.CaseFile != null ? x.CaseFile.PrimaryClaimantName : null,
            x.InjuryDescription,
            x.BodyPart,
            x.SeverityLevel,
            x.IsPermanentDisability,
            x.DisabilityPercentage,
            x.TreatmentStatus,
            x.HospitalName,
            x.DoctorName,
            x.MedicalSpecialty,
            x.MedicalReportStatus,
            x.MedicalReportReceivedDate
        };

        // ─── Shared dropdown loader ────────────────────────────────────────
        private async Task LoadViewBagsAsync()
        {
            ViewBag.CaseFiles = await _db.CaseFiles
                .Where(x => x.IsActive && !x.IsClosed)
                .OrderBy(x => x.FileNumber)
                .Select(x => new { x.Id, x.FileNumber, x.PrimaryClaimantName })
                .ToListAsync();
        }

        // ══════════════════════════════════════════════════════════════════
        //  VIEWS
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> InjuryRecords()
        {
            await LoadViewBagsAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PendingReports()
        {
            await LoadViewBagsAsync();
            return View();
        }

        // ══════════════════════════════════════════════════════════════════
        //  INJURY RECORDS — READ
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> InjuryRead(
            int skip = 0, int take = 15,
            string? search = null,
            string? severity = null, string? treatmentStatus = null,
            string? reportStatus = null, string? caseFileId = null,
            string? isPermanentDisability = null,
            string? scope = "all",             // "all" | "pending"
            string? sort = null, string? dir = null)
        {
            try
            {
                var q = _db.InjuryRecords
                    .Include(x => x.CaseFile)
                    .AsNoTracking()
                    .AsQueryable();

                // ── Scope: pending reports only ───────────────────────────
                if (scope == "pending")
                    q = q.Where(x => x.MedicalReportStatus == "Pending"
                                  || x.MedicalReportStatus == "Requested");

                // ── Filters ───────────────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(search))
                    q = q.Where(x =>
                        (x.CaseFile != null && x.CaseFile.FileNumber.Contains(search)) ||
                        (x.CaseFile != null && x.CaseFile.PrimaryClaimantName.Contains(search)) ||
                        x.InjuryDescription.Contains(search) ||
                        (x.BodyPart      != null && x.BodyPart.Contains(search)) ||
                        (x.HospitalName  != null && x.HospitalName.Contains(search)) ||
                        (x.DoctorName    != null && x.DoctorName.Contains(search)));

                if (!string.IsNullOrWhiteSpace(severity))
                    q = q.Where(x => x.SeverityLevel == severity);

                if (!string.IsNullOrWhiteSpace(treatmentStatus))
                    q = q.Where(x => x.TreatmentStatus == treatmentStatus);

                if (!string.IsNullOrWhiteSpace(reportStatus))
                    q = q.Where(x => x.MedicalReportStatus == reportStatus);

                if (int.TryParse(caseFileId, out var cfid) && cfid > 0)
                    q = q.Where(x => x.CaseFileId == cfid);

                if (bool.TryParse(isPermanentDisability, out var perm))
                    q = q.Where(x => x.IsPermanentDisability == perm);

                var total = await q.CountAsync();

                q = (sort, dir) switch
                {
                    ("SeverityLevel",   "desc") => q.OrderByDescending(x => x.SeverityLevel),
                    ("SeverityLevel",   _)      => q.OrderBy(x => x.SeverityLevel),
                    ("TreatmentStatus", "desc") => q.OrderByDescending(x => x.TreatmentStatus),
                    ("TreatmentStatus", _)      => q.OrderBy(x => x.TreatmentStatus),
                    ("FileNumber",      "desc") => q.OrderByDescending(x => x.CaseFile!.FileNumber),
                    ("FileNumber",      _)      => q.OrderBy(x => x.CaseFile!.FileNumber),
                    _                           => q.OrderBy(x => x.CaseFile!.FileNumber)
                };

                var data = await q.Skip(skip).Take(take).ToListAsync();
                return GridResult(data.Select(ProjectInjuryDto).ToList(), total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InjuryRead failed");
                return GridError("Failed to load injury records.");
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> InjuryCreate(InjuryRecord model)
        {
            try
            {
                ModelState.Remove(nameof(InjuryRecord.CaseFile));
                ModelState.Remove(nameof(InjuryRecord.MedicalExaminations));

                if (!ModelState.IsValid)
                    return GridError(string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                _db.InjuryRecords.Add(model);
                await _db.SaveChangesAsync();

                var saved = await _db.InjuryRecords
                    .Include(x => x.CaseFile)
                    .FirstAsync(x => x.Id == model.Id);

                return GridResult(new[] { ProjectInjuryDto(saved) }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InjuryCreate failed");
                return GridError("Failed to create injury record.");
            }
        }

        // ── UPDATE ────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> InjuryUpdate(InjuryRecord model)
        {
            try
            {
                ModelState.Remove(nameof(InjuryRecord.CaseFile));
                ModelState.Remove(nameof(InjuryRecord.MedicalExaminations));

                var existing = await _db.InjuryRecords
                    .Include(x => x.CaseFile)
                    .FirstOrDefaultAsync(x => x.Id == model.Id);

                if (existing == null) return GridError("Record not found.");

                existing.InjuryDescription        = model.InjuryDescription;
                existing.BodyPart                 = model.BodyPart;
                existing.SeverityLevel            = model.SeverityLevel;
                existing.IsPermanentDisability    = model.IsPermanentDisability;
                existing.DisabilityPercentage     = model.DisabilityPercentage;
                existing.TreatmentStatus          = model.TreatmentStatus;
                existing.HospitalName             = model.HospitalName;
                existing.DoctorName               = model.DoctorName;
                existing.MedicalSpecialty         = model.MedicalSpecialty;
                existing.MedicalReportStatus      = model.MedicalReportStatus;
                existing.MedicalReportReceivedDate = model.MedicalReportReceivedDate;

                await _db.SaveChangesAsync();
                return GridResult(new[] { ProjectInjuryDto(existing) }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InjuryUpdate failed");
                return GridError("Failed to update injury record.");
            }
        }

        // ── DESTROY ───────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> InjuryDestroy(int Id)
        {
            try
            {
                var existing = await _db.InjuryRecords.FindAsync(Id);
                if (existing == null) return GridError("Record not found.");

                var hasExams = await _db.MedicalExaminations.AnyAsync(x => x.InjuryRecordId == Id);
                if (hasExams)
                    return GridError("Cannot delete — this injury has linked medical examination records.");

                _db.InjuryRecords.Remove(existing);
                await _db.SaveChangesAsync();
                return GridResult(Array.Empty<object>(), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InjuryDestroy failed");
                return GridError("Failed to delete injury record.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  QUICK REPORT STATUS UPDATE (AJAX) — used from Pending Reports
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> InjuryMarkReportReceived(int id)
        {
            try
            {
                var existing = await _db.InjuryRecords
                    .Include(x => x.CaseFile)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (existing == null) return GridError("Record not found.");

                existing.MedicalReportStatus       = "Received";
                existing.MedicalReportReceivedDate = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return GridResult(new[] { ProjectInjuryDto(existing) }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InjuryMarkReportReceived failed");
                return GridError("Failed to update report status.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  SUMMARY STATS (AJAX)
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> MedicalSummaryStats()
        {
            try
            {
                var total       = await _db.InjuryRecords.CountAsync();
                var pending     = await _db.InjuryRecords.CountAsync(x =>
                                    x.MedicalReportStatus == "Pending" ||
                                    x.MedicalReportStatus == "Requested");
                var received    = await _db.InjuryRecords.CountAsync(x => x.MedicalReportStatus == "Received");
                var permanent   = await _db.InjuryRecords.CountAsync(x => x.IsPermanentDisability);
                var severe      = await _db.InjuryRecords.CountAsync(x =>
                                    x.SeverityLevel == "Severe" ||
                                    x.SeverityLevel == "Permanent Disability");

                return Json(new { total, pending, received, permanent, severe },
                            new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MedicalSummaryStats failed");
                return GridError("Failed to load stats.");
            }
        }
    }
}
