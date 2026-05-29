namespace MVAManagement.Models.MVA
{
    public class OfferHistory
    {
        public int Id { get; set; }
        public int LegalCaseId { get; set; } // Refactored from FK_ID
        public DateTime OfferDate { get; set; }
        public decimal OfferAmount { get; set; } // Refactored from OfferAmt

        public virtual LegalCase LegalCase { get; set; } = null!;
    }
}
