using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Data;
using MVAManagement.Models.MVA;
using System.Text.Json;
using ClosedXML.Excel;

namespace MVAManagement.Controllers
{
    [Authorize]
    public class CaseReportController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CaseReportController> _logger;

        public CaseReportController(ApplicationDbContext db, ILogger<CaseReportController> logger)
        {
            _db     = db;
            _logger = logger;
        }

        // ─── JSON helper ───────────────────────────────────────────────────
        private JsonResult JsonOk(object data) =>
            Json(data, new JsonSerializerOptions { PropertyNamingPolicy = null });

        private JsonResult JsonError(string msg) =>
            Json(new { success = false, message = msg },
                 new JsonSerializerOptions { PropertyNamingPolicy = null });

        // ─── Excel helper: apply standard header row style ─────────────────
        private static void StyleHeaderRow(IXLRow row, XLColor bgColor)
        {
            row.Style.Font.Bold      = true;
            row.Style.Font.FontColor = XLColor.White;
            row.Style.Fill.BackgroundColor = bgColor;
            row.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row.Style.Border.BottomBorder  = XLBorderStyleValues.Thin;
        }

        private static void AutoFitColumns(IXLWorksheet ws, int fromCol, int toCol)
        {
            for (int c = fromCol; c <= toCol; c++)
                ws.Column(c).AdjustToContents();
        }

        // ══════════════════════════════════════════════════════════════════
        //  REPORT 1 — CASE STATUS SUMMARY
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> CaseStatusSummary()
        {
            ViewBag.CaseStatuses  = await _db.CaseStatuses.OrderBy(x => x.DisplayOrder).ToListAsync();
            ViewBag.Caseworkers   = await _db.CaseworkerProfiles.Where(x => x.IsActive)
                                        .OrderBy(x => x.FullName).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseStatusSummaryData(
            string? dateFrom = null, string? dateTo = null,
            string? caseworkerId = null, string? statusId = null)
        {
            try
            {
                var q = _db.CaseFiles
                    .Include(x => x.CaseStatus)
                    .Include(x => x.AssignedCaseworker)
                    .AsNoTracking().AsQueryable();

                if (DateTime.TryParse(dateFrom, out var df)) q = q.Where(x => x.CreatedAt >= df);
                if (DateTime.TryParse(dateTo,   out var dt)) q = q.Where(x => x.CreatedAt <= dt);
                if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                    q = q.Where(x => x.AssignedCaseworkerId == cwid);
                if (int.TryParse(statusId, out var sid) && sid > 0)
                    q = q.Where(x => x.CaseStatusId == sid);

                var cases = await q.ToListAsync();

                // Summary by status
                var byStatus = cases
                    .GroupBy(x => new { x.CaseStatusId, Name = x.CaseStatus?.StatusName ?? "Unknown", Code = x.CaseStatus?.StatusCode ?? "" })
                    .Select(g => new {
                        StatusId   = g.Key.CaseStatusId,
                        StatusName = g.Key.Name,
                        StatusCode = g.Key.Code,
                        Count      = g.Count(),
                        InLitigation = g.Count(x => x.IsInLitigation),
                        TotalClaimed = g.Sum(x => x.ClaimedAmount),
                        TotalOffers  = g.Sum(x => x.CurrentOffer)
                    })
                    .OrderBy(x => x.StatusName)
                    .ToList();

                // Summary by caseworker
                var byCaseworker = cases
                    .GroupBy(x => new { x.AssignedCaseworkerId, Name = x.AssignedCaseworker?.FullName ?? "Unassigned" })
                    .Select(g => new {
                        CaseworkerName = g.Key.Name,
                        Count          = g.Count(),
                        Active         = g.Count(x => x.IsActive && !x.IsClosed),
                        InLitigation   = g.Count(x => x.IsInLitigation),
                        Closed         = g.Count(x => x.IsClosed)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                return JsonOk(new {
                    total        = cases.Count,
                    active       = cases.Count(x => x.IsActive && !x.IsClosed),
                    inLitigation = cases.Count(x => x.IsInLitigation),
                    closed       = cases.Count(x => x.IsClosed),
                    totalClaimed = cases.Sum(x => x.ClaimedAmount),
                    byStatus,
                    byCaseworker
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseStatusSummaryData failed");
                return JsonError("Failed to generate report.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CaseStatusSummaryExcel(
            string? dateFrom = null, string? dateTo = null,
            string? caseworkerId = null, string? statusId = null)
        {
            var q = _db.CaseFiles
                .Include(x => x.CaseStatus)
                .Include(x => x.AssignedCaseworker)
                .AsNoTracking().AsQueryable();

            if (DateTime.TryParse(dateFrom, out var df)) q = q.Where(x => x.CreatedAt >= df);
            if (DateTime.TryParse(dateTo,   out var dt)) q = q.Where(x => x.CreatedAt <= dt);
            if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                q = q.Where(x => x.AssignedCaseworkerId == cwid);
            if (int.TryParse(statusId, out var sid) && sid > 0)
                q = q.Where(x => x.CaseStatusId == sid);

            var cases = await q.OrderBy(x => x.CaseStatus!.DisplayOrder).ThenBy(x => x.FileNumber).ToListAsync();

            using var wb  = new XLWorkbook();
            var hdrColor  = XLColor.FromHtml("#1d6fa4");

            // ── Sheet 1: Detail ───────────────────────────────────────────
            var ws = wb.Worksheets.Add("Case Detail");
            StyleHeaderRow(ws.Row(1), hdrColor);
            var headers = new[] { "File Number","Court Case No.","Claimant","Status","Caseworker",
                                   "Accident Date","Claimed (MYR)","Current Offer (MYR)","In Litigation","Active","Opened On" };
            for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

            int row = 2;
            foreach (var c in cases)
            {
                ws.Cell(row, 1).Value  = c.FileNumber;
                ws.Cell(row, 2).Value  = c.CourtCaseNumber ?? "";
                ws.Cell(row, 3).Value  = c.PrimaryClaimantName;
                ws.Cell(row, 4).Value  = c.CaseStatus?.StatusName ?? "";
                ws.Cell(row, 5).Value  = c.AssignedCaseworker?.FullName ?? "Unassigned";
                ws.Cell(row, 6).Value  = c.AccidentDate.HasValue ? c.AccidentDate.Value.ToString("dd/MM/yyyy") : "";
                ws.Cell(row, 7).Value  = c.ClaimedAmount;
                ws.Cell(row, 8).Value  = c.CurrentOffer;
                ws.Cell(row, 9).Value  = c.IsInLitigation ? "Yes" : "No";
                ws.Cell(row, 10).Value = c.IsActive ? "Yes" : "No";
                ws.Cell(row, 11).Value = c.CreatedAt.ToString("dd/MM/yyyy");
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                if (row % 2 == 0) ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
                row++;
            }
            AutoFitColumns(ws, 1, 11);
            ws.SheetView.FreezeRows(1);

            // ── Sheet 2: By Status ────────────────────────────────────────
            var ws2 = wb.Worksheets.Add("By Status");
            StyleHeaderRow(ws2.Row(1), hdrColor);
            new[] { "Status","Code","Count","In Litigation","Total Claimed (MYR)","Total Offer (MYR)" }
                .Select((h, i) => new { h, i }).ToList()
                .ForEach(x => ws2.Cell(1, x.i + 1).Value = x.h);
            var byStatus = cases
                .GroupBy(x => new { Name = x.CaseStatus?.StatusName ?? "Unknown", Code = x.CaseStatus?.StatusCode ?? "" })
                .OrderBy(x => x.Key.Name).ToList();
            int r2 = 2;
            foreach (var g in byStatus)
            {
                ws2.Cell(r2, 1).Value = g.Key.Name;
                ws2.Cell(r2, 2).Value = g.Key.Code;
                ws2.Cell(r2, 3).Value = g.Count();
                ws2.Cell(r2, 4).Value = g.Count(x => x.IsInLitigation);
                ws2.Cell(r2, 5).Value = g.Sum(x => x.ClaimedAmount);
                ws2.Cell(r2, 6).Value = g.Sum(x => x.CurrentOffer);
                ws2.Cell(r2, 5).Style.NumberFormat.Format = "#,##0.00";
                ws2.Cell(r2, 6).Style.NumberFormat.Format = "#,##0.00";
                r2++;
            }
            AutoFitColumns(ws2, 1, 6);

            // ── Sheet 3: By Caseworker ────────────────────────────────────
            var ws3 = wb.Worksheets.Add("By Caseworker");
            StyleHeaderRow(ws3.Row(1), hdrColor);
            new[] { "Caseworker","Total Cases","Active","In Litigation","Closed" }
                .Select((h, i) => new { h, i }).ToList()
                .ForEach(x => ws3.Cell(1, x.i + 1).Value = x.h);
            var byCw = cases
                .GroupBy(x => x.AssignedCaseworker?.FullName ?? "Unassigned")
                .OrderByDescending(x => x.Count()).ToList();
            int r3 = 2;
            foreach (var g in byCw)
            {
                ws3.Cell(r3, 1).Value = g.Key;
                ws3.Cell(r3, 2).Value = g.Count();
                ws3.Cell(r3, 3).Value = g.Count(x => x.IsActive && !x.IsClosed);
                ws3.Cell(r3, 4).Value = g.Count(x => x.IsInLitigation);
                ws3.Cell(r3, 5).Value = g.Count(x => x.IsClosed);
                r3++;
            }
            AutoFitColumns(ws3, 1, 5);

            return ExcelFile(wb, $"CaseStatusSummary_{DateTime.Now:yyyyMMdd}");
        }

        // ══════════════════════════════════════════════════════════════════
        //  REPORT 2 — CASE AGE / LIMITATION ALERT
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> CaseAge()
        {
            ViewBag.Caseworkers = await _db.CaseworkerProfiles.Where(x => x.IsActive)
                                      .OrderBy(x => x.FullName).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseAgeData(
            string? caseworkerId = null, string? alertFilter = null)
        {
            try
            {
                var q = _db.CaseFiles
                    .Include(x => x.CaseStatus)
                    .Include(x => x.AssignedCaseworker)
                    .Where(x => x.IsActive && !x.IsClosed)
                    .AsNoTracking().AsQueryable();

                if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                    q = q.Where(x => x.AssignedCaseworkerId == cwid);

                var cases = await q.OrderBy(x => x.AccidentDate).ToListAsync();
                var today = DateTime.Today;

                var rows = cases.Select(c =>
                {
                    var limitationDeadline = c.AccidentDate?.AddYears(6);
                    var daysToLimit = limitationDeadline.HasValue
                        ? (int)(limitationDeadline.Value - today).TotalDays : (int?)null;
                    var ageInDays = (int)(today - c.CreatedAt.Date).TotalDays;

                    return new {
                        c.Id,
                        c.FileNumber,
                        c.PrimaryClaimantName,
                        StatusName         = c.CaseStatus?.StatusName ?? "",
                        CaseworkerName     = c.AssignedCaseworker?.FullName ?? "Unassigned",
                        AccidentDate       = c.AccidentDate,
                        OpenedDate         = c.CreatedAt,
                        AgeInDays          = ageInDays,
                        LimitationDeadline = limitationDeadline,
                        DaysToLimitation   = daysToLimit,
                        IsExpired          = daysToLimit.HasValue && daysToLimit < 0 && !c.IsInLitigation,
                        IsUrgent           = daysToLimit.HasValue && daysToLimit >= 0 && daysToLimit <= 180,
                        IsWarning          = daysToLimit.HasValue && daysToLimit > 180 && daysToLimit <= 365,
                        c.IsInLitigation
                    };
                }).ToList();

                // Apply alert filter
                var filtered = alertFilter switch
                {
                    "expired" => rows.Where(x => x.IsExpired).ToList(),
                    "urgent"  => rows.Where(x => x.IsUrgent).ToList(),
                    "warning" => rows.Where(x => x.IsWarning).ToList(),
                    _         => rows
                };

                return JsonOk(new {
                    total   = filtered.Count,
                    expired = rows.Count(x => x.IsExpired),
                    urgent  = rows.Count(x => x.IsUrgent),
                    warning = rows.Count(x => x.IsWarning),
                    safe    = rows.Count(x => !x.IsExpired && !x.IsUrgent && !x.IsWarning),
                    rows    = filtered
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseAgeData failed");
                return JsonError("Failed to generate report.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CaseAgeExcel(
            string? caseworkerId = null, string? alertFilter = null)
        {
            var q = _db.CaseFiles
                .Include(x => x.CaseStatus)
                .Include(x => x.AssignedCaseworker)
                .Where(x => x.IsActive && !x.IsClosed)
                .AsNoTracking().AsQueryable();

            if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                q = q.Where(x => x.AssignedCaseworkerId == cwid);

            var cases = await q.OrderBy(x => x.AccidentDate).ToListAsync();
            var today = DateTime.Today;

            using var wb = new XLWorkbook();
            var ws       = wb.Worksheets.Add("Case Age Report");
            StyleHeaderRow(ws.Row(1), XLColor.FromHtml("#dc2626"));

            var headers = new[] { "File Number","Claimant","Status","Caseworker",
                                   "Accident Date","Opened On","Age (Days)",
                                   "Limitation Deadline","Days to Limit","Alert Level","In Litigation" };
            for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

            int row = 2;
            foreach (var c in cases)
            {
                var deadline  = c.AccidentDate?.AddYears(6);
                var daysToLim = deadline.HasValue ? (int)(deadline.Value - today).TotalDays : (int?)null;
                var ageDays   = (int)(today - c.CreatedAt.Date).TotalDays;
                var alert     = daysToLim.HasValue && daysToLim < 0 && !c.IsInLitigation ? "EXPIRED"
                              : daysToLim.HasValue && daysToLim <= 180 ? "URGENT"
                              : daysToLim.HasValue && daysToLim <= 365 ? "WARNING" : "OK";

                if (alertFilter == "expired" && alert != "EXPIRED") continue;
                if (alertFilter == "urgent"  && alert != "URGENT")  continue;
                if (alertFilter == "warning" && alert != "WARNING") continue;

                ws.Cell(row, 1).Value  = c.FileNumber;
                ws.Cell(row, 2).Value  = c.PrimaryClaimantName;
                ws.Cell(row, 3).Value  = c.CaseStatus?.StatusName ?? "";
                ws.Cell(row, 4).Value  = c.AssignedCaseworker?.FullName ?? "Unassigned";
                ws.Cell(row, 5).Value  = c.AccidentDate.HasValue ? c.AccidentDate.Value.ToString("dd/MM/yyyy") : "—";
                ws.Cell(row, 6).Value  = c.CreatedAt.ToString("dd/MM/yyyy");
                ws.Cell(row, 7).Value  = ageDays;
                ws.Cell(row, 8).Value  = deadline.HasValue ? deadline.Value.ToString("dd/MM/yyyy") : "—";
                ws.Cell(row, 9).Value  = daysToLim.HasValue ? daysToLim.ToString() : "—";
                ws.Cell(row, 10).Value = alert;
                ws.Cell(row, 11).Value = c.IsInLitigation ? "Yes" : "No";

                var alertColor = alert == "EXPIRED" ? XLColor.FromHtml("#fee2e2")
                               : alert == "URGENT"  ? XLColor.FromHtml("#fef3c7")
                               : alert == "WARNING" ? XLColor.FromHtml("#fefce8")
                               : XLColor.NoColor;
                if (alertColor != XLColor.NoColor)
                    ws.Row(row).Style.Fill.BackgroundColor = alertColor;
                else if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");

                row++;
            }
            AutoFitColumns(ws, 1, 11);
            ws.SheetView.FreezeRows(1);

            return ExcelFile(wb, $"CaseAgeReport_{DateTime.Now:yyyyMMdd}");
        }

        // ══════════════════════════════════════════════════════════════════
        //  REPORT 3 — CASEWORKER WORKLOAD
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> CaseworkerWorkload()
        {
            ViewBag.Caseworkers = await _db.CaseworkerProfiles.Where(x => x.IsActive)
                                      .OrderBy(x => x.FullName).ToListAsync();
            ViewBag.CaseStatuses = await _db.CaseStatuses.OrderBy(x => x.DisplayOrder).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> CaseworkerWorkloadData(string? caseworkerId = null)
        {
            try
            {
                var q = _db.CaseFiles
                    .Include(x => x.CaseStatus)
                    .Include(x => x.AssignedCaseworker)
                    .Where(x => !x.IsClosed)
                    .AsNoTracking().AsQueryable();

                if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                    q = q.Where(x => x.AssignedCaseworkerId == cwid);

                var cases = await q.ToListAsync();

                var summary = cases
                    .GroupBy(x => new {
                        Id   = x.AssignedCaseworkerId ?? 0,
                        Name = x.AssignedCaseworker?.FullName ?? "Unassigned",
                        Role = x.AssignedCaseworker?.JobRole  ?? ""
                    })
                    .Select(g => new {
                        CaseworkerId   = g.Key.Id,
                        CaseworkerName = g.Key.Name,
                        JobRole        = g.Key.Role,
                        Total          = g.Count(),
                        Active         = g.Count(x => x.IsActive),
                        InLitigation   = g.Count(x => x.IsInLitigation),
                        ByStatus       = g.GroupBy(x => x.CaseStatus?.StatusName ?? "Unknown")
                                          .Select(s => new { Status = s.Key, Count = s.Count() })
                                          .OrderBy(s => s.Status).ToList(),
                        TotalClaimed   = g.Sum(x => x.ClaimedAmount),
                        HearingsDue    = g.Count(x => x.NextHearingDate.HasValue
                                             && x.NextHearingDate.Value >= DateTime.Today
                                             && x.NextHearingDate.Value <= DateTime.Today.AddDays(7))
                    })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                return JsonOk(new {
                    totalCaseworkers = summary.Count,
                    totalCases       = cases.Count,
                    unassigned       = cases.Count(x => x.AssignedCaseworkerId == null),
                    summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CaseworkerWorkloadData failed");
                return JsonError("Failed to generate report.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CaseworkerWorkloadExcel(string? caseworkerId = null)
        {
            var q = _db.CaseFiles
                .Include(x => x.CaseStatus)
                .Include(x => x.AssignedCaseworker)
                .Where(x => !x.IsClosed)
                .AsNoTracking().AsQueryable();

            if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                q = q.Where(x => x.AssignedCaseworkerId == cwid);

            var cases = await q.OrderBy(x => x.AssignedCaseworker!.FullName)
                                .ThenBy(x => x.FileNumber).ToListAsync();

            using var wb = new XLWorkbook();
            var hdrColor  = XLColor.FromHtml("#1d6fa4");

            // ── Sheet 1: Workload Summary ─────────────────────────────────
            var ws = wb.Worksheets.Add("Workload Summary");
            StyleHeaderRow(ws.Row(1), hdrColor);
            new[] { "Caseworker","Job Role","Total Cases","Active","In Litigation","Claimed Total (MYR)","Hearings This Week" }
                .Select((h, i) => new { h, i }).ToList()
                .ForEach(x => ws.Cell(1, x.i + 1).Value = x.h);

            var grouped = cases.GroupBy(x => new {
                Name = x.AssignedCaseworker?.FullName ?? "Unassigned",
                Role = x.AssignedCaseworker?.JobRole  ?? ""
            }).OrderByDescending(g => g.Count()).ToList();

            int row = 2;
            foreach (var g in grouped)
            {
                ws.Cell(row, 1).Value = g.Key.Name;
                ws.Cell(row, 2).Value = g.Key.Role;
                ws.Cell(row, 3).Value = g.Count();
                ws.Cell(row, 4).Value = g.Count(x => x.IsActive);
                ws.Cell(row, 5).Value = g.Count(x => x.IsInLitigation);
                ws.Cell(row, 6).Value = g.Sum(x => x.ClaimedAmount);
                ws.Cell(row, 7).Value = g.Count(x => x.NextHearingDate.HasValue
                    && x.NextHearingDate >= DateTime.Today
                    && x.NextHearingDate <= DateTime.Today.AddDays(7));
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                if (row % 2 == 0) ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
                row++;
            }
            AutoFitColumns(ws, 1, 7);
            ws.SheetView.FreezeRows(1);

            // ── Sheet 2: Case Detail ──────────────────────────────────────
            var ws2 = wb.Worksheets.Add("Case Detail");
            StyleHeaderRow(ws2.Row(1), hdrColor);
            new[] { "Caseworker","File Number","Claimant","Status","In Litigation","Accident Date","Claimed (MYR)","Next Hearing" }
                .Select((h, i) => new { h, i }).ToList()
                .ForEach(x => ws2.Cell(1, x.i + 1).Value = x.h);

            int r2 = 2;
            foreach (var c in cases)
            {
                ws2.Cell(r2, 1).Value = c.AssignedCaseworker?.FullName ?? "Unassigned";
                ws2.Cell(r2, 2).Value = c.FileNumber;
                ws2.Cell(r2, 3).Value = c.PrimaryClaimantName;
                ws2.Cell(r2, 4).Value = c.CaseStatus?.StatusName ?? "";
                ws2.Cell(r2, 5).Value = c.IsInLitigation ? "Yes" : "No";
                ws2.Cell(r2, 6).Value = c.AccidentDate.HasValue ? c.AccidentDate.Value.ToString("dd/MM/yyyy") : "";
                ws2.Cell(r2, 7).Value = c.ClaimedAmount;
                ws2.Cell(r2, 8).Value = c.NextHearingDate.HasValue ? c.NextHearingDate.Value.ToString("dd/MM/yyyy") : "";
                ws2.Cell(r2, 7).Style.NumberFormat.Format = "#,##0.00";
                if (r2 % 2 == 0) ws2.Row(r2).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
                r2++;
            }
            AutoFitColumns(ws2, 1, 8);
            ws2.SheetView.FreezeRows(1);

            return ExcelFile(wb, $"CaseworkerWorkload_{DateTime.Now:yyyyMMdd}");
        }

        // ══════════════════════════════════════════════════════════════════
        //  REPORT 4 — NEW CASES BY PERIOD
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> NewCasesByPeriod()
        {
            ViewBag.Caseworkers = await _db.CaseworkerProfiles.Where(x => x.IsActive)
                                      .OrderBy(x => x.FullName).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> NewCasesByPeriodData(
            string? dateFrom = null, string? dateTo = null,
            string? groupBy = "month", string? caseworkerId = null)
        {
            try
            {
                var q = _db.CaseFiles
                    .Include(x => x.CaseStatus)
                    .Include(x => x.AssignedCaseworker)
                    .AsNoTracking().AsQueryable();

                if (DateTime.TryParse(dateFrom, out var df)) q = q.Where(x => x.CreatedAt >= df);
                if (DateTime.TryParse(dateTo,   out var dt)) q = q.Where(x => x.CreatedAt <= dt);
                if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                    q = q.Where(x => x.AssignedCaseworkerId == cwid);

                var cases = await q.OrderBy(x => x.CreatedAt).ToListAsync();

                // Group by selected period
                var grouped = groupBy switch
                {
                    "quarter" => cases
                        .GroupBy(x => new {
                            Label = $"Q{(x.CreatedAt.Month - 1) / 3 + 1} {x.CreatedAt.Year}",
                            Sort  = x.CreatedAt.Year * 10 + (x.CreatedAt.Month - 1) / 3
                        }),
                    "year" => cases
                        .GroupBy(x => new { Label = x.CreatedAt.Year.ToString(), Sort = x.CreatedAt.Year }),
                    _ => cases  // month (default)
                        .GroupBy(x => new { Label = x.CreatedAt.ToString("MMM yyyy"), Sort = x.CreatedAt.Year * 100 + x.CreatedAt.Month })
                };

                var periods = grouped
                    .Select(g => new {
                        Period       = g.Key.Label,
                        Sort         = g.Key.Sort,
                        Count        = g.Count(),
                        InLitigation = g.Count(x => x.IsInLitigation),
                        TotalClaimed = g.Sum(x => x.ClaimedAmount),
                        ByReferral   = g.GroupBy(x => x.ReferralSource ?? "Not Specified")
                                        .Select(r => new { Source = r.Key, Count = r.Count() })
                                        .OrderByDescending(r => r.Count).ToList()
                    })
                    .OrderBy(x => x.Sort)
                    .ToList();

                // Referral source breakdown (overall)
                var byReferral = cases
                    .GroupBy(x => x.ReferralSource ?? "Not Specified")
                    .Select(g => new { Source = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                // Detail rows
                var rows = cases.Select(c => new {
                    c.Id,
                    c.FileNumber,
                    c.PrimaryClaimantName,
                    StatusName     = c.CaseStatus?.StatusName ?? "",
                    CaseworkerName = c.AssignedCaseworker?.FullName ?? "Unassigned",
                    c.ReferralSource,
                    c.AccidentDate,
                    OpenedDate     = c.CreatedAt,
                    c.IsInLitigation,
                    c.ClaimedAmount
                }).ToList();

                return JsonOk(new {
                    total      = cases.Count,
                    byReferral,
                    periods,
                    rows
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NewCasesByPeriodData failed");
                return JsonError("Failed to generate report.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NewCasesByPeriodExcel(
            string? dateFrom = null, string? dateTo = null,
            string? groupBy = "month", string? caseworkerId = null)
        {
            var q = _db.CaseFiles
                .Include(x => x.CaseStatus)
                .Include(x => x.AssignedCaseworker)
                .AsNoTracking().AsQueryable();

            if (DateTime.TryParse(dateFrom, out var df)) q = q.Where(x => x.CreatedAt >= df);
            if (DateTime.TryParse(dateTo,   out var dt)) q = q.Where(x => x.CreatedAt <= dt);
            if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                q = q.Where(x => x.AssignedCaseworkerId == cwid);

            var cases = await q.OrderBy(x => x.CreatedAt).ToListAsync();

            using var wb = new XLWorkbook();
            var hdrColor  = XLColor.FromHtml("#7c3aed");

            // ── Sheet 1: Period Summary ───────────────────────────────────
            var ws = wb.Worksheets.Add("Period Summary");
            StyleHeaderRow(ws.Row(1), hdrColor);
            new[] { "Period","New Cases","In Litigation","Total Claimed (MYR)" }
                .Select((h, i) => new { h, i }).ToList()
                .ForEach(x => ws.Cell(1, x.i + 1).Value = x.h);

            var grouped = groupBy switch
            {
                "quarter" => cases.GroupBy(x => $"Q{(x.CreatedAt.Month - 1) / 3 + 1} {x.CreatedAt.Year}"),
                "year"    => cases.GroupBy(x => x.CreatedAt.Year.ToString()),
                _         => cases.GroupBy(x => x.CreatedAt.ToString("MMM yyyy"))
            };

            int row = 2;
            foreach (var g in grouped)
            {
                ws.Cell(row, 1).Value = g.Key;
                ws.Cell(row, 2).Value = g.Count();
                ws.Cell(row, 3).Value = g.Count(x => x.IsInLitigation);
                ws.Cell(row, 4).Value = g.Sum(x => x.ClaimedAmount);
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                if (row % 2 == 0) ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
                row++;
            }
            AutoFitColumns(ws, 1, 4);

            // ── Sheet 2: Referral Sources ─────────────────────────────────
            var ws2 = wb.Worksheets.Add("Referral Sources");
            StyleHeaderRow(ws2.Row(1), hdrColor);
            new[] { "Referral Source","Count" }
                .Select((h, i) => new { h, i }).ToList()
                .ForEach(x => ws2.Cell(1, x.i + 1).Value = x.h);
            int r2 = 2;
            foreach (var g in cases.GroupBy(x => x.ReferralSource ?? "Not Specified").OrderByDescending(x => x.Count()))
            {
                ws2.Cell(r2, 1).Value = g.Key;
                ws2.Cell(r2, 2).Value = g.Count();
                if (r2 % 2 == 0) ws2.Row(r2).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
                r2++;
            }
            AutoFitColumns(ws2, 1, 2);

            // ── Sheet 3: Case Detail ──────────────────────────────────────
            var ws3 = wb.Worksheets.Add("Case Detail");
            StyleHeaderRow(ws3.Row(1), hdrColor);
            new[] { "File Number","Claimant","Status","Caseworker","Referral Source","Accident Date","Opened On","Claimed (MYR)","In Litigation" }
                .Select((h, i) => new { h, i }).ToList()
                .ForEach(x => ws3.Cell(1, x.i + 1).Value = x.h);
            int r3 = 2;
            foreach (var c in cases)
            {
                ws3.Cell(r3, 1).Value = c.FileNumber;
                ws3.Cell(r3, 2).Value = c.PrimaryClaimantName;
                ws3.Cell(r3, 3).Value = c.CaseStatus?.StatusName ?? "";
                ws3.Cell(r3, 4).Value = c.AssignedCaseworker?.FullName ?? "Unassigned";
                ws3.Cell(r3, 5).Value = c.ReferralSource ?? "Not Specified";
                ws3.Cell(r3, 6).Value = c.AccidentDate.HasValue ? c.AccidentDate.Value.ToString("dd/MM/yyyy") : "";
                ws3.Cell(r3, 7).Value = c.CreatedAt.ToString("dd/MM/yyyy");
                ws3.Cell(r3, 8).Value = c.ClaimedAmount;
                ws3.Cell(r3, 9).Value = c.IsInLitigation ? "Yes" : "No";
                ws3.Cell(r3, 8).Style.NumberFormat.Format = "#,##0.00";
                if (r3 % 2 == 0) ws3.Row(r3).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
                r3++;
            }
            AutoFitColumns(ws3, 1, 9);
            ws3.SheetView.FreezeRows(1);

            return ExcelFile(wb, $"NewCasesByPeriod_{DateTime.Now:yyyyMMdd}");
        }

        // ─── Shared Excel file response helper ────────────────────────────
        private FileContentResult ExcelFile(XLWorkbook wb, string fileName)
        {
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{fileName}.xlsx");
        }
    }
}
