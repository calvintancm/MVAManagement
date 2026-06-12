using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Models;
using MVAManagement.Models.MVA;

namespace MVAManagement.Data
{
    // Use IdentityDbContext<ApplicationUser> so Identity tables (AspNetUsers, etc.) are included
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ── Domain tables ─────────────────────────────────────────────
        public DbSet<CaseFile> CaseFiles { get; set; }
        public DbSet<CaseDisbursement> CaseDisbursements { get; set; }
        public DbSet<AccidentVehicle> AccidentVehicles { get; set; }
        public DbSet<AuditSessionLog> AuditSessionLogs { get; set; }
        public DbSet<InsurerRegistry> InsurerRegistries { get; set; }
        public DbSet<CaseDocument> CaseDocuments { get; set; }
        public DbSet<HearingRecord> HearingRecords { get; set; }
        public DbSet<InjuryRecord> InjuryRecords { get; set; }
        public DbSet<CaseStatus> CaseStatuses { get; set; }
        public DbSet<CaseworkerProfile> CaseworkerProfiles { get; set; }
        public DbSet<CourtVenue> CourtVenues { get; set; }
        public DbSet<DisbursementCategory> DisbursementCategories { get; set; }
        public DbSet<HearingStage> HearingStages { get; set; }
        public DbSet<SettlementOffer> SettlementOffers { get; set; }

        public DbSet<MedicalExamination> MedicalExaminations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── Table name mappings ────────────────────────────────────────────
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
            builder.Entity<HearingStage>().ToTable("HearingStage");
            builder.Entity<DisbursementCategory>().ToTable("DisbursementCategory"); // ✅ singular, matches DB
            builder.Entity<SettlementOffer>().ToTable("SettlementOffer");
            builder.Entity<MedicalExamination>().ToTable("MedicalExamination");

            // ── HearingStage explicit column mapping ───────────────────────────
            builder.Entity<HearingStage>(entity =>
            {
                entity.Property(e => e.StageCode)
                      .IsRequired()
                      .HasMaxLength(10);

                entity.Property(e => e.StageDescription)
                      .IsRequired();

                entity.Property(e => e.DisplayOrder)
                      .HasDefaultValue(0);
            });

            // ── DisbursementCategory configuration ─────────────────────────────
            builder.Entity<DisbursementCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(300);
                entity.Property(e => e.HexColor).HasMaxLength(10).HasDefaultValue("#94A3B8");
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                // Seed data
                entity.HasData(
                    new DisbursementCategory { Id = 1, CategoryName = "Medical Report Fee", HexColor = "#185FA5", DisplayOrder = 1, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new DisbursementCategory { Id = 2, CategoryName = "Specialist Examination Fee", HexColor = "#0EA5E9", DisplayOrder = 2, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new DisbursementCategory { Id = 3, CategoryName = "Court Filing Fee", HexColor = "#7C3AED", DisplayOrder = 3, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new DisbursementCategory { Id = 4, CategoryName = "Police Report Fee", HexColor = "#DC2626", DisplayOrder = 4, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new DisbursementCategory { Id = 5, CategoryName = "Process Server Fee", HexColor = "#D97706", DisplayOrder = 5, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new DisbursementCategory { Id = 6, CategoryName = "Expert Witness Fee", HexColor = "#059669", DisplayOrder = 6, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new DisbursementCategory { Id = 7, CategoryName = "Interpreter Fee", HexColor = "#DB2777", DisplayOrder = 7, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new DisbursementCategory { Id = 8, CategoryName = "Postage / Courier", HexColor = "#64748B", DisplayOrder = 8, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new DisbursementCategory { Id = 9, CategoryName = "Miscellaneous", HexColor = "#94A3B8", DisplayOrder = 9, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
                );
            });

            // ── CaseJournal FK disambiguation ─────────────────────────────────
            builder.Entity<CaseJournal>(entity =>
            {
                entity.HasOne(j => j.Author)
                      .WithMany(c => c.JournalEntries)
                      .HasForeignKey(j => j.AuthorId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(j => j.CaseFile)
                      .WithMany(c => c.JournalEntries)
                      .HasForeignKey(j => j.CaseFileId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Global decimal precision ───────────────────────────────────────
            foreach (var entity in builder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties()
                             .Where(p => p.ClrType == typeof(decimal)
                                      || p.ClrType == typeof(decimal?)))
                {
                    if (property.GetColumnType() == null)
                    {
                        property.SetColumnType("decimal(18,2)");
                    }
                }
            }
        }
    }
}
