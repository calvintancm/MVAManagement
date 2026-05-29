namespace MVAManagement.Models.MVA
{
    public class ClaimantInjury
    {
        public int Id { get; set; }
        public int LegalCaseId { get; set; }
        public string InjuryDescription { get; set; } = null!; // e.g., "Compound Fracture of Right Femur"
        public string SeverityLevel { get; set; } = "Minor"; // e.g., Minor, Severe, Permanent Disability
        public string MedicalReportStatus { get; set; } = "Pending"; // e.g., Pending, Received

        public virtual LegalCase LegalCase { get; set; } = null!;
    }
}
