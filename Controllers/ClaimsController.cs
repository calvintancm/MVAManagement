using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Data;
using MVAManagement.Models.MVA;
using System.Text.Json;

namespace MVAManagement.Controllers
{
    [Authorize]
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ClaimsController> _logger;

        public ClaimsController(ApplicationDbContext db, ILogger<ClaimsController> logger)
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

        // ─── Shared dropdown loader ────────────────────────────────────────
        private async Task LoadViewBagsAsync()
        {
            ViewBag.CaseFiles = await _db.CaseFiles
                .Where(x => !x.IsClosed)
                .OrderBy(x => x.FileNumber)
                .Select(x => new { x.Id, x.FileNumber, x.PrimaryClaimantName })
                .ToListAsync();

            ViewBag.DisbursementCategories = await _db.DisbursementCategories
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new { x.Id, x.CategoryName, x.HexColor })
                .ToListAsync();
        }

        // ══════════════════════════════════════════════════════════════════
        //  VIEWS
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> SettlementOffers()
        {
            await LoadViewBagsAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Disbursements()
        {
            await LoadViewBagsAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> NewDisbursement()
        {
            await LoadViewBagsAsync();
            return View(new CaseDisbursement
            {
                DisbursementDate = DateTime.Today,
                DisbursementCategory = "Miscellaneous"
            });
        }

        [HttpGet]
        public async Task<IActionResult> EditDisbursement(int id)
        {
            var record = await _db.CaseDisbursements
                .Include(x => x.CaseFile)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (record == null) return NotFound();
            await LoadViewBagsAsync();
            return View("NewDisbursement", record);
        }

        // ══════════════════════════════════════════════════════════════════
        //  SETTLEMENT OFFERS — READ
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> SettlementOfferRead(
            int skip = 0, int take = 15,
            string? search = null,
            string? offerDirection = null, string? offerStatus = null,
            string? caseFileId = null,
            string? dateFrom = null, string? dateTo = null,
            string? sort = null, string? dir = null)
        {
            try
            {
                var q = _db.SettlementOffers
                    .Include(x => x.CaseFile)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                    q = q.Where(x =>
                        (x.CaseFile != null && x.CaseFile.FileNumber.Contains(search)) ||
                        (x.CaseFile != null && x.CaseFile.PrimaryClaimantName.Contains(search)) ||
                        (x.OfferedBy != null && x.OfferedBy.Contains(search)) ||
                        (x.InsurerOfferReference != null && x.InsurerOfferReference.Contains(search)));

                if (!string.IsNullOrWhiteSpace(offerDirection))
                    q = q.Where(x => x.OfferDirection == offerDirection);

                if (!string.IsNullOrWhiteSpace(offerStatus))
                    q = q.Where(x => x.OfferStatus == offerStatus);

                if (int.TryParse(caseFileId, out var cfid) && cfid > 0)
                    q = q.Where(x => x.CaseFileId == cfid);

                if (DateTime.TryParse(dateFrom, out var df))
                    q = q.Where(x => x.OfferDate >= df);

                if (DateTime.TryParse(dateTo, out var dt))
                    q = q.Where(x => x.OfferDate <= dt);

                var total = await q.CountAsync();

                q = (sort, dir) switch
                {
                    ("OfferDate",   "desc") => q.OrderByDescending(x => x.OfferDate),
                    ("OfferDate",   _)      => q.OrderBy(x => x.OfferDate),
                    ("OfferAmount", "desc") => q.OrderByDescending(x => x.OfferAmount),
                    ("OfferAmount", _)      => q.OrderBy(x => x.OfferAmount),
                    ("FileNumber",  "desc") => q.OrderByDescending(x => x.CaseFile!.FileNumber),
                    ("FileNumber",  _)      => q.OrderBy(x => x.CaseFile!.FileNumber),
                    _                       => q.OrderByDescending(x => x.OfferDate)
                };

                var data = await q.Skip(skip).Take(take).ToListAsync();

                var dto = data.Select(x => new
                {
                    x.Id,
                    x.CaseFileId,
                    FileNumber    = x.CaseFile != null ? x.CaseFile.FileNumber : null,
                    ClaimantName  = x.CaseFile != null ? x.CaseFile.PrimaryClaimantName : null,
                    ClaimedAmount = x.CaseFile != null ? x.CaseFile.ClaimedAmount : (decimal?)null,
                    x.OfferDate,
                    x.OfferAmount,
                    x.OfferDirection,
                    x.OfferStatus,
                    x.OfferedBy,
                    x.InsurerOfferReference,
                    x.OfferExpiryDate,
                    x.GeneralDamagesComponent,
                    x.SpecialDamagesComponent,
                    x.Notes,
                    x.CreatedAt
                }).ToList();

                return GridResult(dto, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SettlementOfferRead failed");
                return GridError("Failed to load settlement offers.");
            }
        }

        // ── Settlement Offer CREATE ────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> SettlementOfferCreate(SettlementOffer model)
        {
            try
            {
                ModelState.Remove(nameof(SettlementOffer.CaseFile));
                if (!ModelState.IsValid)
                    return GridError(string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                model.CreatedAt = DateTime.UtcNow;
                _db.SettlementOffers.Add(model);

                // Sync CurrentOffer on the case file if this is the latest
                var caseFile = await _db.CaseFiles.FindAsync(model.CaseFileId);
                if (caseFile != null)
                {
                    caseFile.CurrentOffer = model.OfferAmount;
                    caseFile.UpdatedAt    = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                var dto = new
                {
                    model.Id, model.CaseFileId,
                    FileNumber   = caseFile?.FileNumber,
                    ClaimantName = caseFile?.PrimaryClaimantName,
                    ClaimedAmount = caseFile?.ClaimedAmount,
                    model.OfferDate, model.OfferAmount, model.OfferDirection,
                    model.OfferStatus, model.OfferedBy, model.InsurerOfferReference,
                    model.OfferExpiryDate, model.GeneralDamagesComponent,
                    model.SpecialDamagesComponent, model.Notes, model.CreatedAt
                };
                return GridResult(new[] { dto }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SettlementOfferCreate failed");
                return GridError("Failed to create settlement offer.");
            }
        }

        // ── Settlement Offer UPDATE ───────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> SettlementOfferUpdate(SettlementOffer model)
        {
            try
            {
                ModelState.Remove(nameof(SettlementOffer.CaseFile));
                var existing = await _db.SettlementOffers.FindAsync(model.Id);
                if (existing == null) return GridError("Record not found.");

                existing.OfferDate                = model.OfferDate;
                existing.OfferAmount              = model.OfferAmount;
                existing.OfferDirection           = model.OfferDirection;
                existing.OfferStatus              = model.OfferStatus;
                existing.OfferedBy                = model.OfferedBy;
                existing.InsurerOfferReference    = model.InsurerOfferReference;
                existing.OfferExpiryDate          = model.OfferExpiryDate;
                existing.GeneralDamagesComponent  = model.GeneralDamagesComponent;
                existing.SpecialDamagesComponent  = model.SpecialDamagesComponent;
                existing.Notes                    = model.Notes;

                await _db.SaveChangesAsync();

                var caseFile = await _db.CaseFiles.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == existing.CaseFileId);

                var dto = new
                {
                    existing.Id, existing.CaseFileId,
                    FileNumber   = caseFile?.FileNumber,
                    ClaimantName = caseFile?.PrimaryClaimantName,
                    ClaimedAmount = caseFile?.ClaimedAmount,
                    existing.OfferDate, existing.OfferAmount, existing.OfferDirection,
                    existing.OfferStatus, existing.OfferedBy, existing.InsurerOfferReference,
                    existing.OfferExpiryDate, existing.GeneralDamagesComponent,
                    existing.SpecialDamagesComponent, existing.Notes, existing.CreatedAt
                };
                return GridResult(new[] { dto }, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SettlementOfferUpdate failed");
                return GridError("Failed to update settlement offer.");
            }
        }

        // ── Settlement Offer DESTROY ──────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> SettlementOfferDestroy(int Id)
        {
            try
            {
                var existing = await _db.SettlementOffers.FindAsync(Id);
                if (existing == null) return GridError("Record not found.");

                if (existing.OfferStatus == "Accepted")
                    return GridError("Cannot delete an accepted offer — it is part of the settlement record.");

                _db.SettlementOffers.Remove(existing);
                await _db.SaveChangesAsync();
                return GridResult(Array.Empty<object>(), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SettlementOfferDestroy failed");
                return GridError("Failed to delete settlement offer.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  DISBURSEMENTS — READ
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> DisbursementRead(
            int skip = 0, int take = 15,
            string? search = null,
            string? category = null, string? isRecovered = null,
            string? caseFileId = null,
            string? dateFrom = null, string? dateTo = null,
            string? sort = null, string? dir = null)
        {
            try
            {
                var q = _db.CaseDisbursements
                    .Include(x => x.CaseFile)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                    q = q.Where(x =>
                        (x.CaseFile != null && x.CaseFile.FileNumber.Contains(search)) ||
                        (x.CaseFile != null && x.CaseFile.PrimaryClaimantName.Contains(search)) ||
                        x.Description.Contains(search) ||
                        x.Payee.Contains(search) ||
                        (x.ReceiptNumber != null && x.ReceiptNumber.Contains(search)));

                if (!string.IsNullOrWhiteSpace(category))
                    q = q.Where(x => x.DisbursementCategory == category);

                if (bool.TryParse(isRecovered, out var rec))
                    q = q.Where(x => x.IsRecovered == rec);

                if (int.TryParse(caseFileId, out var cfid) && cfid > 0)
                    q = q.Where(x => x.CaseFileId == cfid);

                if (DateTime.TryParse(dateFrom, out var df))
                    q = q.Where(x => x.DisbursementDate >= df);

                if (DateTime.TryParse(dateTo, out var dt))
                    q = q.Where(x => x.DisbursementDate <= dt);

                var total = await q.CountAsync();

                q = (sort, dir) switch
                {
                    ("DisbursementDate", "desc") => q.OrderByDescending(x => x.DisbursementDate),
                    ("DisbursementDate", _)      => q.OrderBy(x => x.DisbursementDate),
                    ("Amount",          "desc") => q.OrderByDescending(x => x.Amount),
                    ("Amount",          _)      => q.OrderBy(x => x.Amount),
                    ("FileNumber",      "desc") => q.OrderByDescending(x => x.CaseFile!.FileNumber),
                    ("FileNumber",      _)      => q.OrderBy(x => x.CaseFile!.FileNumber),
                    _                           => q.OrderByDescending(x => x.DisbursementDate)
                };

                var data = await q.Skip(skip).Take(take).ToListAsync();

                var dto = data.Select(x => new
                {
                    x.Id,
                    x.CaseFileId,
                    FileNumber   = x.CaseFile != null ? x.CaseFile.FileNumber : null,
                    ClaimantName = x.CaseFile != null ? x.CaseFile.PrimaryClaimantName : null,
                    x.DisbursementDate,
                    x.DisbursementCategory,
                    x.Description,
                    x.Payee,
                    x.Amount,
                    x.PaymentMethod,
                    x.PaymentReference,
                    x.ReceiptNumber,
                    x.IsRecovered,
                    x.RecoveredDate,
                    x.RecordedBy,
                    x.CreatedAt
                }).ToList();

                return GridResult(dto, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DisbursementRead failed");
                return GridError("Failed to load disbursements.");
            }
        }

        // ── Disbursement CREATE (form POST) ───────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DisbursementCreate(CaseDisbursement model)
        {
            try
            {
                ModelState.Remove(nameof(CaseDisbursement.CaseFile));
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    await LoadViewBagsAsync();
                    return View("NewDisbursement", model);
                }

                model.CreatedAt = DateTime.UtcNow;
                _db.CaseDisbursements.Add(model);

                // Keep CaseFile.TotalDisbursementAmount in sync
                var caseFile = await _db.CaseFiles.FindAsync(model.CaseFileId);
                if (caseFile != null)
                {
                    caseFile.TotalDisbursementAmount += model.Amount;
                    caseFile.UpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                TempData["Success"] = $"Disbursement of RM {model.Amount:N2} recorded successfully.";
                return RedirectToAction("Disbursements");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DisbursementCreate failed");
                TempData["Error"] = "Failed to record disbursement. Please try again.";
                await LoadViewBagsAsync();
                return View("NewDisbursement", model);
            }
        }

        // ── Disbursement UPDATE (form POST) ───────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DisbursementUpdate(CaseDisbursement model)
        {
            try
            {
                ModelState.Remove(nameof(CaseDisbursement.CaseFile));
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    await LoadViewBagsAsync();
                    return View("NewDisbursement", model);
                }

                var existing = await _db.CaseDisbursements.FindAsync(model.Id);
                if (existing == null) { TempData["Error"] = "Record not found."; return RedirectToAction("Disbursements"); }

                // Recalculate case total: subtract old, add new
                if (existing.CaseFileId == model.CaseFileId)
                {
                    var caseFile = await _db.CaseFiles.FindAsync(existing.CaseFileId);
                    if (caseFile != null)
                    {
                        caseFile.TotalDisbursementAmount =
                            caseFile.TotalDisbursementAmount - existing.Amount + model.Amount;
                        caseFile.UpdatedAt = DateTime.UtcNow;
                    }
                }

                existing.DisbursementDate     = model.DisbursementDate;
                existing.DisbursementCategory = model.DisbursementCategory;
                existing.Description          = model.Description;
                existing.Payee                = model.Payee;
                existing.Amount               = model.Amount;
                existing.PaymentMethod        = model.PaymentMethod;
                existing.PaymentReference     = model.PaymentReference;
                existing.ReceiptNumber        = model.ReceiptNumber;
                existing.IsRecovered          = model.IsRecovered;
                existing.RecoveredDate        = model.RecoveredDate;
                existing.RecordedBy           = model.RecordedBy;

                await _db.SaveChangesAsync();
                TempData["Success"] = "Disbursement updated successfully.";
                return RedirectToAction("Disbursements");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DisbursementUpdate failed");
                TempData["Error"] = "Failed to update disbursement.";
                await LoadViewBagsAsync();
                return View("NewDisbursement", model);
            }
        }

        // ── Disbursement DESTROY (JSON) ───────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> DisbursementDestroy(int Id)
        {
            try
            {
                var existing = await _db.CaseDisbursements.FindAsync(Id);
                if (existing == null) return GridError("Record not found.");

                if (existing.IsRecovered)
                    return GridError("Cannot delete a recovered disbursement — it is part of the financial audit record.");

                // Subtract from case total
                var caseFile = await _db.CaseFiles.FindAsync(existing.CaseFileId);
                if (caseFile != null)
                {
                    caseFile.TotalDisbursementAmount =
                        Math.Max(0, caseFile.TotalDisbursementAmount - existing.Amount);
                    caseFile.UpdatedAt = DateTime.UtcNow;
                }

                _db.CaseDisbursements.Remove(existing);
                await _db.SaveChangesAsync();
                return GridResult(Array.Empty<object>(), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DisbursementDestroy failed");
                return GridError("Failed to delete disbursement.");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  SUMMARY STATS (AJAX)
        // ══════════════════════════════════════════════════════════════════

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> ClaimsSummaryStats()
        {
            try
            {
                var totalOffers      = await _db.SettlementOffers.CountAsync();
                var openOffers       = await _db.SettlementOffers.CountAsync(x => x.OfferStatus == "Open");
                var acceptedOffers   = await _db.SettlementOffers.CountAsync(x => x.OfferStatus == "Accepted");
                var totalDisb        = await _db.CaseDisbursements.SumAsync(x => (decimal?)x.Amount) ?? 0m;
                var unrecoveredDisb  = await _db.CaseDisbursements
                    .Where(x => !x.IsRecovered)
                    .SumAsync(x => (decimal?)x.Amount) ?? 0m;
                var disbCount        = await _db.CaseDisbursements.CountAsync();

                return Json(new
                {
                    totalOffers, openOffers, acceptedOffers,
                    totalDisb, unrecoveredDisb, disbCount
                }, new JsonSerializerOptions { PropertyNamingPolicy = null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClaimsSummaryStats failed");
                return GridError("Failed to load stats.");
            }
        }
    }
}
