
//CaseFile
namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Central entity representing a Motor Vehicle Accident legal case file.
    /// Aggregates all claimant details, accident parameters, financial ledger,
    /// litigation state, and relationships to all supporting case records.
    /// </summary>
    public class CaseFile
    {
        public int Id { get; set; }

        // ── FILE IDENTIFICATION ───────────────────────────────────────────────

        /// <summary>Unique firm file reference number. E.g. "PTC/MVA/2024/001".</summary>
        public string FileNumber { get; set; } = null!;

        /// <summary>Court-assigned case number once proceedings are filed.</summary>
        public string? CourtCaseNumber { get; set; }

        /// <summary>Internal solicitor's own reference, if different from the firm's FileNumber.</summary>
        public string? SolicitorFileReference { get; set; }

        // ── PRIMARY CLAIMANT ──────────────────────────────────────────────────

        /// <summary>Full legal name of the primary claimant / plaintiff.</summary>
        public string PrimaryClaimantName { get; set; } = null!;

        /// <summary>Name of secondary claimant, if applicable (e.g. spouse claiming loss of consortium).</summary>
        public string? SecondaryClaimantName { get; set; }

        /// <summary>Claimant gender. E.g. "Male", "Female".</summary>
        public string? Gender { get; set; }

        /// <summary>Claimant date of birth. Age is computed automatically via ClaimantAge.</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>National Identity Card number (MyKad / Passport).</summary>
        public string? NationalId { get; set; }

        /// <summary>Primary home or office telephone number.</summary>
        public string? TelephoneNumber { get; set; }

        /// <summary>Mobile / handphone number for primary contact.</summary>
        public string? MobileNumber { get; set; }

        /// <summary>Full postal address of the claimant.</summary>
        public string? FullAddress { get; set; }

        /// <summary>How this case was referred to the firm. E.g. "Walk-in", "Agent", "Referral - Dr. Ahmad".</summary>
        public string? ReferralSource { get; set; }

        // ── ACCIDENT PARAMETERS ───────────────────────────────────────────────

        /// <summary>Date on which the motor vehicle accident occurred.</summary>
        public DateTime? AccidentDate { get; set; }

        /// <summary>
        /// Role of the client in the accident.
        /// E.g. "Driver", "Pillion Rider", "Passenger", "Pedestrian", "Cyclist".
        /// </summary>
        public string ClientVehicleRole { get; set; } = "Driver";

        // ── ASSIGNMENT ────────────────────────────────────────────────────────

        /// <summary>The caseworker currently responsible for managing this file.</summary>
        public int? AssignedCaseworkerId { get; set; }

        // ── FINANCIAL LEDGER ──────────────────────────────────────────────────

        /// <summary>Total amount claimed by the claimant across all heads of damage.</summary>
        public decimal ClaimedAmount { get; set; }

        /// <summary>Minimum acceptable settlement amount as assessed by the solicitor.</summary>
        public decimal MinimumSettlementTarget { get; set; }

        /// <summary>Latest settlement offer tabled by the opposing insurer.</summary>
        public decimal CurrentOffer { get; set; }

        /// <summary>
        /// Running total of all disbursements paid on behalf of the claimant.
        /// Line-item breakdown is maintained in CaseDisbursement records.
        /// </summary>
        public decimal TotalDisbursementAmount { get; set; }

        // ── STATUS & LITIGATION ───────────────────────────────────────────────

        /// <summary>Foreign key to CaseStatus lookup (Active, Negotiation, Litigation, Settled, Closed).</summary>
        public int? CaseStatusId { get; set; }

        /// <summary>Foreign key to HearingStage — the current or most recent court hearing type.</summary>
        public int? CurrentHearingStageId { get; set; }

        /// <summary>Date of the next scheduled court appearance.</summary>
        public DateTime? NextHearingDate { get; set; }

        /// <summary>Date on which the case was formally closed.</summary>
        public DateTime? CaseClosedDate { get; set; }

        // ── LITIGATION FLAGS ──────────────────────────────────────────────────

        /// <summary>Indicates the case has been escalated to formal court litigation.</summary>
        public bool IsInLitigation { get; set; }

        /// <summary>Indicates this is an active, open case file.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Indicates the case has been fully closed and concluded.</summary>
        public bool IsClosed { get; set; }

        /// <summary>Date summons were drafted by the solicitor.</summary>
        public DateTime? SummonsDraftedDate { get; set; }

        /// <summary>Date the court sealed (stamped) the summons, making it legally served.</summary>
        public DateTime? SummonsSealedDate { get; set; }

        // ── AUDIT TIMESTAMPS ──────────────────────────────────────────────────

        /// <summary>UTC timestamp when this case file was first created in the system.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the most recent modification to this record.</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>General remarks or flags visible to all caseworkers.</summary>
        public string? Remarks { get; set; }

        // ── COMPUTED PROPERTIES ───────────────────────────────────────────────

        /// <summary>
        /// Claimant's current age in years, calculated from DateOfBirth.
        /// Not persisted — recalculated on every access.
        /// </summary>
        public int ClaimantAge => DateOfBirth.HasValue
            ? (DateTime.Today.Year - DateOfBirth.Value.Year -
              (DateTime.Today.DayOfYear < DateOfBirth.Value.DayOfYear ? 1 : 0))
            : 0;

        /// <summary>
        /// Statute of limitations deadline — 6 years from the accident date under Malaysian law.
        /// Returns null if AccidentDate is not set. Used to trigger limitation alerts.
        /// </summary>
        public DateTime? StatuteOfLimitationsDeadline => AccidentDate?.AddYears(6);

        /// <summary>
        /// Returns true if the statute of limitations deadline has passed and no case has been filed.
        /// </summary>
        public bool IsLimitationExpired => StatuteOfLimitationsDeadline.HasValue
            && !IsInLitigation
            && DateTime.Today > StatuteOfLimitationsDeadline.Value;

        // ── NAVIGATION PROPERTIES — LOOKUPS ────────

        public virtual CaseStatus? CaseStatus { get; set; }
        public virtual HearingStage? CurrentHearingStage { get; set; }
        public virtual CaseworkerProfile? AssignedCaseworker { get; set; }

        // ── NAVIGATION PROPERTIES — ONE-TO-MANY ──────────────────────────────

        public virtual ICollection<AccidentVehicle> AccidentVehicles { get; set; } = new List<AccidentVehicle>();
        public virtual ICollection<InjuryRecord> InjuryRecords { get; set; } = new List<InjuryRecord>();
        public virtual ICollection<CaseDocument> CaseDocuments { get; set; } = new List<CaseDocument>();
        public virtual ICollection<HearingRecord> HearingRecords { get; set; } = new List<HearingRecord>();
        public virtual ICollection<SettlementOffer> SettlementOffers { get; set; } = new List<SettlementOffer>();
        public virtual ICollection<CaseAppointment> CaseAppointments { get; set; } = new List<CaseAppointment>();
        public virtual ICollection<MedicalExamination> MedicalExaminations { get; set; } = new List<MedicalExamination>();
        public virtual ICollection<CaseCorrespondence> Correspondences { get; set; } = new List<CaseCorrespondence>();
        public virtual ICollection<CaseDisbursement> Disbursements { get; set; } = new List<CaseDisbursement>();
        public virtual ICollection<CaseJournal> JournalEntries { get; set; } = new List<CaseJournal>();
    }
}
