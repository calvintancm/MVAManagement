using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Data;
using System.Text.Json;
using ClosedXML.Excel;

namespace MVAManagement.Controllers
{
    [Authorize]
    public class MedicalDocReportController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<MedicalDocReportController> _logger;

        public MedicalDocReportController(ApplicationDbContext db, ILogger<MedicalDocReportController> logger)
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
        //  R11 — PENDING MEDICAL REPORTS
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> PendingMedicalReports()
        {
            ViewBag.Caseworkers = await _db.CaseworkerProfiles
                .Where(x => x.IsActive).OrderBy(x => x.FullName)
                .Select(x => new { x.Id, x.FullName }).ToListAsync();

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> PendingMedicalReportsData(
            string? caseworkerId = null,
            string? severity = null,
            string? reportStatus = null,
            string? specialty = null)
        {
            try
            {
                // Default: Pending + Requested
                var statuses = string.IsNullOrWhiteSpace(reportStatus)
                    ? new[] { "Pending", "Requested" }
                    : new[] { reportStatus };

                var q = _db.InjuryRecords
                    .Include(x => x.CaseFile).ThenInclude(c => c.AssignedCaseworker)
                    .Include(x => x.CaseFile).ThenInclude(c => c.CaseStatus)
                    .Where(x => statuses.Contains(x.MedicalReportStatus))
                    .AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(severity))
                    q = q.Where(x => x.SeverityLevel == severity);

                if (!string.IsNullOrWhiteSpace(specialty))
                    q = q.Where(x => x.MedicalSpecialty != null && x.MedicalSpecialty.Contains(specialty));

                if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                    q = q.Where(x => x.CaseFile != null && x.CaseFile.AssignedCaseworkerId == cwid);

                var injuries = await q.OrderBy(x => x.SeverityLevel).ToListAsync();

                var rows = injuries.Select(x => new {
                    x.Id,
                    CaseFileId      = x.CaseFileId,
                    FileNumber      = x.CaseFile?.FileNumber ?? "",
                    ClaimantName    = x.CaseFile?.PrimaryClaimantName ?? "",
                    CaseworkerName  = x.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned",
                    CaseStatus      = x.CaseFile?.CaseStatus?.StatusName ?? "",
                    x.InjuryDescription,
                    x.BodyPart,
                    x.SeverityLevel,
                    x.IsPermanentDisability,
                    x.TreatmentStatus,
                    x.HospitalName,
                    x.DoctorName,
                    x.MedicalSpecialty,
                    x.MedicalReportStatus
                }).ToList();

                // Summary by severity
                var bySeverity = injuries
                    .GroupBy(x => x.SeverityLevel)
                    .Select(g => new { Severity = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).ToList();

                // Summary by caseworker
                var byCaseworker = injuries
                    .GroupBy(x => x.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned")
                    .Select(g => new {
                        Caseworker = g.Key,
                        Total      = g.Count(),
                        Pending    = g.Count(x => x.MedicalReportStatus == "Pending"),
                        Requested  = g.Count(x => x.MedicalReportStatus == "Requested"),
                        Severe     = g.Count(x => x.SeverityLevel == "Severe" || x.SeverityLevel == "Permanent Disability")
                    })
                    .OrderByDescending(x => x.Total).ToList();

                // Summary by doctor/hospital
                var byDoctor = injuries
                    .Where(x => !string.IsNullOrWhiteSpace(x.DoctorName))
                    .GroupBy(x => new { Doctor = x.DoctorName!, Hospital = x.HospitalName ?? "" })
                    .Select(g => new { g.Key.Doctor, g.Key.Hospital, Count = g.Count() })
                    .OrderByDescending(x => x.Count).Take(10).ToList();

                return JsonOk(new {
                    total      = rows.Count,
                    pending    = injuries.Count(x => x.MedicalReportStatus == "Pending"),
                    requested  = injuries.Count(x => x.MedicalReportStatus == "Requested"),
                    severe     = injuries.Count(x => x.SeverityLevel == "Severe" || x.SeverityLevel == "Permanent Disability"),
                    permanent  = injuries.Count(x => x.IsPermanentDisability),
                    bySeverity,
                    byCaseworker,
                    byDoctor,
                    rows
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PendingMedicalReportsData failed");
                return JsonError("Failed to generate report.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PendingMedicalReportsExcel(
            string? caseworkerId = null,
            string? severity = null,
            string? reportStatus = null)
        {
            var statuses = string.IsNullOrWhiteSpace(reportStatus)
                ? new[] { "Pending", "Requested" }
                : new[] { reportStatus };

            var q = _db.InjuryRecords
                .Include(x => x.CaseFile).ThenInclude(c => c.AssignedCaseworker)
                .Include(x => x.CaseFile).ThenInclude(c => c.CaseStatus)
                .Where(x => statuses.Contains(x.MedicalReportStatus))
                .AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(severity))
                q = q.Where(x => x.SeverityLevel == severity);

            if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                q = q.Where(x => x.CaseFile != null && x.CaseFile.AssignedCaseworkerId == cwid);

            var injuries = await q
                .OrderBy(x => x.SeverityLevel)
                .ThenBy(x => x.CaseFile!.FileNumber)
                .ToListAsync();

            var hdr = XLColor.FromHtml("#dc2626");
            using var wb = new XLWorkbook();

            // ── Sheet 1: Pending Report Detail ────────────────────────────
            var ws = wb.Worksheets.Add("Pending Reports");
            StyleHeader(ws.Row(1), hdr);
            SetHeaders(ws, new[] {
                "File No.", "Claimant", "Caseworker", "Case Status",
                "Injury Description", "Body Part", "Severity",
                "Permanent Disability", "Treatment Status",
                "Doctor", "Hospital", "Specialty", "Report Status"
            });

            int row = 2;
            foreach (var inj in injuries)
            {
                ws.Cell(row, 1).Value  = inj.CaseFile?.FileNumber ?? "";
                ws.Cell(row, 2).Value  = inj.CaseFile?.PrimaryClaimantName ?? "";
                ws.Cell(row, 3).Value  = inj.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned";
                ws.Cell(row, 4).Value  = inj.CaseFile?.CaseStatus?.StatusName ?? "";
                ws.Cell(row, 5).Value  = inj.InjuryDescription;
                ws.Cell(row, 6).Value  = inj.BodyPart ?? "";
                ws.Cell(row, 7).Value  = inj.SeverityLevel;
                ws.Cell(row, 8).Value  = inj.IsPermanentDisability ? "Yes" : "No";
                ws.Cell(row, 9).Value  = inj.TreatmentStatus;
                ws.Cell(row, 10).Value = inj.DoctorName  ?? "";
                ws.Cell(row, 11).Value = inj.HospitalName ?? "";
                ws.Cell(row, 12).Value = inj.MedicalSpecialty ?? "";
                ws.Cell(row, 13).Value = inj.MedicalReportStatus;

                // Highlight severe rows
                var rowBg = inj.SeverityLevel == "Severe" || inj.SeverityLevel == "Permanent Disability"
                    ? XLColor.FromHtml("#fef2f2")
                    : inj.IsPermanentDisability
                    ? XLColor.FromHtml("#fce7f3")
                    : XLColor.NoColor;
                if (rowBg != XLColor.NoColor) ws.Row(row).Style.Fill.BackgroundColor = rowBg;
                else AlternateRow(ws.Row(row), row);
                row++;
            }
            AutoFit(ws, 1, 13);
            ws.SheetView.FreezeRows(1);

            // ── Sheet 2: By Severity ──────────────────────────────────────
            var ws2 = wb.Worksheets.Add("By Severity");
            StyleHeader(ws2.Row(1), hdr);
            SetHeaders(ws2, new[] { "Severity Level", "Count" });
            int r2 = 2;
            foreach (var g in injuries.GroupBy(x => x.SeverityLevel).OrderByDescending(g => g.Count()))
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
            SetHeaders(ws3, new[] { "Caseworker", "Total Pending", "Status: Pending", "Status: Requested", "Severe Cases" });
            int r3 = 2;
            foreach (var g in injuries.GroupBy(x => x.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned").OrderByDescending(g => g.Count()))
            {
                ws3.Cell(r3, 1).Value = g.Key;
                ws3.Cell(r3, 2).Value = g.Count();
                ws3.Cell(r3, 3).Value = g.Count(x => x.MedicalReportStatus == "Pending");
                ws3.Cell(r3, 4).Value = g.Count(x => x.MedicalReportStatus == "Requested");
                ws3.Cell(r3, 5).Value = g.Count(x => x.SeverityLevel == "Severe" || x.SeverityLevel == "Permanent Disability");
                AlternateRow(ws3.Row(r3), r3);
                r3++;
            }
            AutoFit(ws3, 1, 5);

            return ExcelFile(wb, $"PendingMedicalReports_{DateTime.Now:yyyyMMdd}");
        }

        // ══════════════════════════════════════════════════════════════════
        //  R12 — OUTSTANDING DOCUMENTS CHECKLIST
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> OutstandingDocuments()
        {
            ViewBag.Caseworkers = await _db.CaseworkerProfiles
                .Where(x => x.IsActive).OrderBy(x => x.FullName)
                .Select(x => new { x.Id, x.FullName }).ToListAsync();

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> OutstandingDocumentsData(
            string? caseworkerId = null,
            string? category = null,
            string? expectedFrom = null,
            string? collectionStatus = null)
        {
            try
            {
                var statuses = string.IsNullOrWhiteSpace(collectionStatus)
                    ? new[] { "Awaiting", "Requested" }
                    : new[] { collectionStatus };

                var q = _db.CaseDocuments
                    .Include(x => x.CaseFile).ThenInclude(c => c.AssignedCaseworker)
                    .Include(x => x.CaseFile).ThenInclude(c => c.CaseStatus)
                    .Where(x => !x.IsReceived && statuses.Contains(x.CollectionStatus))
                    .AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(category))
                    q = q.Where(x => x.DocumentCategory == category);

                if (!string.IsNullOrWhiteSpace(expectedFrom))
                    q = q.Where(x => x.ExpectedFrom != null && x.ExpectedFrom.Contains(expectedFrom));

                if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                    q = q.Where(x => x.CaseFile != null && x.CaseFile.AssignedCaseworkerId == cwid);

                var docs = await q.OrderBy(x => x.CaseFile!.FileNumber).ToListAsync();

                var rows = docs.Select(x => new {
                    x.Id,
                    CaseFileId      = x.CaseFileId,
                    FileNumber      = x.CaseFile?.FileNumber ?? "",
                    ClaimantName    = x.CaseFile?.PrimaryClaimantName ?? "",
                    CaseworkerName  = x.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned",
                    CaseStatus      = x.CaseFile?.CaseStatus?.StatusName ?? "",
                    x.DocumentName,
                    x.DocumentCategory,
                    x.ExpectedFrom,
                    x.CollectionStatus,
                    x.Remarks
                }).ToList();

                // By case file — grouped checklist
                var byCaseFile = docs
                    .GroupBy(x => new {
                        x.CaseFileId,
                        FileNumber   = x.CaseFile?.FileNumber ?? "",
                        ClaimantName = x.CaseFile?.PrimaryClaimantName ?? "",
                        Caseworker   = x.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned",
                        CaseStatus   = x.CaseFile?.CaseStatus?.StatusName ?? ""
                    })
                    .Select(g => new {
                        g.Key.CaseFileId,
                        g.Key.FileNumber,
                        g.Key.ClaimantName,
                        g.Key.Caseworker,
                        g.Key.CaseStatus,
                        Count     = g.Count(),
                        Awaiting  = g.Count(x => x.CollectionStatus == "Awaiting"),
                        Requested = g.Count(x => x.CollectionStatus == "Requested"),
                        Items     = g.Select(x => new {
                            x.DocumentName, x.DocumentCategory,
                            x.ExpectedFrom, x.CollectionStatus
                        }).ToList()
                    })
                    .OrderByDescending(x => x.Count).ToList();

                // By category
                var byCategory = docs
                    .GroupBy(x => x.DocumentCategory)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).ToList();

                // By expected from
                var bySource = docs
                    .GroupBy(x => x.ExpectedFrom ?? "Not Specified")
                    .Select(g => new { Source = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).ToList();

                return JsonOk(new {
                    total      = docs.Count,
                    awaiting   = docs.Count(x => x.CollectionStatus == "Awaiting"),
                    requested  = docs.Count(x => x.CollectionStatus == "Requested"),
                    caseCount  = byCaseFile.Count,
                    byCategory,
                    bySource,
                    byCaseFile,
                    rows
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutstandingDocumentsData failed");
                return JsonError("Failed to generate report.");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> OutstandingDocumentsExcel(
            string? caseworkerId = null,
            string? category = null,
            string? expectedFrom = null,
            string? collectionStatus = null)
        {
            var statuses = string.IsNullOrWhiteSpace(collectionStatus)
                ? new[] { "Awaiting", "Requested" }
                : new[] { collectionStatus };

            var q = _db.CaseDocuments
                .Include(x => x.CaseFile).ThenInclude(c => c.AssignedCaseworker)
                .Include(x => x.CaseFile).ThenInclude(c => c.CaseStatus)
                .Where(x => !x.IsReceived && statuses.Contains(x.CollectionStatus))
                .AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(x => x.DocumentCategory == category);

            if (!string.IsNullOrWhiteSpace(expectedFrom))
                q = q.Where(x => x.ExpectedFrom != null && x.ExpectedFrom.Contains(expectedFrom));

            if (int.TryParse(caseworkerId, out var cwid) && cwid > 0)
                q = q.Where(x => x.CaseFile != null && x.CaseFile.AssignedCaseworkerId == cwid);

            var docs = await q.OrderBy(x => x.CaseFile!.FileNumber).ThenBy(x => x.DocumentCategory).ToListAsync();
            var hdr  = XLColor.FromHtml("#1d6fa4");

            using var wb = new XLWorkbook();

            // ── Sheet 1: Full Checklist ───────────────────────────────────
            var ws = wb.Worksheets.Add("Outstanding Documents");
            StyleHeader(ws.Row(1), hdr);
            SetHeaders(ws, new[] {
                "File No.", "Claimant", "Caseworker", "Case Status",
                "Document Name", "Category", "Expected From", "Collection Status", "Remarks"
            });

            int row = 2;
            foreach (var d in docs)
            {
                ws.Cell(row, 1).Value = d.CaseFile?.FileNumber ?? "";
                ws.Cell(row, 2).Value = d.CaseFile?.PrimaryClaimantName ?? "";
                ws.Cell(row, 3).Value = d.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned";
                ws.Cell(row, 4).Value = d.CaseFile?.CaseStatus?.StatusName ?? "";
                ws.Cell(row, 5).Value = d.DocumentName;
                ws.Cell(row, 6).Value = d.DocumentCategory;
                ws.Cell(row, 7).Value = d.ExpectedFrom ?? "";
                ws.Cell(row, 8).Value = d.CollectionStatus;
                ws.Cell(row, 9).Value = d.Remarks ?? "";

                var rowBg = d.DocumentCategory == "Medical" ? XLColor.FromHtml("#fef2f2")
                          : d.DocumentCategory == "Legal"   ? XLColor.FromHtml("#faf5ff")
                          : d.DocumentCategory == "Court"   ? XLColor.FromHtml("#ecfeff")
                          : XLColor.NoColor;
                if (rowBg != XLColor.NoColor) ws.Row(row).Style.Fill.BackgroundColor = rowBg;
                else AlternateRow(ws.Row(row), row);
                row++;
            }
            AutoFit(ws, 1, 9);
            ws.SheetView.FreezeRows(1);

            // ── Sheet 2: By Case File ─────────────────────────────────────
            var ws2 = wb.Worksheets.Add("By Case File");
            StyleHeader(ws2.Row(1), hdr);
            SetHeaders(ws2, new[] { "File No.", "Claimant", "Caseworker", "Case Status", "Total Outstanding", "Awaiting", "Requested" });
            int r2 = 2;
            foreach (var g in docs.GroupBy(x => new {
                FileNumber = x.CaseFile?.FileNumber ?? "",
                Claimant   = x.CaseFile?.PrimaryClaimantName ?? "",
                Caseworker = x.CaseFile?.AssignedCaseworker?.FullName ?? "Unassigned",
                Status     = x.CaseFile?.CaseStatus?.StatusName ?? ""
            }).OrderByDescending(g => g.Count()))
            {
                ws2.Cell(r2, 1).Value = g.Key.FileNumber;
                ws2.Cell(r2, 2).Value = g.Key.Claimant;
                ws2.Cell(r2, 3).Value = g.Key.Caseworker;
                ws2.Cell(r2, 4).Value = g.Key.Status;
                ws2.Cell(r2, 5).Value = g.Count();
                ws2.Cell(r2, 6).Value = g.Count(x => x.CollectionStatus == "Awaiting");
                ws2.Cell(r2, 7).Value = g.Count(x => x.CollectionStatus == "Requested");
                AlternateRow(ws2.Row(r2), r2);
                r2++;
            }
            AutoFit(ws2, 1, 7);

            // ── Sheet 3: By Category ──────────────────────────────────────
            var ws3 = wb.Worksheets.Add("By Category");
            StyleHeader(ws3.Row(1), hdr);
            SetHeaders(ws3, new[] { "Document Category", "Count" });
            int r3 = 2;
            foreach (var g in docs.GroupBy(x => x.DocumentCategory).OrderByDescending(g => g.Count()))
            {
                ws3.Cell(r3, 1).Value = g.Key;
                ws3.Cell(r3, 2).Value = g.Count();
                AlternateRow(ws3.Row(r3), r3);
                r3++;
            }
            AutoFit(ws3, 1, 2);

            // ── Sheet 4: By Expected From ─────────────────────────────────
            var ws4 = wb.Worksheets.Add("By Source");
            StyleHeader(ws4.Row(1), hdr);
            SetHeaders(ws4, new[] { "Expected From", "Count" });
            int r4 = 2;
            foreach (var g in docs.GroupBy(x => x.ExpectedFrom ?? "Not Specified").OrderByDescending(g => g.Count()))
            {
                ws4.Cell(r4, 1).Value = g.Key;
                ws4.Cell(r4, 2).Value = g.Count();
                AlternateRow(ws4.Row(r4), r4);
                r4++;
            }
            AutoFit(ws4, 1, 2);

            return ExcelFile(wb, $"OutstandingDocuments_{DateTime.Now:yyyyMMdd}");
        }
    }
}
