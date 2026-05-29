namespace MVAManagement.Models.MVA
{
    public class LitigationEvent
    {
        public int Id { get; set; }
        public int LegalCaseId { get; set; } // Refactored from FK_ID
        public string HearingTypeCode { get; set; } = null!; // Refactored from LitType ("M", "H")
        public DateTime ScheduledDate { get; set; } // Refactored from LitDate
        public string? ProgressNotes { get; set; } // Refactored from Desc
        public bool IsCompleted { get; set; } // Refactored from Completed short conversion

        public virtual LegalCase LegalCase { get; set; } = null!;
    }
}
