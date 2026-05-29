namespace MVAManagement.Models.MVA
{
    public class CourtHearingType
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!; // e.g., "M", "H"
        public string Description { get; set; } = null!; // Refactored from Desc

        // Navigation Property
        public virtual ICollection<LegalCase> LegalCases { get; set; } = new List<LegalCase>();
    }
}
