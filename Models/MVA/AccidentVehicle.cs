namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Records details of each vehicle involved in the accident linked to a case file.
    /// A case file may have multiple vehicles: the client's own vehicle and one or more
    /// third-party (opponent) vehicles.
    /// </summary>
    public class AccidentVehicle
    {
        public int Id { get; set; }

        /// <summary>Foreign key linking this vehicle record to its parent case file.</summary>
        public int CaseFileId { get; set; }

        // ── VEHICLE IDENTIFICATION ────────────────────────────────────────────

        /// <summary>Vehicle registration / number plate. E.g. "WXY 1234".</summary>
        public string PlateNumber { get; set; } = null!;

        /// <summary>Vehicle make and model. E.g. "Toyota Vios 1.5G".</summary>
        public string? VehicleMakeModel { get; set; }

        /// <summary>Year of manufacture of the vehicle.</summary>
        public int? YearOfManufacture { get; set; }

        /// <summary>Vehicle colour as stated in the police report. E.g. "Silver".</summary>
        public string? VehicleColour { get; set; }

        /// <summary>
        /// Vehicle category. E.g. "Motorcycle", "Saloon Car", "Commercial Lorry", "Bus".
        /// Useful for determining applicable insurance limits.
        /// </summary>
        public string? VehicleCategory { get; set; }

        // ── DRIVER DETAILS ────────────────────────────────────────────────────

        /// <summary>Full name of the driver at the time of the accident.</summary>
        public string? DriverName { get; set; }

        /// <summary>Driver's national identity card or passport number.</summary>
        public string? DriverNationalId { get; set; }

        /// <summary>Driver's vehicle licence number.</summary>
        public string? DriverLicenceNumber { get; set; }

        /// <summary>Driver's contact telephone number.</summary>
        public string? DriverContactNumber { get; set; }

        // ── INSURANCE ─────────────────────────────────────────────────────────

        /// <summary>Foreign key to InsurerRegistry — the insurer covering this vehicle.</summary>
        public int? InsuranceProviderId { get; set; }

        /// <summary>Insurance policy number for this vehicle at the time of the accident.</summary>
        public string? PolicyNumber { get; set; }

        /// <summary>Coverage type. E.g. "Comprehensive", "Third Party".</summary>
        public string? CoverageType { get; set; }

        // ── ROLE FLAG ─────────────────────────────────────────────────────────

        /// <summary>
        /// True if this is the client's vehicle. False if this is the opposing/third-party vehicle.
        /// A case file should have exactly one client vehicle; opponents may be multiple.
        /// </summary>
        public bool IsClientVehicle { get; set; }

        // ── NAVIGATION PROPERTIES ─────────────────────────────────────────────

        public virtual CaseFile CaseFile { get; set; } = null!;
        public virtual InsurerRegistry? InsuranceProvider { get; set; }
    }
}
