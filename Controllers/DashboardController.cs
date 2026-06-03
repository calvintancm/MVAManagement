using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Data;          
using MVAManagement.ViewModels.Dashboard;

namespace MVAManagement.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        // Avatar colour classes — cycled by caseworker index / case Id
        private static readonly string[] AvatarClasses =
            { "av-blue", "av-green", "av-amber", "av-rose", "av-violet", "av-teal" };

        // Status hex palette (keyed by StatusCode)
        private static readonly Dictionary<string, string> StatusHex = new()
        {
            { "ACT", "#185FA5" },
            { "NEG", "#F59E0B" },
            { "LIT", "#DC2626" },
            { "SET", "#16A34A" },
            { "CLO", "#6B7280" },
            { "WIT", "#9333EA" },
            { "HLD", "#0891B2" }
        };

        private static readonly string[] DisbursementColours =
            { "#185FA5", "#16A34A", "#F59E0B", "#DC2626", "#9333EA", "#0891B2", "#6B7280" };

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ═══════════════════════════════════════════════════════════════════
        // GET  /Dashboard  or  /Dashboard/Index?selectedMonth=6&selectedYear=2026
        // ═══════════════════════════════════════════════════════════════════
        public async Task<IActionResult> Index(int? selectedMonth, int? selectedYear)
        {
            var month = selectedMonth ?? DateTime.Today.Month;
            var year  = selectedYear  ?? DateTime.Today.Year;

            // Guard valid month/year
            month = Math.Clamp(month, 1, 12);
            year  = Math.Clamp(year, 2000, DateTime.Today.Year + 1);

            var periodStart = new DateTime(year, month, 1);
            var periodEnd   = periodStart.AddMonths(1).AddDays(-1);
            var prevStart   = periodStart.AddMonths(-1);
            var prevEnd     = periodStart.AddDays(-1);
            var today       = DateTime.Today;

            // ── Eagerly load core data ────────────────────────────────────
            // Load only active + recently-closed cases to keep query tight
            var allCases = await _db.GetCaseFiles()
                .Include(c => c.CaseStatus)
                .Include(c => c.CurrentHearingStage)
                .Include(c => c.AssignedCaseworker)
                .Where(c => c.IsActive || (!c.IsActive && c.CaseClosedDate >= prevStart))
                .AsNoTracking()
                .ToListAsync();

            var activeCases = allCases.Where(c => c.IsActive).ToList();

            // ── PREVIOUS MONTH baseline for % change calcs ────────────────
            var prevActiveCount = await _db.GetCaseFiles()
                .CountAsync(c => c.IsActive && c.CreatedAt < periodStart);

            var prevNewCount = await _db.GetCaseFiles()
                .CountAsync(c => c.CreatedAt >= prevStart && c.CreatedAt <= prevEnd);

            // ── Upcoming hearings (next 30 days) ──────────────────────────
            var hearingCutoff = today.AddDays(30);
            var upcomingHearings = await _db.HearingRecords
                .Include(h => h.CaseFile)
                .Include(h => h.HearingStage)
                .Include(h => h.CourtVenue)
                .Where(h => !h.IsCompleted
                         && h.ScheduledDate >= today
                         && h.ScheduledDate <= hearingCutoff)
                .OrderBy(h => h.ScheduledDate)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            // ── Pending medical reports count ─────────────────────────────
            var medPending = await _db.InjuryRecords
                .CountAsync(i => i.MedicalReportStatus == "Pending"
                              || i.MedicalReportStatus == "Requested");

            // ── Outstanding documents count ───────────────────────────────
            var docsOutstanding = await _db.CaseDocuments
                .CountAsync(d => !d.IsReceived
                              && d.CollectionStatus != "Not Required");

            // ── Disbursements by category (this month) ────────────────────
            var disbursements = await _db.CaseDisbursements
                .Where(d => d.DisbursementDate >= periodStart && d.DisbursementDate <= periodEnd)
                .GroupBy(d => d.DisbursementCategory)
                .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount), Count = g.Count() })
                .OrderByDescending(g => g.Total)
                .Take(7)
                .AsNoTracking()
                .ToListAsync();

            // ── Caseworker workload ───────────────────────────────────────
            var caseworkers = await _db.CaseworkerProfiles
                .Where(w => w.IsActive)
                .AsNoTracking()
                .ToListAsync();

            // ── Build KPI ─────────────────────────────────────────────────
            var newThisMonth    = activeCases.Count(c => c.CreatedAt >= periodStart && c.CreatedAt <= periodEnd);
            var closedThisMonth = allCases.Count(c  => c.CaseClosedDate >= periodStart && c.CaseClosedDate <= periodEnd);
            var inLitigation    = activeCases.Count(c => c.IsInLitigation);
            var limitIn90       = activeCases.Count(c =>
                c.StatuteOfLimitationsDeadline.HasValue &&
                !c.IsInLitigation &&
                (c.StatuteOfLimitationsDeadline.Value - today).TotalDays is >= 0 and <= 90);

            var totalClaimed   = activeCases.Sum(c => c.ClaimedAmount);
            var totalOffer     = activeCases.Sum(c => c.CurrentOffer);
            var totalDisburse  = activeCases.Sum(c => c.TotalDisbursementAmount);
            var settledCases   = allCases.Where(c => c.CaseStatus?.StatusCode == "SET").ToList();
            var avgSettlement  = settledCases.Any() ? settledCases.Average(c => c.CurrentOffer) : 0;

            decimal SafePct(int current, int previous) =>
                previous == 0 ? 0 : Math.Round((current - previous) / (decimal)previous * 100, 1);

            var kpi = new DashboardKpi
            {
                TotalActiveCases      = activeCases.Count,
                NewCasesThisMonth     = newThisMonth,
                ClosedCasesThisMonth  = closedThisMonth,
                InLitigationCount     = inLitigation,
                ActiveCasesPctChange  = SafePct(activeCases.Count, prevActiveCount),
                NewCasesPctChange     = SafePct(newThisMonth, prevNewCount),
                TotalClaimedAmount    = totalClaimed,
                TotalSettledAmount    = settledCases.Sum(c => c.CurrentOffer),
                TotalDisbursements    = totalDisburse,
                AvgSettlementValue    = Math.Round(avgSettlement, 2),
                LimitationDueIn90Days = limitIn90,
                PendingHearingsCount  = upcomingHearings.Count,
                MedicalReportsPending = medPending,
                DocumentsOutstanding  = docsOutstanding
            };

            // ── Case Grid rows (top 50 active, newest first) ──────────────
            var caseRows = activeCases
                .OrderByDescending(c => c.CreatedAt)
                .Take(50)
                .Select((c, idx) => new CaseGridRow
                {
                    Id                  = c.Id,
                    FileNumber          = c.FileNumber,
                    ClaimantName        = c.PrimaryClaimantName,
                    Initials            = BuildInitials(c.PrimaryClaimantName),
                    AvatarClass         = AvatarClasses[idx % AvatarClasses.Length],
                    StatusName          = c.CaseStatus?.StatusName ?? "—",
                    StatusCode          = c.CaseStatus?.StatusCode ?? "",
                    AssignedCaseworker  = c.AssignedCaseworker?.FullName ?? "Unassigned",
                    AccidentDate        = c.AccidentDate?.ToString("dd MMM yyyy"),
                    ClaimedAmount       = c.ClaimedAmount,
                    CurrentOffer        = c.CurrentOffer,
                    IsInLitigation      = c.IsInLitigation,
                    IsLimitationExpired = c.IsLimitationExpired,
                    DaysToLimitation    = c.StatuteOfLimitationsDeadline.HasValue
                                           ? (int)(c.StatuteOfLimitationsDeadline.Value - today).TotalDays
                                           : 9999,
                    NextHearingDate     = c.NextHearingDate?.ToString("dd MMM yyyy"),
                    HearingStage        = c.CurrentHearingStage?.StageCode ?? ""
                })
                .ToList();

            // ── Status mix (donut) ────────────────────────────────────────
            var statusMix = activeCases
                .GroupBy(c => new { Name = c.CaseStatus?.StatusName ?? "Unknown", Code = c.CaseStatus?.StatusCode ?? "ACT" })
                .Select(g => new StatusMixSegment
                {
                    Label    = g.Key.Name,
                    Code     = g.Key.Code,
                    Count    = g.Count(),
                    HexColor = StatusHex.GetValueOrDefault(g.Key.Code, "#94A3B8")
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            // ── Upcoming hearing rows ─────────────────────────────────────
            var hearingRows = upcomingHearings.Select(h =>
            {
                var daysAway = (int)(h.ScheduledDate - today).TotalDays;
                return new UpcomingHearingRow
                {
                    CaseFileId     = h.CaseFileId,
                    FileNumber     = h.CaseFile?.FileNumber ?? "",
                    ClaimantName   = h.CaseFile?.PrimaryClaimantName ?? "",
                    HearingStage   = h.HearingStage?.StageDescription ?? "",
                    StageCode      = h.HearingStage?.StageCode ?? "",
                    CourtVenue     = h.CourtVenue?.VenueName ?? "TBC",
                    HearingDateStr = h.ScheduledDate.ToString("dd MMM yyyy"),
                    HearingDay     = h.ScheduledDate.ToString("ddd"),
                    DaysFromToday  = daysAway,
                    UrgencyClass   = daysAway <= 3 ? "urgent" : daysAway <= 7 ? "soon" : "ok"
                };
            }).ToList();

            // ── Settlement funnel ─────────────────────────────────────────
            var allStatuses = await _db.CaseStatuses.AsNoTracking().OrderBy(s => s.DisplayOrder).ToListAsync();
            var funnelRows = allStatuses
                .Select(st =>
                {
                    var group = activeCases.Where(c => c.CaseStatusId == st.Id).ToList();
                    return new SettlementFunnelRow
                    {
                        StatusLabel   = st.StatusName,
                        CaseCount     = group.Count,
                        TotalClaimed  = group.Sum(c => c.ClaimedAmount),
                        TotalOffer    = group.Sum(c => c.CurrentOffer),
                        OfferRatioPct = group.Sum(c => c.ClaimedAmount) > 0
                            ? Math.Round(group.Sum(c => c.CurrentOffer) / group.Sum(c => c.ClaimedAmount) * 100, 1)
                            : 0,
                        HexColor      = StatusHex.GetValueOrDefault(st.StatusCode ?? "", "#94A3B8")
                    };
                })
                .Where(r => r.CaseCount > 0)
                .ToList();
            // Normalise bar widths
            var maxCount = funnelRows.Any() ? funnelRows.Max(r => r.CaseCount) : 1;
            funnelRows.ForEach(r => r.BarWidthPct = Math.Round((decimal)r.CaseCount / maxCount * 100, 1));

            // ── Caseworker workload ───────────────────────────────────────
            var workloadRows = caseworkers.Select((cw, idx) =>
            {
                var myCases   = activeCases.Where(c => c.AssignedCaseworkerId == cw.Id).ToList();
                var myLit     = myCases.Count(c => c.IsInLitigation);
                var myHrg     = upcomingHearings.Count(h => h.CaseFile?.AssignedCaseworkerId == cw.Id);
                var myAlert   = myCases.Count(c => c.StatuteOfLimitationsDeadline.HasValue
                    && !c.IsInLitigation
                    && (c.StatuteOfLimitationsDeadline.Value - today).TotalDays is >= 0 and <= 90);
                var load      = activeCases.Count > 0 ? (int)((decimal)myCases.Count / activeCases.Count * 100) : 0;
                return new CaseworkerWorkloadRow
                {
                    CaseworkerId     = cw.Id,
                    FullName         = cw.FullName,
                    Initials         = BuildInitials(cw.FullName),
                    AvatarClass      = AvatarClasses[idx % AvatarClasses.Length],
                    JobRole          = cw.JobRole,
                    TotalCases       = myCases.Count,
                    ActiveCases      = myCases.Count,
                    InLitigation     = myLit,
                    UpcomingHearings = myHrg,
                    LimitationAlerts = myAlert,
                    TotalClaimed     = myCases.Sum(c => c.ClaimedAmount),
                    LoadPct          = load,
                    LoadClass        = load >= 60 ? "high" : load >= 30 ? "medium" : "low"
                };
            }).OrderByDescending(r => r.TotalCases).ToList();

            // ── Limitation alerts ─────────────────────────────────────────
            var alerts = activeCases
                .Where(c => c.StatuteOfLimitationsDeadline.HasValue && !c.IsInLitigation)
                .Where(c => (c.StatuteOfLimitationsDeadline!.Value - today).TotalDays <= 90)
                .OrderBy(c => c.StatuteOfLimitationsDeadline)
                .Take(5)
                .Select(c =>
                {
                    var days = (int)(c.StatuteOfLimitationsDeadline!.Value - today).TotalDays;
                    return new LimitationAlertRow
                    {
                        CaseFileId      = c.Id,
                        FileNumber      = c.FileNumber,
                        ClaimantName    = c.PrimaryClaimantName,
                        DeadlineDateStr = c.StatuteOfLimitationsDeadline.Value.ToString("dd MMM yyyy"),
                        DaysRemaining   = days,
                        IsExpired       = days < 0
                    };
                }).ToList();

            // ── Disbursement chart ────────────────────────────────────────
            var maxDisb = disbursements.Any() ? disbursements.Max(d => d.Total) : 1;
            var disbChart = disbursements.Select((d, idx) => new DisbursementChartSegment
            {
                Category    = d.Category,
                Total       = d.Total,
                Count       = d.Count,
                HexColor    = DisbursementColours[idx % DisbursementColours.Length],
                BarWidthPct = Math.Round(d.Total / maxDisb * 100, 1)
            }).ToList();

            // ── Serialise to JSON for Kendo / JS ─────────────────────────
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            ViewBag.CaseRowsJson          = JsonSerializer.Serialize(caseRows,     jsonOptions);
            ViewBag.StatusMixJson         = JsonSerializer.Serialize(statusMix,    jsonOptions);
            ViewBag.DisbursementChartJson = JsonSerializer.Serialize(disbChart,    jsonOptions);

            // ── Compose ViewModel ─────────────────────────────────────────
            var vm = new DashboardViewModel
            {
                SelectedMonth     = month,
                SelectedYear      = year,
                PeriodLabel       = periodStart.ToString("MMMM yyyy"),
                Kpi               = kpi,
                LimitationAlerts  = alerts,
                CaseRowsJson      = ViewBag.CaseRowsJson,
                StatusMixJson     = ViewBag.StatusMixJson,
                UpcomingHearings  = hearingRows,
                SettlementFunnel  = funnelRows,
                WorkloadRows      = workloadRows,
                DisbursementChartJson = ViewBag.DisbursementChartJson
            };

            return View(vm);
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════

        private static string BuildInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "??";
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 1
                ? fullName[..Math.Min(2, fullName.Length)].ToUpper()
                : $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        }
    }
}
