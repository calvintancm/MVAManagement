using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Data;
using System.Text.Json;
using ClosedXML.Excel;

namespace MVAManagement.Controllers
{
    [Authorize]
    public class LitigationReportController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<LitigationReportController> _logger;

        public LitigationReportController(ApplicationDbContext db, ILogger<LitigationReportController> logger)
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
        //  R9 — HEARING SCHEDULE REPORT
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> HearingSchedule()
        {
            ViewBag.Caseworkers = await _db.CaseworkerProfiles
                .Where(x => x.IsActive).OrderBy(x => x.FullName)
                .Select(x => new { x.Id, x.FullName }).ToListAsync();

            ViewBag.CourtVenues = await _db.CourtVenues
                .OrderBy(x => x.VenueName)
                .Select(x => new { x.Id, x.VenueName, x.VenueCode }).ToListAsync();

            ViewBag.HearingStages = await _db.HearingStages
                .OrderBy(x => x.StageCode)
                .Select(x => new { x.Id, x.StageCode, x.StageDescription }).ToListAsync();

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> HearingScheduleData(
            string? dateFrom = null, string? dateTo = null,
            string? caseworkerId = null, string? venueId = null,
            string? stageId = null, string? groupBy = "date")
        {
            try
            {
                var q = _db.HearingRecords
                    .Include(x => x.CaseFile).ThenInclude(c => c.AssignedCaseworker)
                    .Include(x => x.CaseFile).ThenInclude(c => c.CaseStatus)
                    .Include(x => x.HearingStage)
                    .Include(x => x.CourtVenue)
                    .Where(x => !x.IsCompleted)
                    .AsNoTracking().AsQueryable();

                // Default: today onwards
                var fromDate = DateTime.TryParse(dateFrom, out var df) ? df : DateTime.Today;
                var toDate   = DateTime.TryParse(dateTo,   out var dt) ? dt : DateTime.Today.AddDays(90);

                q = q.Where(x => x.ScheduledDate >= fromDate && x.ScheduledDate <= toDate);

                if (int.TryParse(venueId,  out var vid) && vid > 0) q = q.Where(x => x.CourtVenueId    == vid);
                if (int.TryParse(stageId,  out var sid) && sid > 0) q = q.Where(x => x.HearingStageId  == sid);
                if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                    q = q.Where(x => x.CaseFile != null && x.CaseFile.AssignedCaseworkerId == cwid);

                var hearings = await q.OrderBy(x => x.ScheduledDate).ThenBy(x => x.HearingTime).ToListAsync();
                var today    = DateTime.Today;

                var rows = hearings.Select(h => new {
                    h.Id,
                    CaseFileId     = h.CaseFileId,
                    FileNumber     = h.CaseFile?.FileNumber ?? "",
                    ClaimantName   = h.CaseFile?.PrimaryClaimantName ?? "",
                    CaseworkerName = h.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned",
                    CaseStatus     = h.CaseFile?.CaseStatus?.StatusName ?? "",
                    StageCode      = h.HearingStage?.StageCode ?? "",
                    StageDesc      = h.HearingStage?.StageDescription ?? "",
                    VenueName      = h.CourtVenue?.VenueName ?? "",
                    VenueCode      = h.CourtVenue?.VenueCode ?? "",
                    h.ScheduledDate,
                    HearingTime    = h.HearingTime.HasValue ? h.HearingTime.Value.ToString(@"hh\:mm") : "",
                    h.CourtroomNumber,
                    h.PresidingJudge,
                    h.AdjournedToDate,
                    DaysFromToday  = (int)(h.ScheduledDate - today).TotalDays,
                    IsToday        = h.ScheduledDate.Date == today,
                    IsThisWeek     = h.ScheduledDate.Date >= today && h.ScheduledDate.Date <= today.AddDays(7)
                }).ToList();

                // Group summaries
                var byVenue = hearings
                    .GroupBy(h => h.CourtVenue?.VenueName ?? "No Venue Assigned")
                    .Select(g => new { Venue = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).ToList();

                var byStage = hearings
                    .GroupBy(h => new { Code = h.HearingStage?.StageCode ?? "", Desc = h.HearingStage?.StageDescription ?? "" })
                    .Select(g => new { Stage = g.Key.Code + " — " + g.Key.Desc, Count = g.Count() })
                    .OrderByDescending(x => x.Count).ToList();

                var byCaseworker = hearings
                    .GroupBy(h => h.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned")
                    .Select(g => new { Caseworker = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).ToList();

                return JsonOk(new {
                    total       = rows.Count,
                    todayCount  = rows.Count(x => x.IsToday),
                    weekCount   = rows.Count(x => x.IsThisWeek),
                    venueCount  = byVenue.Count,
                    byVenue,
                    byStage,
                    byCaseworker,
                    rows
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HearingScheduleData failed");
                return JsonError("Failed to generate report.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> HearingScheduleExcel(
            string? dateFrom = null, string? dateTo = null,
            string? caseworkerId = null, string? venueId = null, string? stageId = null)
        {
            var q = _db.HearingRecords
                .Include(x => x.CaseFile).ThenInclude(c => c.AssignedCaseworker)
                .Include(x => x.CaseFile).ThenInclude(c => c.CaseStatus)
                .Include(x => x.HearingStage)
                .Include(x => x.CourtVenue)
                .Where(x => !x.IsCompleted)
                .AsNoTracking().AsQueryable();

            var fromDate = DateTime.TryParse(dateFrom, out var df) ? df : DateTime.Today;
            var toDate   = DateTime.TryParse(dateTo,   out var dt) ? dt : DateTime.Today.AddDays(90);
            q = q.Where(x => x.ScheduledDate >= fromDate && x.ScheduledDate <= toDate);

            if (int.TryParse(venueId,  out var vid) && vid > 0) q = q.Where(x => x.CourtVenueId   == vid);
            if (int.TryParse(stageId,  out var sid) && sid > 0) q = q.Where(x => x.HearingStageId == sid);
            if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                q = q.Where(x => x.CaseFile != null && x.CaseFile.AssignedCaseworkerId == cwid);

            var hearings = await q.OrderBy(x => x.ScheduledDate).ThenBy(x => x.HearingTime).ToListAsync();
            var today    = DateTime.Today;
            var hdr      = XLColor.FromHtml("#0e7490");

            using var wb = new XLWorkbook();

            // ── Sheet 1: Hearing Schedule ─────────────────────────────────
            var ws = wb.Worksheets.Add("Hearing Schedule");
            StyleHeader(ws.Row(1), hdr);
            SetHeaders(ws, new[] {
                "Date","Time","File No.","Claimant","Caseworker","Case Status",
                "Stage","Court Venue","Courtroom","Presiding Judge","Days From Today"
            });

            int row = 2;
            foreach (var h in hearings)
            {
                var days     = (int)(h.ScheduledDate - today).TotalDays;
                var rowColor = h.ScheduledDate.Date == today         ? XLColor.FromHtml("#dbeafe")
                             : h.ScheduledDate.Date <= today.AddDays(7) ? XLColor.FromHtml("#fef9c3")
                             : XLColor.NoColor;

                ws.Cell(row, 1).Value  = h.ScheduledDate.ToString("dd/MM/yyyy");
                ws.Cell(row, 2).Value  = h.HearingTime.HasValue ? h.HearingTime.Value.ToString(@"hh\:mm") : "";
                ws.Cell(row, 3).Value  = h.CaseFile?.FileNumber ?? "";
                ws.Cell(row, 4).Value  = h.CaseFile?.PrimaryClaimantName ?? "";
                ws.Cell(row, 5).Value  = h.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned";
                ws.Cell(row, 6).Value  = h.CaseFile?.CaseStatus?.StatusName ?? "";
                ws.Cell(row, 7).Value  = (h.HearingStage?.StageCode ?? "") + " — " + (h.HearingStage?.StageDescription ?? "");
                ws.Cell(row, 8).Value  = h.CourtVenue?.VenueName ?? "";
                ws.Cell(row, 9).Value  = h.CourtroomNumber ?? "";
                ws.Cell(row, 10).Value = h.PresidingJudge  ?? "";
                ws.Cell(row, 11).Value = days;

                if (rowColor != XLColor.NoColor) ws.Row(row).Style.Fill.BackgroundColor = rowColor;
                else AlternateRow(ws.Row(row), row);
                row++;
            }
            AutoFit(ws, 1, 11);
            ws.SheetView.FreezeRows(1);

            // ── Sheet 2: By Venue ─────────────────────────────────────────
            var ws2 = wb.Worksheets.Add("By Venue");
            StyleHeader(ws2.Row(1), hdr);
            SetHeaders(ws2, new[] { "Court Venue","Hearings Scheduled" });
            int r2 = 2;
            foreach (var g in hearings.GroupBy(h => h.CourtVenue?.VenueName ?? "No Venue").OrderByDescending(g => g.Count()))
            {
                ws2.Cell(r2, 1).Value = g.Key;
                ws2.Cell(r2, 2).Value = g.Count();
                AlternateRow(ws2.Row(r2), r2);
                r2++;
            }
            AutoFit(ws2, 1, 2);

            // ── Sheet 3: By Caseworker ────────────────────────────────────
            var ws3 = wb.Worksheets.Add("By Caseworker");
            StyleHeader(ws3.Row(1), hdr);
            SetHeaders(ws3, new[] { "Caseworker","Today","This Week","Total in Period" });
            int r3 = 2;
            foreach (var g in hearings.GroupBy(h => h.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned").OrderByDescending(g => g.Count()))
            {
                ws3.Cell(r3, 1).Value = g.Key;
                ws3.Cell(r3, 2).Value = g.Count(h => h.ScheduledDate.Date == today);
                ws3.Cell(r3, 3).Value = g.Count(h => h.ScheduledDate.Date >= today && h.ScheduledDate.Date <= today.AddDays(7));
                ws3.Cell(r3, 4).Value = g.Count();
                AlternateRow(ws3.Row(r3), r3);
                r3++;
            }
            AutoFit(ws3, 1, 4);

            return ExcelFile(wb, $"HearingSchedule_{DateTime.Now:yyyyMMdd}");
        }

        // ══════════════════════════════════════════════════════════════════
        //  R10 — LITIGATION CASES REPORT
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> LitigationCases()
        {
            ViewBag.Caseworkers = await _db.CaseworkerProfiles
                .Where(x => x.IsActive).OrderBy(x => x.FullName)
                .Select(x => new { x.Id, x.FullName }).ToListAsync();

            ViewBag.HearingStages = await _db.HearingStages
                .OrderBy(x => x.StageCode)
                .Select(x => new { x.Id, x.StageCode, x.StageDescription }).ToListAsync();

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> LitigationCasesData(
            string? caseworkerId = null, string? hearingStageId = null,
            string? summonsDraftedFrom = null, string? summonsDraftedTo = null)
        {
            try
            {
                var q = _db.CaseFiles
                    .Include(x => x.CaseStatus)
                    .Include(x => x.AssignedCaseworker)
                    .Include(x => x.CurrentHearingStage)
                    .Where(x => x.IsInLitigation && !x.IsClosed)
                    .AsNoTracking().AsQueryable();

                if (int.TryParse(caseworkerId,   out var cwid) && cwid > 0) q = q.Where(x => x.AssignedCaseworkerId    == cwid);
                if (int.TryParse(hearingStageId, out var hsid) && hsid > 0) q = q.Where(x => x.CurrentHearingStageId   == hsid);
                if (DateTime.TryParse(summonsDraftedFrom, out var sdf))     q = q.Where(x => x.SummonsDraftedDate >= sdf);
                if (DateTime.TryParse(summonsDraftedTo,   out var sdt))     q = q.Where(x => x.SummonsDraftedDate <= sdt);

                var cases = await q.OrderBy(x => x.NextHearingDate).ToListAsync();
                var today = DateTime.Today;

                var rows = cases.Select(c => new {
                    c.Id,
                    c.FileNumber,
                    c.CourtCaseNumber,
                    c.PrimaryClaimantName,
                    StatusName         = c.CaseStatus?.StatusName ?? "",
                    CaseworkerName     = c.AssignedCaseworker?.FullName ?? "Unassigned",
                    HearingStage       = c.CurrentHearingStage != null
                                         ? c.CurrentHearingStage.StageCode + " — " + c.CurrentHearingStage.StageDescription
                                         : "—",
                    c.AccidentDate,
                    c.SummonsDraftedDate,
                    c.SummonsSealedDate,
                    c.NextHearingDate,
                    c.ClaimedAmount,
                    c.CurrentOffer,
                    c.TotalDisbursementAmount,
                    DaysInLitigation   = c.SummonsDraftedDate.HasValue
                                         ? (int)(today - c.SummonsDraftedDate.Value).TotalDays : (int?)null,
                    NextHearingDays    = c.NextHearingDate.HasValue
                                         ? (int)(c.NextHearingDate.Value - today).TotalDays : (int?)null,
                    IsHearingUrgent    = c.NextHearingDate.HasValue
                                         && c.NextHearingDate.Value >= today
                                         && (c.NextHearingDate.Value - today).TotalDays <= 7
                }).ToList();

                // By hearing stage breakdown
                var byStage = cases
                    .GroupBy(c => c.CurrentHearingStage != null
                        ? c.CurrentHearingStage.StageCode + " — " + c.CurrentHearingStage.StageDescription
                        : "No Stage")
                    .Select(g => new {
                        Stage        = g.Key,
                        Count        = g.Count(),
                        TotalClaimed = g.Sum(c => c.ClaimedAmount)
                    })
                    .OrderByDescending(x => x.Count).ToList();

                // By caseworker
                var byCaseworker = cases
                    .GroupBy(c => c.AssignedCaseworker?.FullName ?? "Unassigned")
                    .Select(g => new {
                        Caseworker   = g.Key,
                        Count        = g.Count(),
                        TotalClaimed = g.Sum(c => c.ClaimedAmount),
                        HearingsDue  = g.Count(c => c.NextHearingDate.HasValue
                                          && c.NextHearingDate >= today
                                          && c.NextHearingDate <= today.AddDays(7))
                    })
                    .OrderByDescending(x => x.Count).ToList();

                return JsonOk(new {
                    total         = cases.Count,
                    totalClaimed  = cases.Sum(c => c.ClaimedAmount),
                    urgentHearings= rows.Count(x => x.IsHearingUrgent),
                    noSummons     = cases.Count(c => !c.SummonsDraftedDate.HasValue),
                    byStage,
                    byCaseworker,
                    rows
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LitigationCasesData failed");
                return JsonError("Failed to generate report.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LitigationCasesExcel(
            string? caseworkerId = null, string? hearingStageId = null,
            string? summonsDraftedFrom = null, string? summonsDraftedTo = null)
        {
            var q = _db.CaseFiles
                .Include(x => x.CaseStatus)
                .Include(x => x.AssignedCaseworker)
                .Include(x => x.CurrentHearingStage)
                .Where(x => x.IsInLitigation && !x.IsClosed)
                .AsNoTracking().AsQueryable();

            if (int.TryParse(caseworkerId,   out var cwid) && cwid > 0) q = q.Where(x => x.AssignedCaseworkerId   == cwid);
            if (int.TryParse(hearingStageId, out var hsid) && hsid > 0) q = q.Where(x => x.CurrentHearingStageId  == hsid);
            if (DateTime.TryParse(summonsDraftedFrom, out var sdf))      q = q.Where(x => x.SummonsDraftedDate >= sdf);
            if (DateTime.TryParse(summonsDraftedTo,   out var sdt))      q = q.Where(x => x.SummonsDraftedDate <= sdt);

            var cases = await q.OrderBy(x => x.NextHearingDate).ToListAsync();
            var today = DateTime.Today;
            var hdr   = XLColor.FromHtml("#7c3aed");

            using var wb = new XLWorkbook();

            // ── Sheet 1: Litigation Detail ────────────────────────────────
            var ws = wb.Worksheets.Add("Litigation Cases");
            StyleHeader(ws.Row(1), hdr);
            SetHeaders(ws, new[] {
                "File No.","Court Case No.","Claimant","Caseworker","Case Status",
                "Hearing Stage","Accident Date","Summons Drafted","Summons Sealed",
                "Next Hearing","Days in Litigation","Claimed (MYR)","Current Offer (MYR)","Disbursements (MYR)"
            });

            int row = 2;
            foreach (var c in cases)
            {
                var daysInLit = c.SummonsDraftedDate.HasValue
                    ? (int)(today - c.SummonsDraftedDate.Value).TotalDays : (int?)null;
                var nextDays  = c.NextHearingDate.HasValue
                    ? (int)(c.NextHearingDate.Value - today).TotalDays : (int?)null;

                ws.Cell(row, 1).Value  = c.FileNumber;
                ws.Cell(row, 2).Value  = c.CourtCaseNumber ?? "";
                ws.Cell(row, 3).Value  = c.PrimaryClaimantName;
                ws.Cell(row, 4).Value  = c.AssignedCaseworker?.FullName ?? "Unassigned";
                ws.Cell(row, 5).Value  = c.CaseStatus?.StatusName ?? "";
                ws.Cell(row, 6).Value  = c.CurrentHearingStage != null
                                         ? c.CurrentHearingStage.StageCode + " — " + c.CurrentHearingStage.StageDescription : "";
                ws.Cell(row, 7).Value  = c.AccidentDate.HasValue       ? c.AccidentDate.Value.ToString("dd/MM/yyyy")       : "";
                ws.Cell(row, 8).Value  = c.SummonsDraftedDate.HasValue ? c.SummonsDraftedDate.Value.ToString("dd/MM/yyyy") : "";
                ws.Cell(row, 9).Value  = c.SummonsSealedDate.HasValue  ? c.SummonsSealedDate.Value.ToString("dd/MM/yyyy")  : "";
                ws.Cell(row, 10).Value = c.NextHearingDate.HasValue     ? c.NextHearingDate.Value.ToString("dd/MM/yyyy")    : "";
                ws.Cell(row, 11).Value = daysInLit.HasValue ? daysInLit.ToString() : "—";
                ws.Cell(row, 12).Value = c.ClaimedAmount;
                ws.Cell(row, 13).Value = c.CurrentOffer;
                ws.Cell(row, 14).Value = c.TotalDisbursementAmount;
                for (int col = 12; col <= 14; col++)
                    ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";

                // Urgent hearing: amber; long litigation (>365d): light red
                var rowBg = nextDays.HasValue && nextDays <= 7 && nextDays >= 0 ? XLColor.FromHtml("#fef9c3")
                          : daysInLit.HasValue && daysInLit > 365 ? XLColor.FromHtml("#fef2f2")
                          : XLColor.NoColor;
                if (rowBg != XLColor.NoColor) ws.Row(row).Style.Fill.BackgroundColor = rowBg;
                else AlternateRow(ws.Row(row), row);
                row++;
            }
            AutoFit(ws, 1, 14);
            ws.SheetView.FreezeRows(1);

            // ── Sheet 2: By Hearing Stage ─────────────────────────────────
            var ws2 = wb.Worksheets.Add("By Hearing Stage");
            StyleHeader(ws2.Row(1), hdr);
            SetHeaders(ws2, new[] { "Hearing Stage","Cases","Total Claimed (MYR)" });
            int r2 = 2;
            foreach (var g in cases.GroupBy(c => c.CurrentHearingStage != null
                ? c.CurrentHearingStage.StageCode + " — " + c.CurrentHearingStage.StageDescription : "No Stage")
                .OrderByDescending(g => g.Count()))
            {
                ws2.Cell(r2, 1).Value = g.Key;
                ws2.Cell(r2, 2).Value = g.Count();
                ws2.Cell(r2, 3).Value = g.Sum(c => c.ClaimedAmount);
                ws2.Cell(r2, 3).Style.NumberFormat.Format = "#,##0.00";
                AlternateRow(ws2.Row(r2), r2);
                r2++;
            }
            AutoFit(ws2, 1, 3);

            // ── Sheet 3: By Caseworker ────────────────────────────────────
            var ws3 = wb.Worksheets.Add("By Caseworker");
            StyleHeader(ws3.Row(1), hdr);
            SetHeaders(ws3, new[] { "Caseworker","Litigation Cases","Hearings This Week","Total Claimed (MYR)" });
            int r3 = 2;
            foreach (var g in cases.GroupBy(c => c.AssignedCaseworker?.FullName ?? "Unassigned").OrderByDescending(g => g.Count()))
            {
                ws3.Cell(r3, 1).Value = g.Key;
                ws3.Cell(r3, 2).Value = g.Count();
                ws3.Cell(r3, 3).Value = g.Count(c => c.NextHearingDate.HasValue
                    && c.NextHearingDate >= today && c.NextHearingDate <= today.AddDays(7));
                ws3.Cell(r3, 4).Value = g.Sum(c => c.ClaimedAmount);
                ws3.Cell(r3, 4).Style.NumberFormat.Format = "#,##0.00";
                AlternateRow(ws3.Row(r3), r3);
                r3++;
            }
            AutoFit(ws3, 1, 4);

            return ExcelFile(wb, $"LitigationCases_{DateTime.Now:yyyyMMdd}");
        }
    }
}
