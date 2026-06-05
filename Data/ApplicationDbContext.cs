using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Models;
using MVAManagement.Models.MVA;

namespace MVAManagement.Data
{
    // ──────────────────────────────────────────────────────────────────────
    // FIX: was IdentityDbContext (untyped) — must be IdentityDbContext<ApplicationUser>
    //
    // The untyped base registers IdentityUser, not ApplicationUser, so EF Core
    // cannot create a DbSet<ApplicationUser> and UserManager<ApplicationUser>
    // throws "type is not included in the model for the context".
    // ──────────────────────────────────────────────────────────────────────
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ── MVA domain tables ─────────────────────────────────────────────
        public DbSet<CaseFile>           CaseFiles            { get; set; }
        public DbSet<CaseDisbursement>   CaseDisbursements    { get; set; }
        public DbSet<AccidentVehicle>    AccidentVehicles     { get; set; }
        public DbSet<AuditSessionLog>    AuditSessionLogs     { get; set; }
        public DbSet<InsurerRegistry>    InsurerRegistries    { get; set; }
        public DbSet<CaseDocument>       CaseDocuments        { get; set; }
        public DbSet<HearingRecord>      HearingRecords       { get; set; }
        public DbSet<InjuryRecord>       InjuryRecords        { get; set; }
        public DbSet<CaseStatus>         CaseStatuses         { get; set; }
        public DbSet<CaseworkerProfile>  CaseworkerProfiles   { get; set; }

        public DbSet<CourtVenue>          CourtVenues          { get; set; }    
        public DbSet<DisbursementCategory> DisbursementCategories { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // MUST call base first — wires up all ASP.NET Identity tables
            base.OnModelCreating(builder);

            // ── Map every DbSet to its exact SQL table name ───────────────
            //
            // EF Core default: pluralises the class name
            //   CaseFile → "CaseFiles"  ← does NOT exist in your DB
            //
            // Your tables are singular (confirmed from INFORMATION_SCHEMA):
            //   CaseFile, CaseDisbursement, CaseDocument, CaseStatus,
            //   CaseworkerProfile, CourtVenue, CaseJournal
            //
            // Remaining tables (HearingRecord, InjuryRecord, AccidentVehicle,
            // AuditSessionLog, InsurerRegistry) follow the same singular
            // pattern — adjust the string if any differ.

            builder.Entity<CaseFile>().ToTable("CaseFile");
            builder.Entity<CaseDisbursement>().ToTable("CaseDisbursement");
            builder.Entity<CaseDocument>().ToTable("CaseDocument");
            builder.Entity<CaseStatus>().ToTable("CaseStatus");
            builder.Entity<CaseworkerProfile>().ToTable("CaseworkerProfile");
            builder.Entity<HearingRecord>().ToTable("HearingRecord");
            builder.Entity<InjuryRecord>().ToTable("InjuryRecord");
            builder.Entity<AccidentVehicle>().ToTable("AccidentVehicle");
            builder.Entity<AuditSessionLog>().ToTable("AuditSessionLog");
            builder.Entity<InsurerRegistry>().ToTable("InsurerRegistry");
            builder.Entity<CourtVenue>().ToTable("CourtVenue");
        }

    }
}
