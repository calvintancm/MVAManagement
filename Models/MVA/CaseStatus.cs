//CaseStatus
namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Lookup table defining the lifecycle status of a case file.
    /// Examples: Active, Negotiation, Litigation, Settled, Closed, Withdrawn.
    /// </summary>
    public class CaseStatus
    {
        public int Id { get; set; }

        /// <summary>The full display name of the status. E.g. "Under Negotiation".</summary>
        public string StatusName { get; set; } = null!;

        /// <summary>Short code for filtering and reporting. E.g. "NEG", "LIT", "CLO".</summary>
        public string? StatusCode { get; set; }

        /// <summary>Display order for dropdowns and status pipelines.</summary>
        public int DisplayOrder { get; set; }

        //Navigation Property
        public virtual ICollection<CaseFile> CaseFiles { get; set; } = new List<CaseFile>();
    }
}
