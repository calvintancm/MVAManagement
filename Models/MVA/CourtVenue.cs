namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Lookup table for court venues where hearings are conducted.
    /// Enables scheduling, reporting, and attendance tracking by venue.
    /// </summary>
    public class CourtVenue
    {
        public int Id { get; set; }

        /// <summary>Official name of the court. E.g. "Mahkamah Sesyen Kuala Lumpur".</summary>
        public string VenueName { get; set; } = null!;

        /// <summary>Short identifier for display in lists. E.g. "MSKL".</summary>
        public string? VenueCode { get; set; }

        /// <summary>State or district jurisdiction this court serves.</summary>
        public string? Jurisdiction { get; set; }

        /// <summary>Full postal address of the court building.</summary>
        public string? Address { get; set; }

        /// <summary>Contact telephone number for the court registry.</summary>
        public string? ContactNumber { get; set; }

        // Navigation Property
        public virtual ICollection<HearingRecord> HearingRecords { get; set; } = new List<HearingRecord>();
    }
}
