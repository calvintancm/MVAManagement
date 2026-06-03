namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Records the full chronological history of settlement offers exchanged between
    /// the firm and the opposing insurer or defendant for a case file.
    /// Enables trend analysis and negotiation audit trail.
    /// </summary>
    public class SettlementOffer
    {
        public int Id { get; set; }

        /// <summary>Foreign key linking this offer to its parent case file.</summary>
        public int CaseFileId { get; set; }

        // ── OFFER DETAILS ─────────────────────────────────────────────────────

        /// <summary>Date this offer was tabled.</summary>
        public DateTime OfferDate { get; set; }

        /// <summary>Monetary amount of this offer.</summary>
        public decimal OfferAmount { get; set; }

        /// <summary>
        /// Direction of the offer.
        /// Values: "From Insurer" (offer received), "Counter Offer" (offer made by firm).
        /// </summary>
        public string OfferDirection { get; set; } = "From Insurer";

        /// <summary>
        /// Current status of this specific offer.
        /// Values: "Open", "Accepted", "Rejected", "Countered", "Lapsed".
        /// </summary>
        public string OfferStatus { get; set; } = "Open";

        /// <summary>Name or designation of the person who tabled this offer. E.g. "Mr. Tan, Allianz Claims".</summary>
        public string? OfferedBy { get; set; }

        /// <summary>
        /// Reference number provided by the insurer for this offer, if any.
        /// Useful for correspondence follow-up.
        /// </summary>
        public string? InsurerOfferReference { get; set; }

        /// <summary>Deadline by which this offer must be accepted or rejected.</summary>
        public DateTime? OfferExpiryDate { get; set; }

        // ── HEADS OF DAMAGE BREAKDOWN (Optional) ─────────────────────────────

        /// <summary>Portion of the offer allocated to general damages (pain, suffering, loss of amenity).</summary>
        public decimal? GeneralDamagesComponent { get; set; }

        /// <summary>Portion of the offer allocated to special damages (medical expenses, loss of income).</summary>
        public decimal? SpecialDamagesComponent { get; set; }

        // ── NOTES ─────────────────────────────────────────────────────────────

        /// <summary>Conditions, caveats, or negotiation notes attached to this offer.</summary>
        public string? Notes { get; set; }

        // ── AUDIT ─────────────────────────────────────────────────────────────

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────────────

        public virtual CaseFile CaseFile { get; set; } = null!;
    }
}
