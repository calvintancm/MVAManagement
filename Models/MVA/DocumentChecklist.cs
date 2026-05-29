namespace MVAManagement.Models.MVA
{
    public class DocumentChecklist
    {
        public int Id { get; set; }
        public int LegalCaseId { get; set; }
        public string DocumentName { get; set; } = null!; // e.g., "Police Report", "Hospital Case Note"
        public bool IsReceived { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public string? DigitalStoragePath { get; set; } // Server path for uploaded file attachments

        public virtual LegalCase LegalCase { get; set; } = null!;
    }
}
