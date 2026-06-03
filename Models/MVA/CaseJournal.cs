namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Running case diary maintained by caseworkers throughout the life of a case file.
    /// Each entry is immutable once created, providing a tamper-evident chronological record
    /// of all significant developments, decisions, and communications.
    /// Replaces the flat Remarks / Notes fields on CaseFile with a structured, attributed log.
    /// </summary>
    public class CaseJournal
    {
        public int Id { get; set; }

        /// <summary>Foreign key linking this journal entry to its parent case file.</summary>
        public int CaseFileId { get; set; }

        /// <summary>Foreign key to the caseworker who authored this entry.</summary>
        public int AuthorCaseworkerId { get; set; }

        // ── ENTRY CONTENT ─────────────────────────────────────────────────────

        /// <summary>UTC timestamp when this entry was recorded. Immutable after creation.</summary>
        public DateTime EntryDateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Category of the journal entry for filtering and reporting.
        /// Values: "General", "Client Communication", "Insurer Communication", "Medical Update",
        ///         "Legal Strategy", "Court Update", "Financial", "Internal Note".
        /// </summary>
        public string EntryCategory { get; set; } = "General";

        /// <summary>
        /// Full text of the journal entry as recorded by the caseworker.
        /// Should be factual and objective — describes what occurred, not opinions.
        /// </summary>
        public string EntryText { get; set; } = null!;

        // ── VISIBILITY ────────────────────────────────────────────────────────

        /// <summary>
        /// Visibility scope of this entry.
        /// Values: "All Staff" (default), "Solicitors Only", "Private" (author only).
        /// </summary>
        public string Visibility { get; set; } = "All Staff";

        /// <summary>
        /// Whether this entry is flagged as important or requires follow-up action.
        /// Flagged entries are highlighted in the case diary view.
        /// </summary>
        public bool IsFlagged { get; set; }

        /// <summary>Follow-up action required, if any. E.g. "Confirm client attendance by 20/07/2024".</summary>
        public string? FollowUpAction { get; set; }

        /// <summary>Due date for any follow-up action linked to this entry.</summary>
        public DateTime? FollowUpDueDate { get; set; }

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────────────

        public virtual CaseFile CaseFile { get; set; } = null!;
        public virtual CaseworkerProfile Author { get; set; } = null!;
    }
}
