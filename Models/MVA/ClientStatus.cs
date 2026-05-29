namespace MVAManagement.Models.MVA
{
    public class ClientStatus
    {
        public int Id { get; set; }
        public string StatusName { get; set; } = null!; // Refactored from ClientStatus
        public string? StatusCode { get; set; } // Refactored from Code

        // Navigation Property
        public virtual ICollection<LegalCase> LegalCases { get; set; } = new List<LegalCase>();
    }
}
