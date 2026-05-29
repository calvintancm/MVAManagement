namespace MVAManagement.Models.MVA
{
    public class LegalCase
    {
        public int Id { get; set; }
        public string FileNumber { get; set; } = null!; // Refactored from FileNo
        public string PrimaryClaimantName { get; set; } = null!; // Refactored from Name
        public string? SecondaryClaimantName { get; set; } // Refactored from SName
        public string? Gender { get; set; } // Refactored from Sex
        public DateTime? DateOfBirth { get; set; } // Refactored from DOB
        public string? NationalId { get; set; } // Refactored from IC (Identity Card)
        public string? TelephoneNumber { get; set; } // Refactored from Tel
        public string? MobileNumber { get; set; } // Refactored from HP
        public string? FullAddress { get; set; } // Consolidated from Add1, Add2, Add3

        // Accident Parameters
        public DateTime? AccidentDate { get; set; } // Refactored from DOA
        public string ClientVehicleRole { get; set; } = "Driver"; // e.g., Driver, Passenger, Pedestrian
        public string? AssignedStaff { get; set; } // Refactored from Incharge
        public string? CourtCaseNumber { get; set; } // Refactored from CourtCaseNo

        // Financial Ledger (Decimals preserve exact monetary precision)
        public decimal ClaimedAmount { get; set; } // Refactored from OSum
        public decimal MinimumSettlementTarget { get; set; } // Refactored from MinSum
        public decimal CurrentOffer { get; set; } // Refactored from Offer
        public decimal TotalDisbursementAmount { get; set; } // Refactored from Disbursment

        // Status Management Foreign Keys
        public int? ClientStatusId { get; set; } // Refactored from Status
        public int? CurrentHearingTypeId { get; set; } // Refactored from Litigation
        public DateTime? NextCourtDate { get; set; } // Refactored from LitigationDate
        public DateTime? CaseClosedDate { get; set; } // Refactored from CaseCloseDate

        // Business Logic Control Properties (Safely tracks bits as standard booleans)
        public bool IsInLitigation { get; set; } // Refactored from Litigate
        public bool IsActive { get; set; } // Refactored from Active
        public bool IsClosed { get; set; } // Refactored from CaseClose
        public DateTime? SummonsDraftedDate { get; set; } // Refactored from DraftDate
        public DateTime? SummonsSealedDate { get; set; } // Refactored from SealDate

        // Audit Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Refactored from CreationDate
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Refactored from ModifiedDate
        public string? Remarks { get; set; }
        public string? Notes { get; set; }

        // C# Computed Property (Calculates age automatically without saving stale data)
        public int ClaimantAge => DateOfBirth.HasValue ? (DateTime.Today.Year - DateOfBirth.Value.Year) : 0;

        // Automated Limitation Safeguard (Standard 6-year legal window alert calculation)
        public DateTime? StatuteOfLimitationsDeadline => AccidentDate?.AddYears(6);

        // Navigation Properties (Lookup Assignments)
        public virtual ClientStatus? ClientStatus { get; set; }
        public virtual CourtHearingType? CurrentHearingType { get; set; }

        // Navigation Properties (One-To-Many Relational Dependencies)
        public virtual ICollection<VehicleDetail> Vehicles { get; set; } = new List<VehicleDetail>();
        public virtual ICollection<ClaimantInjury> Injuries { get; set; } = new List<ClaimantInjury>();
        public virtual ICollection<DocumentChecklist> DocumentChecklists { get; set; } = new List<DocumentChecklist>();
        public virtual ICollection<LitigationEvent> LitigationEvents { get; set; } = new List<LitigationEvent>();
        public virtual ICollection<OfferHistory> OfferHistories { get; set; } = new List<OfferHistory>();
        public virtual ICollection<OperationalSchedule> Schedules { get; set; } = new List<OperationalSchedule>();
    }
}

