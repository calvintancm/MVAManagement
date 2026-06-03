using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Models.MVA;

namespace MVAManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<CaseDisbursement> CaseDisbursements { get; set; }
        private DbSet<CaseFile> caseFiles;

        public DbSet<CaseFile> GetCaseFiles()
        {
            return caseFiles;
        }

        public void SetCaseFiles(DbSet<CaseFile> value)
        {
            caseFiles = value;
        }

        public DbSet<AccidentVehicle> AccidentVehicles { get; set; }
        public DbSet<AuditSessionLog> AuditSessionLogs { get; set; }
        public DbSet<InsurerRegistry> InsurerRegistries { get; set; }
        public DbSet<CaseDocument> CaseDocuments { get; set; }
        public DbSet<HearingRecord> HearingRecords { get; set; }
        public DbSet<InjuryRecord> InjuryRecords { get; set; }
        public DbSet<CaseStatus> CaseStatuses { get; set; }
        public DbSet<CaseFile> CaseFiles { get; set; }
        public DbSet<CaseworkerProfile> CaseworkerProfiles { get; set; }


















    }
}
