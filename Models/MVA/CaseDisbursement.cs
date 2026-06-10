using System.ComponentModel.DataAnnotations.Schema;

namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Records individual disbursement line items paid on behalf of the claimant.
    /// Provides the detailed breakdown behind CaseFile.TotalDisbursementAmount.
    /// All disbursements are recoverable from the defendant or insurer upon successful settlement.
    /// </summary>
    public class CaseDisbursement
    {
        public int Id { get; set; }

        /// <summary>Foreign key linking this disbursement to its parent case file.</summary>
        public int CaseFileId { get; set; }

        // ── DISBURSEMENT DETAILS ──────────────────────────────────────────────

        /// <summary>Date on which this disbursement was paid or incurred.</summary>
        public DateTime DisbursementDate { get; set; }

        /// <summary>
        /// Category of disbursement for reporting and cost recovery.
        /// Values: "Medical Report Fee", "Specialist Examination Fee", "Court Filing Fee",
        ///         "Police Report Fee", "Process Server Fee", "Expert Witness Fee",
        ///         "Interpreter Fee", "Postage / Courier", "Miscellaneous".
        /// </summary>
        public string DisbursementCategory { get; set; } = "Miscellaneous";

        /// <summary>
        /// Detailed description of what was paid for.
        /// E.g. "Specialist report fee — Dr. Suresh, Pantai KL (Orthopaedic)".
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>Name of the payee — doctor, court registry, courier company, etc.</summary>
        public string Payee { get; set; } = null!;

        /// <summary>Amount paid, in the local currency (MYR by default).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // ── PAYMENT DETAILS ───────────────────────────────────────────────────

        /// <summary>
        /// Payment method used.
        /// Values: "Cheque", "Online Transfer", "Cash", "Credit Card".
        /// </summary>
        public string? PaymentMethod { get; set; }

        /// <summary>Cheque number or transaction reference for this payment.</summary>
        public string? PaymentReference { get; set; }

        /// <summary>Receipt or invoice number issued by the payee.</summary>
        public string? ReceiptNumber { get; set; }

        // ── DIGITAL STORAGE ───────────────────────────────────────────────────

        /// <summary>Server-side file path or storage key for the scanned receipt or invoice.</summary>
        public string? ReceiptStoragePath { get; set; }

        // ── RECOVERY STATUS ───────────────────────────────────────────────────

        /// <summary>Indicates this disbursement has been recovered from the defendant or insurer.</summary>
        public bool IsRecovered { get; set; }

        /// <summary>Date the disbursement was recovered, if applicable.</summary>
        public DateTime? RecoveredDate { get; set; }

        // ── AUDIT ─────────────────────────────────────────────────────────────

        /// <summary>Username or staff ID of the person who recorded this disbursement.</summary>
        public string? RecordedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────────────

        public virtual CaseFile CaseFile { get; set; } = null!;
    }
}
