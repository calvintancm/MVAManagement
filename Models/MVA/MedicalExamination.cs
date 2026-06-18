//MedicalExamination
namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Records each medical examination or specialist assessment arranged for the claimant.
    /// Linked to both the parent case file and the specific injury being assessed.
    /// Distinct from CaseAppointment — this captures clinical outcome data, not just scheduling.
    /// </summary>
    public class MedicalExamination
    {
        public int Id { get; set; }

        /// <summary>Foreign key linking this examination to its parent case file.</summary>
        public int CaseFileId { get; set; }

        /// <summary>Foreign key to the specific injury record this examination relates to.</summary>
        public int? InjuryRecordId { get; set; }

        // ── EXAMINATION DETAILS ───────────────────────────────────────────────

        /// <summary>Name of the clinic, hospital, or specialist centre. E.g. "Pantai Hospital Kuala Lumpur".</summary>
        public string ClinicName { get; set; } = null!;

        /// <summary>Full name of the examining doctor or specialist.</summary>
        public string DoctorName { get; set; } = null!;

        /// <summary>Medical specialty of the examining doctor. E.g. "Orthopaedic Surgeon", "Neurologist".</summary>
        public string? Specialty { get; set; }

        /// <summary>
        /// Type of examination.
        /// E.g. "Medical Examination", "Specialist Referral", "Independent Medical Examination", "Follow-up".
        /// </summary>
        public string ExaminationType { get; set; } = "Medical Examination";

        // ── SCHEDULING ────────────────────────────────────────────────────────

        /// <summary>Date the examination is or was scheduled.</summary>
        public DateTime AppointmentDate { get; set; }

        /// <summary>Appointment time, if confirmed.</summary>
        public TimeSpan? AppointmentTime { get; set; }

        /// <summary>Whether the claimant attended the scheduled examination.</summary>
        public bool ClaimantAttended { get; set; }

        // ── REPORT TRACKING ───────────────────────────────────────────────────

        /// <summary>Date the medical report was formally requested from the doctor or facility.</summary>
        public DateTime? ReportRequestedDate { get; set; }

        /// <summary>Date the medical report was received by the firm.</summary>
        public DateTime? ReportReceivedDate { get; set; }

        /// <summary>
        /// Summary of the doctor's findings or diagnosis from this examination.
        /// Not a substitute for the full report — used for quick reference.
        /// </summary>
        public string? ExaminationFindings { get; set; }

        /// <summary>Fee charged for this medical examination or specialist report.</summary>
        public decimal? ExaminationFee { get; set; }

        // ── REFERRAL ──────────────────────────────────────────────────────────

        /// <summary>Who referred the claimant to this examination. E.g. "Firm", "Government Hospital", "GP".</summary>
        public string? ReferralSource { get; set; }

        /// <summary>Additional notes or instructions for this examination.</summary>
        public string? Notes { get; set; }

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────────────

        public virtual CaseFile CaseFile { get; set; } = null!;
        public virtual InjuryRecord? InjuryRecord { get; set; }
    }
}
