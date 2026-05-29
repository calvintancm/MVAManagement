namespace MVAManagement.Models.MVA
{
    public class VehicleDetail
    {
        public int Id { get; set; }
        public int LegalCaseId { get; set; } // Link back to LegalCase
        public string PlateNumber { get; set; } = null!; // Unified from RegNo / PRegNo
        public string? VehicleMakeModel { get; set; }
        public string? DriverName { get; set; }
        public string? DriverNationalId { get; set; }
        public int? InsuranceProviderId { get; set; } // Links to InsuranceProvider
        public bool IsClientVehicle { get; set; } // Identifies if it's the client vs. opponent vehicle

        public virtual LegalCase LegalCase { get; set; } = null!;
        public virtual InsuranceProvider? InsuranceProvider { get; set; }
    }
}
