using System;
using System.Collections.Generic;

namespace MVAManagement.ViewModels.Dashboard
{
    // ═══════════════════════════════════════════════════════════════════════════
    // ROOT VIEWMODEL
    // ═══════════════════════════════════════════════════════════════════════════

    public class DashboardViewModel
    {
        // Period selectors (driven by month/year dropdowns)
        public int    SelectedMonth { get; set; } = DateTime.Today.Month;
        public int    SelectedYear  { get; set; } = DateTime.Today.Year;
        public string PeriodLabel   { get; set; } = string.Empty; // E.g. "June 2026"

        // KPI summary cards
        public DashboardKpi     Kpi            { get; set; } = new();

        // Approaching limitation alert strip
        public List<LimitationAlertRow> LimitationAlerts { get; set; } = new();

        // Recent active case grid (pre-serialised for Kendo)
        public string CaseRowsJson         { get; set; } = "[]";

        // Case status breakdown (donut/pie)
        public string StatusMixJson        { get; set; } = "[]";

        // Hearing calendar — upcoming hearings
        public List<UpcomingHearingRow> UpcomingHearings { get; set; } = new();

        // Settlement funnel (offer vs claimed)
        public List<SettlementFunnelRow> SettlementFunnel { get; set; } = new();

        // Caseworker workload table
        public List<CaseworkerWorkloadRow> WorkloadRows { get; set; } = new();

        // Top disbursement categories (bar chart)
        public string DisbursementChartJson { get; set; } = "[]";
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // KPI CARDS
    // ═══════════════════════════════════════════════════════════════════════════

    public class DashboardKpi
    {
        // Case counts
        public int TotalActiveCases      { get; set; }
        public int NewCasesThisMonth     { get; set; }
        public int ClosedCasesThisMonth  { get; set; }
        public int InLitigationCount     { get; set; }

        // Period-over-period % changes
        public decimal ActiveCasesPctChange  { get; set; }
        public decimal NewCasesPctChange     { get; set; }

        // Financial
        public decimal TotalClaimedAmount    { get; set; }
        public decimal TotalSettledAmount    { get; set; }
        public decimal TotalDisbursements    { get; set; }
        public decimal AvgSettlementValue    { get; set; }

        // Risk / urgency
        public int     LimitationDueIn90Days { get; set; }  // Cases expiring soon
        public int     PendingHearingsCount  { get; set; }  // Hearings in next 30 days
        public int     MedicalReportsPending { get; set; }
        public int     DocumentsOutstanding  { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CASE GRID ROW  (serialised to JSON → Kendo DataSource)
    // ═══════════════════════════════════════════════════════════════════════════

    public class CaseGridRow
    {
        public int     Id                   { get; set; }
        public string  FileNumber           { get; set; } = string.Empty;
        public string  ClaimantName         { get; set; } = string.Empty;
        public string  Initials             { get; set; } = string.Empty;   // 2-char avatar
        public string  AvatarClass          { get; set; } = string.Empty;   // CSS colour class
        public string  StatusName           { get; set; } = string.Empty;
        public string  StatusCode           { get; set; } = string.Empty;
        public string  AssignedCaseworker   { get; set; } = string.Empty;
        public string? AccidentDate         { get; set; }                   // "dd MMM yyyy"
        public decimal ClaimedAmount        { get; set; }
        public decimal CurrentOffer         { get; set; }
        public bool    IsInLitigation       { get; set; }
        public bool    IsLimitationExpired  { get; set; }
        public int     DaysToLimitation     { get; set; }                   // negative = expired
        public string? NextHearingDate      { get; set; }                   // "dd MMM yyyy" or null
        public string  HearingStage         { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // STATUS MIX  (donut chart segments)
    // ═══════════════════════════════════════════════════════════════════════════

    public class StatusMixSegment
    {
        public string Label    { get; set; } = string.Empty;
        public string Code     { get; set; } = string.Empty;
        public int    Count    { get; set; }
        public string HexColor { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UPCOMING HEARINGS  (calendar strip)
    // ═══════════════════════════════════════════════════════════════════════════

    public class UpcomingHearingRow
    {
        public int    CaseFileId     { get; set; }
        public string FileNumber     { get; set; } = string.Empty;
        public string ClaimantName   { get; set; } = string.Empty;
        public string HearingStage   { get; set; } = string.Empty;
        public string StageCode      { get; set; } = string.Empty;
        public string CourtVenue     { get; set; } = string.Empty;
        public string HearingDateStr { get; set; } = string.Empty;  // "dd MMM yyyy"
        public string HearingDay     { get; set; } = string.Empty;  // "Mon", "Tue" …
        public int    DaysFromToday  { get; set; }
        public string UrgencyClass   { get; set; } = string.Empty;  // "urgent", "soon", "ok"
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SETTLEMENT FUNNEL
    // ═══════════════════════════════════════════════════════════════════════════

    public class SettlementFunnelRow
    {
        public string  StatusLabel        { get; set; } = string.Empty;
        public int     CaseCount          { get; set; }
        public decimal TotalClaimed       { get; set; }
        public decimal TotalOffer         { get; set; }
        public decimal OfferRatioPct      { get; set; }  // (TotalOffer / TotalClaimed) * 100
        public string  HexColor           { get; set; } = string.Empty;
        public decimal BarWidthPct        { get; set; }  // relative to max for bar rendering
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CASEWORKER WORKLOAD
    // ═══════════════════════════════════════════════════════════════════════════

    public class CaseworkerWorkloadRow
    {
        public int     CaseworkerId      { get; set; }
        public string  FullName          { get; set; } = string.Empty;
        public string  Initials          { get; set; } = string.Empty;
        public string  AvatarClass       { get; set; } = string.Empty;
        public string  JobRole           { get; set; } = string.Empty;
        public int     TotalCases        { get; set; }
        public int     ActiveCases       { get; set; }
        public int     InLitigation      { get; set; }
        public int     UpcomingHearings  { get; set; }
        public int     LimitationAlerts  { get; set; }
        public decimal TotalClaimed      { get; set; }
        public int     LoadPct           { get; set; }  // % of max team load (for bar)
        public string  LoadClass         { get; set; } = string.Empty; // "low","medium","high"
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LIMITATION ALERT ROW
    // ═══════════════════════════════════════════════════════════════════════════

    public class LimitationAlertRow
    {
        public int    CaseFileId      { get; set; }
        public string FileNumber      { get; set; } = string.Empty;
        public string ClaimantName    { get; set; } = string.Empty;
        public string DeadlineDateStr { get; set; } = string.Empty;
        public int    DaysRemaining   { get; set; }
        public bool   IsExpired       { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DISBURSEMENT CHART SEGMENT
    // ═══════════════════════════════════════════════════════════════════════════

    public class DisbursementChartSegment
    {
        public string  Category   { get; set; } = string.Empty;
        public decimal Total      { get; set; }
        public int     Count      { get; set; }
        public string  HexColor   { get; set; } = string.Empty;
        public decimal BarWidthPct { get; set; }
    }
}
