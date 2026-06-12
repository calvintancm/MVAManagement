using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Data;
using MVAManagement.Models.MVA;
using System.Text.Json;

namespace MVAManagement.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(ApplicationDbContext db, ILogger<DocumentController> logger)
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
        private static object ProjectDto(CaseDocument x) => new
        {
            x.Id,
            x.CaseFileId,
            FileNumber        = x.CaseFile != null ? x.CaseFile.FileNumber          : null,
            ClaimantName      = x.CaseFile != null ? x.CaseFile.PrimaryClaimantName : null,
            x.DocumentName,
            x.DocumentCategory,
            x.ExpectedFrom,
            x.IsReceived,
            x.ReceivedDate,
            x.CollectionStatus,
            x.DigitalStoragePath,
            x.OriginalFileName,
            x.FileMimeType,
            x.UploadedBy,
            x.UploadedAt,
            x.Remarks
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
        public async Task<IActionResult> AllDocuments()
        {
            await LoadViewBagsAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Outstanding()
        {
            await LoadViewBagsAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> UploadDocument()
        {
            await LoadViewBagsAsync();
            return View(new CaseDocument
            {
                CollectionStatus = "Awaiting",
                DocumentCategory = "Other"
            });
        }

        [HttpGet]
        public async Task<IActionResult> EditDocument(int id)
        {
            var record = await _db.CaseDocuments
                .Include(x => x.CaseFile)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (record == null) return NotFound();
            await LoadViewBagsAsync();
            return View("UploadDocument", record);
        }

        // ══════════════════════════════════════════════════════════════════
        //  READ  — scope: "all" | "outstanding"
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> DocumentRead(
            int skip = 0, int take = 15,
            string? search = null,
            string? category = null, string? collectionStatus = null,
            string? isReceived = null, string? caseFileId = null,
            string? expectedFrom = null,
            string? scope = "all",
            string? sort = null, string? dir = null)
        {
            try
            {
                var q = _db.CaseDocuments
                    .Include(x => x.CaseFile)
                    .AsNoTracking()
                    .AsQueryable();

                // ── Scope gate ────────────────────────────────────────────
                if (scope == "outstanding")
                    q = q.Where(x => !x.IsReceived
                                  && x.CollectionStatus != "Not Required"
                                  && x.CollectionStatus != "Received");

                // ── Filters ───────────────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(search))
                    q = q.Where(x =>
                        (x.CaseFile != null && x.CaseFile.FileNumber.Contains(search)) ||
                        (x.CaseFile != null && x.CaseFile.PrimaryClaimantName.Contains(search)) ||
                        x.DocumentName.Contains(search) ||
                        (x.ExpectedFrom      != null && x.ExpectedFrom.Contains(search)) ||
                        (x.OriginalFileName  != null && x.OriginalFileName.Contains(search)));

                if (!string.IsNullOrWhiteSpace(category))
                    q = q.Where(x => x.DocumentCategory == category);

                if (!string.IsNullOrWhiteSpace(collectionStatus))
                    q = q.Where(x => x.CollectionStatus == collectionStatus);

                if (bool.TryParse(isReceived, out var rec))
                    q = q.Where(x => x.IsReceived == rec);

                if (int.TryParse(caseFileId, out var cfid) && cfid > 0)
                    q = q.Where(x => x.CaseFileId == cfid);

                if (!string.IsNullOrWhiteSpace(expectedFrom))
                    q = q.Where(x => x.ExpectedFrom != null && x.ExpectedFrom.Contains(expectedFrom));

                var total = await q.CountAsync();

                q = (sort, dir) switch
                {
                    ("DocumentName",     "desc") => q.OrderByDescending(x => x.DocumentName),
                    ("DocumentName",     _)      => q.OrderBy(x => x.DocumentName),
                    ("DocumentCategory", "desc") => q.OrderByDescending(x => x.DocumentCategory),
                    ("DocumentCategory", _)      => q.OrderBy(x => x.DocumentCategory),
                    ("CollectionStatus", "desc") => q.OrderByDescending(x => x.CollectionStatus),
                    ("CollectionStatus", _)      => q.OrderBy(x => x.CollectionStatus),
                    ("FileNumber",       "desc") => q.OrderByDescending(x => x.CaseFile!.FileNumber),
                    ("FileNumber",       _)      => q.OrderBy(x => x.CaseFile!.FileNumber),
                    _                            => q.OrderBy(x => x.CaseFile!.FileNumber)
                };

                var data = await q.Skip(skip).Take(take).ToListAsync();
                return GridResult(data.Select(ProjectDto).ToList(), total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentRead failed");
                return GridError("Failed to load documents.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  CREATE (form POST — handles optional file upload)
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentCreate(CaseDocument model, IFormFile? uploadedFile)
        {
            try
            {
                ModelState.Remove(nameof(CaseDocument.CaseFile));

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    await LoadViewBagsAsync();
                    return View("UploadDocument", model);
                }

                // ── Handle file upload ────────────────────────────────────
                if (uploadedFile != null && uploadedFile.Length > 0)
                {
                    var allowedTypes = new[] { "application/pdf","image/jpeg","image/png",
                                               "image/gif","application/msword",
                                               "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
                    if (!allowedTypes.Contains(uploadedFile.ContentType))
                    {
                        TempData["Error"] = "Only PDF, JPG, PNG, GIF, DOC, and DOCX files are allowed.";
                        await LoadViewBagsAsync();
                        return View("UploadDocument", model);
                    }

                    const long maxBytes = 10 * 1024 * 1024; // 10 MB
                    if (uploadedFile.Length > maxBytes)
                    {
                        TempData["Error"] = "File size must not exceed 10 MB.";
                        await LoadViewBagsAsync();
                        return View("UploadDocument", model);
                    }

                    // Save to wwwroot/uploads/documents/
                    var uploadsFolder = Path.Combine(
                        Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
                    Directory.CreateDirectory(uploadsFolder);

                    var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(uploadedFile.FileName)}";
                    var fullPath     = Path.Combine(uploadsFolder, safeFileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                        await uploadedFile.CopyToAsync(stream);

                    model.DigitalStoragePath = $"/uploads/documents/{safeFileName}";
                    model.OriginalFileName   = uploadedFile.FileName;
                    model.FileMimeType       = uploadedFile.ContentType;
                    model.UploadedAt         = DateTime.UtcNow;
                    model.UploadedBy         = User.Identity?.Name ?? "System";
                    model.IsReceived         = true;
                    model.ReceivedDate       = DateTime.UtcNow;
                    model.CollectionStatus   = "Received";
                }

                _db.CaseDocuments.Add(model);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Document '{model.DocumentName}' added successfully.";
                return RedirectToAction("AllDocuments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentCreate failed");
                TempData["Error"] = "Failed to save document. Please try again.";
                await LoadViewBagsAsync();
                return View("UploadDocument", model);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  UPDATE (form POST)
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentUpdate(CaseDocument model, IFormFile? uploadedFile)
        {
            try
            {
                ModelState.Remove(nameof(CaseDocument.CaseFile));

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    await LoadViewBagsAsync();
                    return View("UploadDocument", model);
                }

                var existing = await _db.CaseDocuments.FindAsync(model.Id);
                if (existing == null) { TempData["Error"] = "Record not found."; return RedirectToAction("AllDocuments"); }

                existing.DocumentName     = model.DocumentName;
                existing.DocumentCategory = model.DocumentCategory;
                existing.ExpectedFrom     = model.ExpectedFrom;
                existing.IsReceived       = model.IsReceived;
                existing.ReceivedDate     = model.ReceivedDate;
                existing.CollectionStatus = model.CollectionStatus;
                existing.Remarks          = model.Remarks;

                // ── Handle replacement file upload ────────────────────────
                if (uploadedFile != null && uploadedFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(
                        Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
                    Directory.CreateDirectory(uploadsFolder);

                    var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(uploadedFile.FileName)}";
                    var fullPath     = Path.Combine(uploadsFolder, safeFileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                        await uploadedFile.CopyToAsync(stream);

                    existing.DigitalStoragePath = $"/uploads/documents/{safeFileName}";
                    existing.OriginalFileName   = uploadedFile.FileName;
                    existing.FileMimeType       = uploadedFile.ContentType;
                    existing.UploadedAt         = DateTime.UtcNow;
                    existing.UploadedBy         = User.Identity?.Name ?? "System";
                }

                await _db.SaveChangesAsync();
                TempData["Success"] = $"Document '{existing.DocumentName}' updated.";
                return RedirectToAction("AllDocuments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentUpdate failed");
                TempData["Error"] = "Failed to update document.";
                await LoadViewBagsAsync();
                return View("UploadDocument", model);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  DESTROY (JSON — from grid)
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> DocumentDestroy(int Id)
        {
            try
            {
                var existing = await _db.CaseDocuments.FindAsync(Id);
                if (existing == null) return GridError("Record not found.");

                // Delete physical file if it exists
                if (!string.IsNullOrWhiteSpace(existing.DigitalStoragePath))
                {
                    var physicalPath = Path.Combine(
                        Directory.GetCurrentDirectory(), "wwwroot",
                        existing.DigitalStoragePath.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                        System.IO.File.Delete(physicalPath);
                }

                _db.CaseDocuments.Remove(existing);
                await _db.SaveChangesAsync();
                return GridResult(Array.Empty<object>(), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentDestroy failed");
                return GridError("Failed to delete document.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  QUICK MARK RECEIVED (AJAX — from Outstanding grid)
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> DocumentMarkReceived(int id)
        {
            try
            {
                var existing = await _db.CaseDocuments
                    .Include(x => x.CaseFile)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (existing == null) return GridError("Record not found.");

                existing.IsReceived       = true;
                existing.ReceivedDate     = DateTime.UtcNow;
                existing.CollectionStatus = "Received";

                await _db.SaveChangesAsync();
                return GridResult(new[] { ProjectDto(existing) }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentMarkReceived failed");
                return GridError("Failed to mark document received.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  SUMMARY STATS (AJAX)
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> DocumentSummaryStats()
        {
            try
            {
                var total       = await _db.CaseDocuments.CountAsync();
                var received    = await _db.CaseDocuments.CountAsync(x => x.IsReceived);
                var outstanding = await _db.CaseDocuments.CountAsync(x =>
                                    !x.IsReceived &&
                                    x.CollectionStatus != "Not Required" &&
                                    x.CollectionStatus != "Received");
                var withFile    = await _db.CaseDocuments.CountAsync(x =>
                                    x.DigitalStoragePath != null && x.DigitalStoragePath != "");
                var notRequired = await _db.CaseDocuments.CountAsync(x =>
                                    x.CollectionStatus == "Not Required");

                return Json(new { total, received, outstanding, withFile, notRequired },
                            new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentSummaryStats failed");
                return GridError("Failed to load stats.");
            }
        }
    }
}
