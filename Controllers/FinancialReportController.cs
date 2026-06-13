using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Data;
using System.Text.Json;
using ClosedXML.Excel;

namespace MVAManagement.Controllers
{
    [Authorize]
    public class FinancialReportController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<FinancialReportController> _logger;

        public FinancialReportController(ApplicationDbContext db, ILogger<FinancialReportController> logger)
        {
            _db     = db;
            _logger = logger;
        }

        // ─── Helpers ──────────────────────────────────────────────────────
        private JsonResult JsonOk(object data) =>
            Json(data, new JsonSerializerOptions { PropertyNamingPolicy = null });

        private JsonResult JsonError(string msg) =>
            Json(new { success = false, message = msg },
                 new JsonSerializerOptions { PropertyNamingPolicy = null });

        private static void StyleHeader(IXLRow row, XLColor bg)
        {
            row.Style.Font.Bold            = true;
            row.Style.Font.FontColor       = XLColor.White;
            row.Style.Fill.BackgroundColor = bg;
            row.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row.Style.Border.BottomBorder  = XLBorderStyleValues.Thin;
        }

        private static void SetHeaders(IXLWorksheet ws, IEnumerable<string> headers)
        {
            int i = 1;
            foreach (var h in headers) ws.Cell(1, i++).Value = h;
        }

        private static void AutoFit(IXLWorksheet ws, int from, int to)
        {
            for (int c = from; c <= to; c++) ws.Column(c).AdjustToContents();
        }

        private static void AlternateRow(IXLRow row, int rowNum)
        {
            if (rowNum % 2 == 0)
                row.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
        }

        private FileContentResult ExcelFile(XLWorkbook wb, string name)
        {
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{name}.xlsx");
        }

        // ══════════════════════════════════════════════════════════════════
        //  R5 — SETTLEMENT SUMMARY
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> SettlementSummary()
        {
            ViewBag.Caseworkers = await _db.CaseworkerProfiles
                .Where(x => x.IsActive).OrderBy(x => x.FullName)
                .Select(x => new { x.Id, x.FullName }).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> SettlementSummaryData(
            string? dateFrom = null, string? dateTo = null,
            string? caseworkerId = null, string? offerStatus = null)
        {
            try
            {
                var q = _db.SettlementOffers
                    .Include(x => x.CaseFile).ThenInclude(c => c.AssignedCaseworker)
                    .Include(x => x.CaseFile).ThenInclude(c => c.CaseStatus)
                    .AsNoTracking().AsQueryable();

                if (DateTime.TryParse(dateFrom, out var df)) q = q.Where(x => x.OfferDate >= df);
                if (DateTime.TryParse(dateTo,   out var dt)) q = q.Where(x => x.OfferDate <= dt);
                if (!string.IsNullOrWhiteSpace(offerStatus))  q = q.Where(x => x.OfferStatus == offerStatus);
                if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                    q = q.Where(x => x.CaseFile != null && x.CaseFile.AssignedCaseworkerId == cwid);

                var offers = await q.OrderByDescending(x => x.OfferDate).ToListAsync();

                // Accepted offers — recovery ratio
                var accepted = offers.Where(x => x.OfferStatus == "Accepted").ToList();
                var totalClaimed  = accepted.Sum(x => x.CaseFile?.ClaimedAmount ?? 0);
                var totalSettled  = accepted.Sum(x => x.OfferAmount);
                var recoveryRatio = totalClaimed > 0 ? (totalSettled / totalClaimed) * 100 : 0;

                var rows = offers.Select(x => new {
                    x.Id,
                    CaseFileId    = x.CaseFileId,
                    FileNumber    = x.CaseFile?.FileNumber ?? "",
                    ClaimantName  = x.CaseFile?.PrimaryClaimantName ?? "",
                    CaseworkerName= x.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned",
                    StatusName    = x.CaseFile?.CaseStatus?.StatusName ?? "",
                    ClaimedAmount = x.CaseFile?.ClaimedAmount ?? 0,
                    x.OfferDate,
                    x.OfferAmount,
                    x.OfferDirection,
                    x.OfferStatus,
                    x.OfferedBy,
                    x.InsurerOfferReference,
                    x.OfferExpiryDate,
                    x.GeneralDamagesComponent,
                    x.SpecialDamagesComponent,
                    RecoveryRatioPct = (x.CaseFile?.ClaimedAmount ?? 0) > 0
                        ? Math.Round((x.OfferAmount / x.CaseFile!.ClaimedAmount) * 100, 1)
                        : (decimal?)null
                }).ToList();

                // By status summary
                var byStatus = offers
                    .GroupBy(x => x.OfferStatus)
                    .Select(g => new {
                        Status      = g.Key,
                        Count       = g.Count(),
                        TotalAmount = g.Sum(x => x.OfferAmount)
                    }).OrderBy(x => x.Status).ToList();

                return JsonOk(new {
                    total          = offers.Count,
                    acceptedCount  = accepted.Count,
                    totalClaimed,
                    totalSettled,
                    recoveryRatio  = Math.Round(recoveryRatio, 1),
                    byStatus,
                    rows
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SettlementSummaryData failed");
                return JsonError("Failed to generate report.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SettlementSummaryExcel(
            string? dateFrom = null, string? dateTo = null,
            string? caseworkerId = null, string? offerStatus = null)
        {
            var q = _db.SettlementOffers
                .Include(x => x.CaseFile).ThenInclude(c => c.AssignedCaseworker)
                .Include(x => x.CaseFile).ThenInclude(c => c.CaseStatus)
                .AsNoTracking().AsQueryable();

            if (DateTime.TryParse(dateFrom, out var df)) q = q.Where(x => x.OfferDate >= df);
            if (DateTime.TryParse(dateTo,   out var dt)) q = q.Where(x => x.OfferDate <= dt);
            if (!string.IsNullOrWhiteSpace(offerStatus))  q = q.Where(x => x.OfferStatus == offerStatus);
            if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                q = q.Where(x => x.CaseFile != null && x.CaseFile.AssignedCaseworkerId == cwid);

            var offers = await q.OrderByDescending(x => x.OfferDate).ToListAsync();
            var hdr    = XLColor.FromHtml("#16a34a");

            using var wb = new XLWorkbook();

            // ── Sheet 1: All Offers ───────────────────────────────────────
            var ws = wb.Worksheets.Add("Settlement Offers");
            StyleHeader(ws.Row(1), hdr);
            SetHeaders(ws, new[] { "File No.","Claimant","Caseworker","Status","Offer Date",
                "Direction","Offer Amount (MYR)","Claimed (MYR)","Recovery %","Offer Status",
                "Offered By","Insurer Ref.","Expiry Date" });
            int row = 2;
            foreach (var o in offers)
            {
                var claimed  = o.CaseFile?.ClaimedAmount ?? 0;
                var recPct   = claimed > 0 ? Math.Round((o.OfferAmount / claimed) * 100, 1) : 0;
                ws.Cell(row, 1).Value  = o.CaseFile?.FileNumber ?? "";
                ws.Cell(row, 2).Value  = o.CaseFile?.PrimaryClaimantName ?? "";
                ws.Cell(row, 3).Value  = o.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned";
                ws.Cell(row, 4).Value  = o.CaseFile?.CaseStatus?.StatusName ?? "";
                ws.Cell(row, 5).Value  = o.OfferDate.ToString("dd/MM/yyyy");
                ws.Cell(row, 6).Value  = o.OfferDirection;
                ws.Cell(row, 7).Value  = o.OfferAmount;
                ws.Cell(row, 8).Value  = claimed;
                ws.Cell(row, 9).Value  = recPct;
                ws.Cell(row, 10).Value = o.OfferStatus;
                ws.Cell(row, 11).Value = o.OfferedBy ?? "";
                ws.Cell(row, 12).Value = o.InsurerOfferReference ?? "";
                ws.Cell(row, 13).Value = o.OfferExpiryDate.HasValue ? o.OfferExpiryDate.Value.ToString("dd/MM/yyyy") : "";
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 9).Style.NumberFormat.Format = "0.0\"%\"";

                // Highlight accepted green, rejected red
                if (o.OfferStatus == "Accepted")
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f0fdf4");
                else if (o.OfferStatus == "Rejected")
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#fef2f2");
                else
                    AlternateRow(ws.Row(row), row);
                row++;
            }
            AutoFit(ws, 1, 13);
            ws.SheetView.FreezeRows(1);

            // ── Sheet 2: Accepted Summary ─────────────────────────────────
            var ws2   = wb.Worksheets.Add("Accepted Summary");
            StyleHeader(ws2.Row(1), hdr);
            SetHeaders(ws2, new[] { "File No.","Claimant","Caseworker","Settlement Date",
                "Claimed (MYR)","Settled Amount (MYR)","Recovery %" });
            int r2 = 2;
            foreach (var o in offers.Where(x => x.OfferStatus == "Accepted"))
            {
                var claimed = o.CaseFile?.ClaimedAmount ?? 0;
                ws2.Cell(r2, 1).Value = o.CaseFile?.FileNumber ?? "";
                ws2.Cell(r2, 2).Value = o.CaseFile?.PrimaryClaimantName ?? "";
                ws2.Cell(r2, 3).Value = o.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned";
                ws2.Cell(r2, 4).Value = o.OfferDate.ToString("dd/MM/yyyy");
                ws2.Cell(r2, 5).Value = claimed;
                ws2.Cell(r2, 6).Value = o.OfferAmount;
                ws2.Cell(r2, 7).Value = claimed > 0 ? Math.Round((o.OfferAmount / claimed) * 100, 1) : 0;
                ws2.Cell(r2, 5).Style.NumberFormat.Format = "#,##0.00";
                ws2.Cell(r2, 6).Style.NumberFormat.Format = "#,##0.00";
                ws2.Cell(r2, 7).Style.NumberFormat.Format = "0.0\"%\"";
                AlternateRow(ws2.Row(r2), r2);
                r2++;
            }
            // Totals row
            if (r2 > 2)
            {
                ws2.Cell(r2, 1).Value = "TOTAL";
                ws2.Cell(r2, 5).Value = offers.Where(x => x.OfferStatus == "Accepted").Sum(x => x.CaseFile?.ClaimedAmount ?? 0);
                ws2.Cell(r2, 6).Value = offers.Where(x => x.OfferStatus == "Accepted").Sum(x => x.OfferAmount);
                ws2.Row(r2).Style.Font.Bold = true;
                ws2.Row(r2).Style.Fill.BackgroundColor = XLColor.FromHtml("#dcfce7");
                ws2.Cell(r2, 5).Style.NumberFormat.Format = "#,##0.00";
                ws2.Cell(r2, 6).Style.NumberFormat.Format = "#,##0.00";
            }
            AutoFit(ws2, 1, 7);
            ws2.SheetView.FreezeRows(1);

            return ExcelFile(wb, $"SettlementSummary_{DateTime.Now:yyyyMMdd}");
        }

        // ══════════════════════════════════════════════════════════════════
        //  R6 — DISBURSEMENTS BY CATEGORY
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> DisbursementsByCategory()
        {
            ViewBag.Categories = await _db.DisbursementCategories
                .Where(x => x.IsActive).OrderBy(x => x.DisplayOrder)
                .Select(x => new { x.Id, x.CategoryName, x.HexColor }).ToListAsync();
            ViewBag.Caseworkers = await _db.CaseworkerProfiles
                .Where(x => x.IsActive).OrderBy(x => x.FullName)
                .Select(x => new { x.Id, x.FullName }).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> DisbursementsByCategoryData(
            string? dateFrom = null, string? dateTo = null, string? caseFileId = null)
        {
            try
            {
                var q = _db.CaseDisbursements
                    .Include(x => x.CaseFile).ThenInclude(c => c.AssignedCaseworker)
                    .AsNoTracking().AsQueryable();

                if (DateTime.TryParse(dateFrom, out var df)) q = q.Where(x => x.DisbursementDate >= df);
                if (DateTime.TryParse(dateTo,   out var dt)) q = q.Where(x => x.DisbursementDate <= dt);
                if (int.TryParse(caseFileId, out var cfid) && cfid > 0)
                    q = q.Where(x => x.CaseFileId == cfid);

                var disbs = await q.ToListAsync();

                // By category
                var byCategory = disbs
                    .GroupBy(x => x.DisbursementCategory)
                    .Select(g => new {
                        Category    = g.Key,
                        Count       = g.Count(),
                        TotalAmount = g.Sum(x => x.Amount),
                        Recovered   = g.Sum(x => x.IsRecovered ? x.Amount : 0),
                        Unrecovered = g.Sum(x => !x.IsRecovered ? x.Amount : 0)
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToList();

                var grandTotal = disbs.Sum(x => x.Amount);

                // Enrich with percentage
                var byCategoryWithPct = byCategory.Select(c => new {
                    c.Category, c.Count, c.TotalAmount, c.Recovered, c.Unrecovered,
                    Pct = grandTotal > 0 ? Math.Round((c.TotalAmount / grandTotal) * 100, 1) : 0
                }).ToList();

                // By month trend
                var byMonth = disbs
                    .GroupBy(x => new { Label = x.DisbursementDate.ToString("MMM yyyy"),
                                        Sort  = x.DisbursementDate.Year * 100 + x.DisbursementDate.Month })
                    .Select(g => new {
                        Period = g.Key.Label, Sort = g.Key.Sort,
                        Total  = g.Sum(x => x.Amount), Count = g.Count()
                    })
                    .OrderBy(x => x.Sort).ToList();

                var rows = disbs.Select(x => new {
                    x.Id,
                    FileNumber    = x.CaseFile?.FileNumber ?? "",
                    ClaimantName  = x.CaseFile?.PrimaryClaimantName ?? "",
                    CaseworkerName= x.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned",
                    x.DisbursementDate,
                    x.DisbursementCategory,
                    x.Description,
                    x.Payee,
                    x.Amount,
                    x.PaymentMethod,
                    x.ReceiptNumber,
                    x.IsRecovered,
                    x.RecoveredDate
                }).OrderByDescending(x => x.DisbursementDate).ToList();

                return JsonOk(new {
                    total        = disbs.Count,
                    grandTotal,
                    totalRecovered   = disbs.Sum(x => x.IsRecovered ? x.Amount : 0),
                    totalUnrecovered = disbs.Sum(x => !x.IsRecovered ? x.Amount : 0),
                    byCategory   = byCategoryWithPct,
                    byMonth,
                    rows
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DisbursementsByCategoryData failed");
                return JsonError("Failed to generate report.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DisbursementsByCategoryExcel(
            string? dateFrom = null, string? dateTo = null, string? caseFileId = null)
        {
            var q = _db.CaseDisbursements
                .Include(x => x.CaseFile).ThenInclude(c => c.AssignedCaseworker)
                .AsNoTracking().AsQueryable();

            if (DateTime.TryParse(dateFrom, out var df)) q = q.Where(x => x.DisbursementDate >= df);
            if (DateTime.TryParse(dateTo,   out var dt)) q = q.Where(x => x.DisbursementDate <= dt);
            if (int.TryParse(caseFileId, out var cfid) && cfid > 0)
                q = q.Where(x => x.CaseFileId == cfid);

            var disbs    = await q.OrderBy(x => x.DisbursementCategory).ThenBy(x => x.DisbursementDate).ToListAsync();
            var hdr      = XLColor.FromHtml("#7c3aed");
            var grandTotal = disbs.Sum(x => x.Amount);

            using var wb = new XLWorkbook();

            // ── Sheet 1: Category Summary ─────────────────────────────────
            var ws = wb.Worksheets.Add("By Category");
            StyleHeader(ws.Row(1), hdr);
            SetHeaders(ws, new[] { "Category","Count","Total (MYR)","Recovered (MYR)","Unrecovered (MYR)","% of Total" });
            int row = 2;
            foreach (var g in disbs.GroupBy(x => x.DisbursementCategory).OrderByDescending(x => x.Sum(d => d.Amount)))
            {
                var total = g.Sum(x => x.Amount);
                ws.Cell(row, 1).Value = g.Key;
                ws.Cell(row, 2).Value = g.Count();
                ws.Cell(row, 3).Value = total;
                ws.Cell(row, 4).Value = g.Where(x => x.IsRecovered).Sum(x => x.Amount);
                ws.Cell(row, 5).Value = g.Where(x => !x.IsRecovered).Sum(x => x.Amount);
                ws.Cell(row, 6).Value = grandTotal > 0 ? Math.Round((total / grandTotal) * 100, 1) : 0;
                for (int c = 3; c <= 5; c++) ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 6).Style.NumberFormat.Format = "0.0\"%\"";
                AlternateRow(ws.Row(row), row);
                row++;
            }
            // Grand total row
            ws.Cell(row, 1).Value = "GRAND TOTAL";
            ws.Cell(row, 2).Value = disbs.Count;
            ws.Cell(row, 3).Value = grandTotal;
            ws.Cell(row, 4).Value = disbs.Where(x => x.IsRecovered).Sum(x => x.Amount);
            ws.Cell(row, 5).Value = disbs.Where(x => !x.IsRecovered).Sum(x => x.Amount);
            ws.Row(row).Style.Font.Bold = true;
            ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f3e8ff");
            for (int c = 3; c <= 5; c++) ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";
            AutoFit(ws, 1, 6);

            // ── Sheet 2: Monthly Trend ────────────────────────────────────
            var ws2 = wb.Worksheets.Add("Monthly Trend");
            StyleHeader(ws2.Row(1), hdr);
            SetHeaders(ws2, new[] { "Period","Count","Total (MYR)" });
            int r2 = 2;
            foreach (var g in disbs.GroupBy(x => x.DisbursementDate.ToString("MMM yyyy"))
                                    .OrderBy(x => { var d = disbs.First(d => d.DisbursementDate.ToString("MMM yyyy") == x.Key).DisbursementDate; return d.Year * 100 + d.Month; }))
            {
                ws2.Cell(r2, 1).Value = g.Key;
                ws2.Cell(r2, 2).Value = g.Count();
                ws2.Cell(r2, 3).Value = g.Sum(x => x.Amount);
                ws2.Cell(r2, 3).Style.NumberFormat.Format = "#,##0.00";
                AlternateRow(ws2.Row(r2), r2);
                r2++;
            }
            AutoFit(ws2, 1, 3);

            // ── Sheet 3: Transaction Detail ───────────────────────────────
            var ws3 = wb.Worksheets.Add("Transaction Detail");
            StyleHeader(ws3.Row(1), hdr);
            SetHeaders(ws3, new[] { "File No.","Claimant","Caseworker","Date","Category",
                "Description","Payee","Amount (MYR)","Payment Method","Receipt No.","Recovered" });
            int r3 = 2;
            foreach (var d in disbs)
            {
                ws3.Cell(r3, 1).Value  = d.CaseFile?.FileNumber ?? "";
                ws3.Cell(r3, 2).Value  = d.CaseFile?.PrimaryClaimantName ?? "";
                ws3.Cell(r3, 3).Value  = d.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned";
                ws3.Cell(r3, 4).Value  = d.DisbursementDate.ToString("dd/MM/yyyy");
                ws3.Cell(r3, 5).Value  = d.DisbursementCategory;
                ws3.Cell(r3, 6).Value  = d.Description;
                ws3.Cell(r3, 7).Value  = d.Payee;
                ws3.Cell(r3, 8).Value  = d.Amount;
                ws3.Cell(r3, 9).Value  = d.PaymentMethod ?? "";
                ws3.Cell(r3, 10).Value = d.ReceiptNumber ?? "";
                ws3.Cell(r3, 11).Value = d.IsRecovered ? "Yes" : "No";
                ws3.Cell(r3, 8).Style.NumberFormat.Format = "#,##0.00";
                if (!d.IsRecovered) ws3.Cell(r3, 11).Style.Font.FontColor = XLColor.FromHtml("#dc2626");
                AlternateRow(ws3.Row(r3), r3);
                r3++;
            }
            AutoFit(ws3, 1, 11);
            ws3.SheetView.FreezeRows(1);

            return ExcelFile(wb, $"DisbursementsByCategory_{DateTime.Now:yyyyMMdd}");
        }

        // ══════════════════════════════════════════════════════════════════
        //  R7 — UNRECOVERED DISBURSEMENTS
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> UnrecoveredDisbursements()
        {
            ViewBag.Caseworkers = await _db.CaseworkerProfiles
                .Where(x => x.IsActive).OrderBy(x => x.FullName)
                .Select(x => new { x.Id, x.FullName }).ToListAsync();
            ViewBag.Categories = await _db.DisbursementCategories
                .Where(x => x.IsActive).OrderBy(x => x.DisplayOrder)
                .Select(x => new { x.Id, x.CategoryName, x.HexColor }).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> UnrecoveredDisbursementsData(
            string? dateFrom = null, string? dateTo = null,
            string? caseworkerId = null, string? category = null)
        {
            try
            {
                var q = _db.CaseDisbursements
                    .Include(x => x.CaseFile).ThenInclude(c => c.AssignedCaseworker)
                    .Include(x => x.CaseFile).ThenInclude(c => c.CaseStatus)
                    .Where(x => !x.IsRecovered)
                    .AsNoTracking().AsQueryable();

                if (DateTime.TryParse(dateFrom, out var df)) q = q.Where(x => x.DisbursementDate >= df);
                if (DateTime.TryParse(dateTo,   out var dt)) q = q.Where(x => x.DisbursementDate <= dt);
                if (!string.IsNullOrWhiteSpace(category))    q = q.Where(x => x.DisbursementCategory == category);
                if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                    q = q.Where(x => x.CaseFile != null && x.CaseFile.AssignedCaseworkerId == cwid);

                var disbs = await q.OrderBy(x => x.CaseFile!.FileNumber).ToListAsync();

                // Group by case file
                var byCaseFile = disbs
                    .GroupBy(x => new {
                        x.CaseFileId,
                        FileNumber   = x.CaseFile?.FileNumber ?? "",
                        ClaimantName = x.CaseFile?.PrimaryClaimantName ?? "",
                        Caseworker   = x.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned",
                        CaseStatus   = x.CaseFile?.CaseStatus?.StatusName ?? ""
                    })
                    .Select(g => new {
                        g.Key.CaseFileId, g.Key.FileNumber, g.Key.ClaimantName,
                        g.Key.Caseworker, g.Key.CaseStatus,
                        Count       = g.Count(),
                        TotalAmount = g.Sum(x => x.Amount),
                        Items       = g.Select(x => new {
                            x.Id, x.DisbursementDate, x.DisbursementCategory,
                            x.Description, x.Amount, x.Payee
                        }).ToList()
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToList();

                var rows = disbs.Select(x => new {
                    x.Id,
                    FileNumber    = x.CaseFile?.FileNumber ?? "",
                    ClaimantName  = x.CaseFile?.PrimaryClaimantName ?? "",
                    CaseworkerName= x.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned",
                    CaseStatus    = x.CaseFile?.CaseStatus?.StatusName ?? "",
                    x.DisbursementDate, x.DisbursementCategory,
                    x.Description, x.Payee, x.Amount, x.ReceiptNumber
                }).ToList();

                return JsonOk(new {
                    total        = disbs.Count,
                    grandTotal   = disbs.Sum(x => x.Amount),
                    caseCount    = byCaseFile.Count,
                    topCategory  = disbs.GroupBy(x => x.DisbursementCategory)
                                        .OrderByDescending(g => g.Sum(x => x.Amount))
                                        .FirstOrDefault()?.Key ?? "—",
                    byCaseFile,
                    rows
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UnrecoveredDisbursementsData failed");
                return JsonError("Failed to generate report.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UnrecoveredDisbursementsExcel(
            string? dateFrom = null, string? dateTo = null,
            string? caseworkerId = null, string? category = null)
        {
            var q = _db.CaseDisbursements
                .Include(x => x.CaseFile).ThenInclude(c => c.AssignedCaseworker)
                .Include(x => x.CaseFile).ThenInclude(c => c.CaseStatus)
                .Where(x => !x.IsRecovered)
                .AsNoTracking().AsQueryable();

            if (DateTime.TryParse(dateFrom, out var df)) q = q.Where(x => x.DisbursementDate >= df);
            if (DateTime.TryParse(dateTo,   out var dt)) q = q.Where(x => x.DisbursementDate <= dt);
            if (!string.IsNullOrWhiteSpace(category))    q = q.Where(x => x.DisbursementCategory == category);
            if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                q = q.Where(x => x.CaseFile != null && x.CaseFile.AssignedCaseworkerId == cwid);

            var disbs = await q.OrderBy(x => x.CaseFile!.FileNumber).ThenBy(x => x.DisbursementDate).ToListAsync();
            var hdr   = XLColor.FromHtml("#dc2626");

            using var wb = new XLWorkbook();

            // ── Sheet 1: By Case File ─────────────────────────────────────
            var ws = wb.Worksheets.Add("By Case File");
            StyleHeader(ws.Row(1), hdr);
            SetHeaders(ws, new[] { "File No.","Claimant","Caseworker","Case Status","Items","Total Unrecovered (MYR)" });
            int row = 2;
            foreach (var g in disbs.GroupBy(x => new {
                FileNumber = x.CaseFile?.FileNumber ?? "",
                Claimant   = x.CaseFile?.PrimaryClaimantName ?? "",
                Caseworker = x.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned",
                Status     = x.CaseFile?.CaseStatus?.StatusName ?? ""
            }).OrderByDescending(g => g.Sum(d => d.Amount)))
            {
                ws.Cell(row, 1).Value = g.Key.FileNumber;
                ws.Cell(row, 2).Value = g.Key.Claimant;
                ws.Cell(row, 3).Value = g.Key.Caseworker;
                ws.Cell(row, 4).Value = g.Key.Status;
                ws.Cell(row, 5).Value = g.Count();
                ws.Cell(row, 6).Value = g.Sum(d => d.Amount);
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                AlternateRow(ws.Row(row), row);
                row++;
            }
            // Grand total
            ws.Cell(row, 1).Value = "GRAND TOTAL";
            ws.Cell(row, 5).Value = disbs.Count;
            ws.Cell(row, 6).Value = disbs.Sum(x => x.Amount);
            ws.Row(row).Style.Font.Bold = true;
            ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#fee2e2");
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            AutoFit(ws, 1, 6);

            // ── Sheet 2: Transaction Detail ───────────────────────────────
            var ws2 = wb.Worksheets.Add("Transaction Detail");
            StyleHeader(ws2.Row(1), hdr);
            SetHeaders(ws2, new[] { "File No.","Claimant","Caseworker","Date","Category",
                "Description","Payee","Amount (MYR)","Receipt No." });
            int r2 = 2;
            foreach (var d in disbs)
            {
                ws2.Cell(r2, 1).Value = d.CaseFile?.FileNumber ?? "";
                ws2.Cell(r2, 2).Value = d.CaseFile?.PrimaryClaimantName ?? "";
                ws2.Cell(r2, 3).Value = d.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned";
                ws2.Cell(r2, 4).Value = d.DisbursementDate.ToString("dd/MM/yyyy");
                ws2.Cell(r2, 5).Value = d.DisbursementCategory;
                ws2.Cell(r2, 6).Value = d.Description;
                ws2.Cell(r2, 7).Value = d.Payee;
                ws2.Cell(r2, 8).Value = d.Amount;
                ws2.Cell(r2, 9).Value = d.ReceiptNumber ?? "";
                ws2.Cell(r2, 8).Style.NumberFormat.Format = "#,##0.00";
                AlternateRow(ws2.Row(r2), r2);
                r2++;
            }
            AutoFit(ws2, 1, 9);
            ws2.SheetView.FreezeRows(1);

            return ExcelFile(wb, $"UnrecoveredDisbursements_{DateTime.Now:yyyyMMdd}");
        }

        // ══════════════════════════════════════════════════════════════════
        //  R8 — OUTSTANDING OFFER REGISTER
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> OutstandingOfferRegister()
        {
            ViewBag.Caseworkers = await _db.CaseworkerProfiles
                .Where(x => x.IsActive).OrderBy(x => x.FullName)
                .Select(x => new { x.Id, x.FullName }).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> OutstandingOfferRegisterData(
            string? caseworkerId = null, string? offerStatus = null)
        {
            try
            {
                // Default: Open + Countered
                var statuses = string.IsNullOrWhiteSpace(offerStatus)
                    ? new[] { "Open", "Countered" }
                    : new[] { offerStatus };

                var q = _db.SettlementOffers
                    .Include(x => x.CaseFile).ThenInclude(c => c.AssignedCaseworker)
                    .Include(x => x.CaseFile).ThenInclude(c => c.CaseStatus)
                    .Where(x => statuses.Contains(x.OfferStatus))
                    .AsNoTracking().AsQueryable();

                if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                    q = q.Where(x => x.CaseFile != null && x.CaseFile.AssignedCaseworkerId == cwid);

                var offers = await q.OrderBy(x => x.OfferExpiryDate).ToListAsync();
                var today  = DateTime.Today;

                var rows = offers.Select(x => new {
                    x.Id,
                    x.CaseFileId,
                    FileNumber    = x.CaseFile?.FileNumber ?? "",
                    ClaimantName  = x.CaseFile?.PrimaryClaimantName ?? "",
                    CaseworkerName= x.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned",
                    CaseStatus    = x.CaseFile?.CaseStatus?.StatusName ?? "",
                    ClaimedAmount = x.CaseFile?.ClaimedAmount ?? 0,
                    x.OfferDate,
                    x.OfferAmount,
                    x.OfferDirection,
                    x.OfferStatus,
                    x.OfferedBy,
                    x.InsurerOfferReference,
                    x.OfferExpiryDate,
                    x.Notes,
                    DaysToExpiry  = x.OfferExpiryDate.HasValue
                        ? (int)(x.OfferExpiryDate.Value - today).TotalDays
                        : (int?)null,
                    IsExpiringSoon = x.OfferExpiryDate.HasValue
                        && (x.OfferExpiryDate.Value - today).TotalDays <= 14
                        && x.OfferExpiryDate.Value >= today,
                    IsExpired = x.OfferExpiryDate.HasValue && x.OfferExpiryDate.Value < today,
                    RecoveryPct = (x.CaseFile?.ClaimedAmount ?? 0) > 0
                        ? Math.Round((x.OfferAmount / x.CaseFile!.ClaimedAmount) * 100, 1)
                        : (decimal?)null
                }).ToList();

                return JsonOk(new {
                    total         = rows.Count,
                    openCount     = rows.Count(x => x.OfferStatus == "Open"),
                    counteredCount= rows.Count(x => x.OfferStatus == "Countered"),
                    expiringSoon  = rows.Count(x => x.IsExpiringSoon),
                    expiredOffers = rows.Count(x => x.IsExpired),
                    totalValue    = rows.Sum(x => x.OfferAmount),
                    rows
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutstandingOfferRegisterData failed");
                return JsonError("Failed to generate report.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> OutstandingOfferRegisterExcel(
            string? caseworkerId = null, string? offerStatus = null)
        {
            var statuses = string.IsNullOrWhiteSpace(offerStatus)
                ? new[] { "Open", "Countered" }
                : new[] { offerStatus };

            var q = _db.SettlementOffers
                .Include(x => x.CaseFile).ThenInclude(c => c.AssignedCaseworker)
                .Include(x => x.CaseFile).ThenInclude(c => c.CaseStatus)
                .Where(x => statuses.Contains(x.OfferStatus))
                .AsNoTracking().AsQueryable();

            if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                q = q.Where(x => x.CaseFile != null && x.CaseFile.AssignedCaseworkerId == cwid);

            var offers = await q.OrderBy(x => x.OfferExpiryDate).ToListAsync();
            var today  = DateTime.Today;
            var hdr    = XLColor.FromHtml("#d97706");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Outstanding Offers");
            StyleHeader(ws.Row(1), hdr);
            SetHeaders(ws, new[] {
                "File No.","Claimant","Caseworker","Case Status",
                "Offer Date","Direction","Offer Amount (MYR)","Claimed (MYR)","Recovery %",
                "Offer Status","Offered By","Insurer Ref.","Expiry Date","Days to Expiry","Notes"
            });

            int row = 2;
            foreach (var o in offers)
            {
                var claimed     = o.CaseFile?.ClaimedAmount ?? 0;
                var daysToExp   = o.OfferExpiryDate.HasValue ? (int)(o.OfferExpiryDate.Value - today).TotalDays : (int?)null;
                var recPct      = claimed > 0 ? Math.Round((o.OfferAmount / claimed) * 100, 1) : 0;

                ws.Cell(row, 1).Value  = o.CaseFile?.FileNumber ?? "";
                ws.Cell(row, 2).Value  = o.CaseFile?.PrimaryClaimantName ?? "";
                ws.Cell(row, 3).Value  = o.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned";
                ws.Cell(row, 4).Value  = o.CaseFile?.CaseStatus?.StatusName ?? "";
                ws.Cell(row, 5).Value  = o.OfferDate.ToString("dd/MM/yyyy");
                ws.Cell(row, 6).Value  = o.OfferDirection;
                ws.Cell(row, 7).Value  = o.OfferAmount;
                ws.Cell(row, 8).Value  = claimed;
                ws.Cell(row, 9).Value  = recPct;
                ws.Cell(row, 10).Value = o.OfferStatus;
                ws.Cell(row, 11).Value = o.OfferedBy ?? "";
                ws.Cell(row, 12).Value = o.InsurerOfferReference ?? "";
                ws.Cell(row, 13).Value = o.OfferExpiryDate.HasValue ? o.OfferExpiryDate.Value.ToString("dd/MM/yyyy") : "—";
                ws.Cell(row, 14).Value = daysToExp.HasValue ? daysToExp.ToString() : "—";
                ws.Cell(row, 15).Value = o.Notes ?? "";
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 9).Style.NumberFormat.Format = "0.0\"%\"";

                // Colour: expiring soon = amber, expired = red
                var rowBg = daysToExp.HasValue && daysToExp < 0  ? XLColor.FromHtml("#fef2f2")
                          : daysToExp.HasValue && daysToExp <= 14 ? XLColor.FromHtml("#fffbeb")
                          : XLColor.NoColor;
                if (rowBg != XLColor.NoColor) ws.Row(row).Style.Fill.BackgroundColor = rowBg;
                else AlternateRow(ws.Row(row), row);
                row++;
            }
            AutoFit(ws, 1, 15);
            ws.SheetView.FreezeRows(1);

            return ExcelFile(wb, $"OutstandingOfferRegister_{DateTime.Now:yyyyMMdd}");
        }
    }
}
