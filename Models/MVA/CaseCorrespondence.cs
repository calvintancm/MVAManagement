namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Records all formal correspondence sent and received in relation to a case file.
    /// Maintains a complete communication audit trail: demand letters, insurer replies,
    /// court notices, client notifications, and inter-party communications.
    /// </summary>
    public class CaseCorrespondence
    {
        public int Id { get; set; }

        /// <summary>Foreign key linking this correspondence to its parent case file.</summary>
        public int CaseFileId { get; set; }

        // ── CORRESPONDENCE IDENTITY ───────────────────────────────────────────

        /// <summary>Date on which this correspondence was sent or received.</summary>
        public DateTime CorrespondenceDate { get; set; }

        /// <summary>
        /// Direction of communication relative to the firm.
        /// Values: "Outgoing" (sent by firm), "Incoming" (received by firm).
        /// </summary>
        public string Direction { get; set; } = "Outgoing";

        /// <summary>
        /// Channel through which this correspondence was transmitted.
        /// Values: "Letter", "Email", "Fax", "Courier", "Hand Delivered", "Court Filing".
        /// </summary>
        public string Channel { get; set; } = "Letter";

        /// <summary>Subject line or brief title of the correspondence.</summary>
        public string Subject { get; set; } = null!;

        /// <summary>
        /// Category of correspondence for filtering and reporting.
        /// Values: "Demand Letter", "Insurer Reply", "Court Notice", "Client Notification",
        ///         "Without Prejudice", "Open Letter", "Internal Memo".
        /// </summary>
        public string CorrespondenceType { get; set; } = "Letter";

        // ── PARTIES ───────────────────────────────────────────────────────────

        /// <summary>
        /// Name of the recipient (if outgoing) or the sender (if incoming).
        /// E.g. "Allianz General Insurance — Claims Dept", "Mahkamah Sesyen KL".
        /// </summary>
        public string CounterpartyName { get; set; } = null!;

        /// <summary>Reference number assigned by the counterparty, if provided.</summary>
        public string? CounterpartyReference { get; set; }

        // ── TRACKING ──────────────────────────────────────────────────────────

        /// <summary>Firm's own reference number for this piece of correspondence.</summary>
        public string? FirmReference { get; set; }

        /// <summary>Date a response to this correspondence is due, if applicable.</summary>
        public DateTime? ResponseDueDate { get; set; }

        /// <summary>Indicates a response has been received or sent for this correspondence.</summary>
        public bool IsResponseReceived { get; set; }

        // ── DIGITAL STORAGE ───────────────────────────────────────────────────

        /// <summary>Server-side file path or storage key for the digital copy of this document.</summary>
        public string? DigitalStoragePath { get; set; }

        /// <summary>Additional notes or context about this correspondence.</summary>
        public string? Notes { get; set; }

        // ── AUDIT ─────────────────────────────────────────────────────────────

        /// <summary>Username or staff ID of the person who logged this correspondence.</summary>
        public string? LoggedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────────────

        public virtual CaseFile CaseFile { get; set; } = null!;
    }
}
