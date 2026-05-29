namespace MVAManagement.Models.MVA
{
    public class InsuranceProvider
    {
        public int Id { get; set; }
        public string ProviderName { get; set; } = null!; // Refactored from Name
        public string? ProviderCode { get; set; } // Refactored from Code

        // Navigation Properties
        public virtual ICollection<VehicleDetail> InsuredVehicles { get; set; } = new List<VehicleDetail>();
    }
}
