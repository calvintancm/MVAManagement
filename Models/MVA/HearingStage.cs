namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Lookup table defining the type or stage of a court hearing within litigation.
    /// Examples: Mention (M), Full Hearing (H), Case Management (CM), Settlement Conference (SC).
    /// </summary>
    public class HearingStage
    {
        public int Id { get; set; }

        /// <summary>Short code used in scheduling and event records. E.g. "M", "H", "CM".</summary>
        public string StageCode { get; set; } = null!;

        /// <summary>Full description of the hearing stage. E.g. "Case Management Conference".</summary>
        public string StageDescription { get; set; } = null!;

        /// <summary>Sort order for dropdowns and display lists.</summary>
        public int DisplayOrder { get; set; } = 0;

        // Navigation Properties
        public virtual ICollection<CaseFile> CaseFiles { get; set; } = new List<CaseFile>();
        public virtual ICollection<HearingRecord> HearingRecords { get; set; } = new List<HearingRecord>();
    }
}
