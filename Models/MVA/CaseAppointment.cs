namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Records operational appointments and scheduled activities for a case file.
    /// Covers non-court activities such as client meetings, medical escort appointments,
    /// document collection runs, and internal review sessions.
    /// For formal court hearings, use HearingRecord. For medical examinations, use MedicalExamination.
    /// </summary>
    public class CaseAppointment
    {
        public int Id { get; set; }

        /// <summary>Foreign key linking this appointment to its parent case file.</summary>
        public int CaseFileId { get; set; }

        // ── APPOINTMENT DETAILS ───────────────────────────────────────────────

        /// <summary>Scheduled date of the appointment.</summary>
        public DateTime AppointmentDate { get; set; }

        /// <summary>Scheduled time of the appointment.</summary>
        public TimeSpan? AppointmentTime { get; set; }

        /// <summary>
        /// Subject or purpose of the appointment.
        /// E.g. "Accompany client to Dr. Ramasamy, SJMC", "Collect police report from IPD Cheras".
        /// </summary>
        public string Subject { get; set; } = null!;

        /// <summary>
        /// Activity type code for categorisation and filtering.
        /// Values: "ME" (Medical Examination), "CA" (Client Appointment), "DC" (Document Collection),
        ///         "IR" (Internal Review), "PH" (Phone Call), "OT" (Other).
        /// </summary>
        public string? ActivityType { get; set; }

        /// <summary>Location or venue for the appointment.</summary>
        public string? AppointmentLocation { get; set; }

        /// <summary>Staff member assigned to attend or manage this appointment.</summary>
        public int? AssignedCaseworkerId { get; set; }

        // ── STATUS ────────────────────────────────────────────────────────────

        /// <summary>Whether this appointment has been completed.</summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Outcome or result of the appointment once completed.
        /// E.g. "Client attended. Report requested.", "Rescheduled — client uncontactable."
        /// </summary>
        public string? CompletionNotes { get; set; }

        // ── ADDITIONAL CONTEXT ────────────────────────────────────────────────

        /// <summary>Any additional context, preparation instructions, or reference information.</summary>
        public string? ContextNotes { get; set; }

        // ── AUDIT ─────────────────────────────────────────────────────────────

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────────────

        public virtual CaseFile CaseFile { get; set; } = null!;
        public virtual CaseworkerProfile? AssignedCaseworker { get; set; }
    }
}
