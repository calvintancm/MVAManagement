using Microsoft.EntityFrameworkCore;
using MVAManagement.Models.MVA;

namespace MVAManagement.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        //public DbSet<A_LegalCase> LegalCases { get; set; }
        //public DbSet<A_ClientStatus> ClientStatuses { get; set; }
        //public DbSet<A_InsuranceProvider> InsuranceProviders { get; set; }
        //public DbSet<A_CourtHearingType> CourtHearingTypes { get; set; }
        //public DbSet<A_VehicleDetail> VehicleDetails { get; set; }
        //public DbSet<A_ClaimantInjury> ClaimantInjuries { get; set; }
        //public DbSet<A_DocumentChecklist> DocumentChecklists { get; set; }
        //public DbSet<A_LitigationEvent> LitigationEvents { get; set; }
        //public DbSet<A_OfferHistory> OfferHistories { get; set; }
        //public DbSet<A_OperationalSchedule> OperationalSchedules { get; set; }
        //public DbSet<A_UserSessionLog> UserSessionLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map LegalCase Entity to Legacy tblCase
            //modelBuilder.Entity<A_LegalCase>(entity =>
            //{
            //    entity.ToTable("tblCase");
            //    entity.HasKey(e => e.Id);

            //    entity.Property(e => e.FileNumber).HasColumnName("FileNo").HasMaxLength(50).IsRequired();
            //    entity.Property(e => e.PrimaryClaimantName).HasColumnName("Name").HasMaxLength(255).IsRequired();
            //    entity.Property(e => e.SecondaryClaimantName).HasColumnName("SName");
            //    entity.Property(e => e.Gender).HasColumnName("Sex");
            //    entity.Property(e => e.DateOfBirth).HasColumnName("DOB");
            //    entity.Property(e => e.NationalId).HasColumnName("IC");
            //    entity.Property(e => e.TelephoneNumber).HasColumnName("Tel");
            //    entity.Property(e => e.MobileNumber).HasColumnName("HP");
            //    entity.Property(e => e.AccidentDate).HasColumnName("DOA");
            //    entity.Property(e => e.AssignedStaff).HasColumnName("Incharge");
            //    entity.Property(e => e.ClientStatusId).HasColumnName("Status");
            //    entity.Property(e => e.CurrentHearingTypeId).HasColumnName("Litigation");
            //    entity.Property(e => e.NextCourtDate).HasColumnName("LitigationDate");
            //    entity.Property(e => e.CaseClosedDate).HasColumnName("CaseCloseDate");

            //    // Decimals Financial Map
            //    entity.Property(e => e.ClaimedAmount).HasColumnName("OSum").HasPrecision(18, 2);
            //    entity.Property(e => e.MinimumSettlementTarget).HasColumnName("MinSum").HasPrecision(18, 2);
            //    entity.Property(e => e.CurrentOffer).HasColumnName("Offer").HasPrecision(18, 2);
            //    entity.Property(e => e.TotalDisbursementAmount).HasColumnName("Disbursment").HasPrecision(18, 2);

            //    // Short Bit Mapping (Converts standard C# bools into the older database's short constraints)
            //    entity.Property(e => e.IsInLitigation).HasColumnName("Litigate").HasConversion<short>();
            //    entity.Property(e => e.IsActive).HasColumnName("Active").HasConversion<short>();
            //    entity.Property(e => e.IsClosed).HasColumnName("CaseClose").HasConversion<short>();

            //    entity.Property(e => e.SummonsDraftedDate).HasColumnName("DraftDate");
            //    entity.Property(e => e.SummonsSealedDate).HasColumnName("SealDate");
            //    entity.Property(e => e.CreatedAt).HasColumnName("CreationDate");
            //    entity.Property(e => e.UpdatedAt).HasColumnName("ModifiedDate");
            //});

            // Map ClientStatus Entity to tblClientStatus
            //modelBuilder.Entity<A_ClientStatus>(entity =>
            //{
            //    entity.ToTable("tblClientStatus");
            //    entity.Property(e => e.StatusName).HasColumnName("ClientStatus");
            //    entity.Property(e => e.StatusCode).HasColumnName("Code");
            //});

            // Map InsuranceProvider to tblInsurance
            //modelBuilder.Entity<A_InsuranceProvider>(entity =>
            //{
            //    entity.ToTable("tblInsurance");
            //    entity.Property(e => e.ProviderName).HasColumnName("Name");
            //    entity.Property(e => e.ProviderCode).HasColumnName("Code");
            //});

            // Map CourtHearingType to tblLitigation
            //modelBuilder.Entity<A_CourtHearingType>(entity =>
            //{
            //    entity.ToTable("tblLitigation");
            //    entity.Property(e => e.Description).HasColumnName("Desc");
            //});

            // Map LitigationEvent log to tblLitigationAct
            //modelBuilder.Entity<A_LitigationEvent>(entity =>
            //{
            //    entity.ToTable("tblLitigationAct");
            //    entity.Property(e => e.LegalCaseId).HasColumnName("FK_ID");
            //    entity.Property(e => e.HearingTypeCode).HasColumnName("LitType");
            //    entity.Property(e => e.ScheduledDate).HasColumnName("LitDate");
            //    entity.Property(e => e.ProgressNotes).HasColumnName("Desc");
            //    entity.Property(e => e.IsCompleted).HasColumnName("Completed").HasConversion<short>();
            //});

            // Map OfferHistory to tblOfferHis
            //modelBuilder.Entity<A_OfferHistory>(entity =>
            //{
            //    entity.ToTable("tblOfferHis");
            //    entity.Property(e => e.LegalCaseId).HasColumnName("FK_ID");
            //    entity.Property(e => e.OfferAmount).HasColumnName("OfferAmt").HasPrecision(18, 2);
            //});

            // Map OperationalSchedule to tblSchedule
            //modelBuilder.Entity<A_OperationalSchedule>(entity =>
            //{
            //    entity.ToTable("tblSchedule");
            //    entity.Property(e => e.LegalCaseId).HasColumnName("FK_ID");
            //    entity.Property(e => e.AppointmentDate).HasColumnName("SchDate");
            //    entity.Property(e => e.ActivityType).HasColumnName("Type");
            //    entity.Property(e => e.ContextNotes).HasColumnName("OthersInfo");
            //    entity.Property(e => e.IsCompleted).HasColumnName("Completed").HasConversion<short>();
            //});
        }
    }
}