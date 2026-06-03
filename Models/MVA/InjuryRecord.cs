namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Records each distinct injury sustained by the claimant as a result of the accident.
    /// A single case file may have multiple injury records — one per diagnosed condition.
    /// </summary>
    public class InjuryRecord
    {
        public int Id { get; set; }

        /// <summary>Foreign key linking this injury to its parent case file.</summary>
        public int CaseFileId { get; set; }

        // ── INJURY CLASSIFICATION ─────────────────────────────────────────────

        /// <summary>
        /// Clinical description of the injury as documented in the medical report.
        /// E.g. "Compound Fracture of Right Femur", "Cervical Spine Strain (Whiplash)".
        /// </summary>
        public string InjuryDescription { get; set; } = null!;

        /// <summary>
        /// Anatomical body part affected. E.g. "Right Femur", "Lumbar Spine", "Left Wrist".
        /// Enables grouping and reporting across similar injury types.
        /// </summary>
        public string? BodyPart { get; set; }

        /// <summary>
        /// Severity classification. Values: "Minor", "Moderate", "Severe", "Permanent Disability", "Fatal".
        /// </summary>
        public string SeverityLevel { get; set; } = "Minor";

        /// <summary>
        /// Whether the injury has resulted in a permanent impairment or disability.
        /// Drives additional heads of damage (future loss of earnings, cost of care).
        /// </summary>
        public bool IsPermanentDisability { get; set; }

        /// <summary>
        /// Estimated percentage of permanent disability, if assessed by a medical specialist.
        /// E.g. 15.0 for 15% permanent disability.
        /// </summary>
        public decimal? DisabilityPercentage { get; set; }

        // ── TREATMENT ─────────────────────────────────────────────────────────

        /// <summary>
        /// Current treatment status.
        /// Values: "Ongoing", "Discharged", "Referred to Specialist", "Surgery Required", "Completed".
        /// </summary>
        public string TreatmentStatus { get; set; } = "Ongoing";

        /// <summary>Name of the primary hospital or clinic where treatment was received.</summary>
        public string? HospitalName { get; set; }

        /// <summary>Name of the treating or examining doctor / specialist.</summary>
        public string? DoctorName { get; set; }

        /// <summary>Medical specialty of the treating doctor. E.g. "Orthopaedic", "Neurologist", "Plastic Surgery".</summary>
        public string? MedicalSpecialty { get; set; }

        // ── MEDICAL REPORT ────────────────────────────────────────────────────

        /// <summary>
        /// Status of the medical report for this injury.
        /// Values: "Pending", "Requested", "Received", "Reviewed".
        /// </summary>
        public string MedicalReportStatus { get; set; } = "Pending";

        /// <summary>Date the medical report was received from the hospital or specialist.</summary>
        public DateTime? MedicalReportReceivedDate { get; set; }

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────────────

        public virtual CaseFile CaseFile { get; set; } = null!;
        public virtual ICollection<MedicalExamination> MedicalExaminations { get; set; } = new List<MedicalExamination>();
    }
}
