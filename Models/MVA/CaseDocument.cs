//CaseDocument
namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Tracks each document required or received for a case file.
    /// Serves as a compliance checklist and a digital document register.
    /// Covers both documents to be collected (outstanding) and those already received.
    /// </summary>
    public class CaseDocument
    {
        public int Id { get; set; }

        /// <summary>Foreign key linking this document entry to its parent case file.</summary>
        public int CaseFileId { get; set; }

        // ── DOCUMENT IDENTITY ─────────────────────────────────────────────────

        /// <summary>
        /// Descriptive name of the document.
        /// E.g. "Police Report", "Hospital Case Note", "Specialist Medical Report", "Motor Vehicle Assessment".
        /// </summary>
        public string DocumentName { get; set; } = null!;

        /// <summary>
        /// Broad category for grouping and filtering.
        /// Values: "Police", "Medical", "Legal", "Insurance", "Financial", "Court", "Other".
        /// </summary>
        public string DocumentCategory { get; set; } = "Other";

        /// <summary>
        /// The party responsible for supplying this document.
        /// E.g. "Client", "Hospital", "Police", "Insurer", "Court Registry".
        /// </summary>
        public string? ExpectedFrom { get; set; }

        // ── RECEIPT STATUS ────────────────────────────────────────────────────

        /// <summary>Indicates whether the document has been received by the firm.</summary>
        public bool IsReceived { get; set; }

        /// <summary>Date the document was physically or digitally received.</summary>
        public DateTime? ReceivedDate { get; set; }

        /// <summary>
        /// Current status in the collection workflow.
        /// Values: "Awaiting", "Requested", "Received", "Reviewed", "Not Required".
        /// </summary>
        public string CollectionStatus { get; set; } = "Awaiting";

        // ── DIGITAL STORAGE ───────────────────────────────────────────────────

        /// <summary>Server-side file path or storage key for the uploaded digital copy.</summary>
        public string? DigitalStoragePath { get; set; }

        /// <summary>Original filename of the uploaded file for display purposes.</summary>
        public string? OriginalFileName { get; set; }

        /// <summary>MIME type of the uploaded file. E.g. "application/pdf", "image/jpeg".</summary>
        public string? FileMimeType { get; set; }

        // ── AUDIT ─────────────────────────────────────────────────────────────

        /// <summary>Username or staff ID of the person who uploaded the digital copy.</summary>
        public string? UploadedBy { get; set; }

        /// <summary>UTC timestamp when the digital copy was uploaded to the system.</summary>
        public DateTime? UploadedAt { get; set; }

        /// <summary>Additional remarks about this document. E.g. "Pages 3–5 missing. Re-requested 14/06/2024".</summary>
        public string? Remarks { get; set; }

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────────────

        public virtual CaseFile CaseFile { get; set; } = null!;
    }
}
