//HearingRecord
namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Records each individual court hearing or litigation event associated with a case file.
    /// Maintains the full chronological litigation history from first mention to final disposal.
    /// </summary>
    public class HearingRecord
    {
        public int Id { get; set; }

        /// <summary>Foreign key linking this hearing to its parent case file.</summary>
        public int CaseFileId { get; set; }

        // ── HEARING CLASSIFICATION ────────────────────────────────────────────

        /// <summary>Foreign key to HearingStage lookup defining the type of hearing.</summary>
        public int HearingStageId { get; set; }

        /// <summary>Foreign key to CourtVenue where this hearing is or was held.</summary>
        public int? CourtVenueId { get; set; }

        // ── SCHEDULING ────────────────────────────────────────────────────────

        /// <summary>Date on which this hearing is or was scheduled.</summary>
        public DateTime ScheduledDate { get; set; }

        /// <summary>Hearing time, if known at the time of scheduling.</summary>
        public TimeSpan? HearingTime { get; set; }

        /// <summary>Courtroom or chamber number within the court venue.</summary>
        public string? CourtroomNumber { get; set; }

       

        // ── PRESIDING OFFICER ─────────────────────────────────────────────────

        /// <summary>Name of the presiding judge or magistrate for this hearing.</summary>
        public string? PresidingJudge { get; set; }

        // ── OUTCOME ───────────────────────────────────────────────────────────

        /// <summary>Indicates this hearing has been conducted and concluded.</summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Outcome or order issued at this hearing.
        /// E.g. "Adjourned to next mention", "Consent judgement entered", "Interlocutory order granted".
        /// </summary>
        public string? HearingOutcome { get; set; }

        /// <summary>Date to which the case was adjourned, if applicable.</summary>
        public DateTime? AdjournedToDate { get; set; }

        /// <summary>
        /// Detailed progress notes recorded by the attending solicitor or clerk.
        /// Documents what transpired during the hearing.
        /// </summary>
        public string? ProgressNotes { get; set; }

        // ── AUDIT ─────────────────────────────────────────────────────────────

        /// <summary>UTC timestamp when this hearing record was created in the system.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────────────

        public virtual CaseFile CaseFile { get; set; } = null!;
        public virtual HearingStage HearingStage { get; set; } = null!;
        public virtual CourtVenue? CourtVenue { get; set; }
    }
}
