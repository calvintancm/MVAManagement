namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Registry of insurance providers involved in MVA claims.
    /// Used to identify the insurer for both the client's and third-party's vehicles.
    /// </summary>
    public class InsurerRegistry
    {
        public int Id { get; set; }

        /// <summary>Full registered name of the insurance company. E.g. "Allianz General Insurance Malaysia Berhad".</summary>
        public string InsurerName { get; set; } = null!;

        /// <summary>Internal short code for reporting. E.g. "ALZ", "PRU", "ZUR".</summary>
        public string? InsurerCode { get; set; }

        /// <summary>Primary claims contact telephone for the insurer.</summary>
        public string? ClaimsContactNumber { get; set; }

        /// <summary>Claims department email address.</summary>
        public string? ClaimsEmail { get; set; }

        /// <summary>Whether this insurer is currently active in the registry.</summary>
        public bool IsActive { get; set; } = true;

        // Navigation Property
        public virtual ICollection<AccidentVehicle> InsuredVehicles { get; set; } = new List<AccidentVehicle>();
    }
}
