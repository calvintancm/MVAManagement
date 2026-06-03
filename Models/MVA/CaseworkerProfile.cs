namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Represents a firm staff member who manages or contributes to case files.
    /// Linked to ASP.NET Identity via UserId for authentication; stores professional profile separately.
    /// </summary>
    public class CaseworkerProfile
    {
        public int Id { get; set; }

        /// <summary>Foreign key to ASP.NET Identity user. Links authentication to this profile.</summary>
        public string? IdentityUserId { get; set; }

        /// <summary>Full legal name of the staff member.</summary>
        public string FullName { get; set; } = null!;

        /// <summary>Login username, mirrored from Identity for display purposes.</summary>
        public string Username { get; set; } = null!;

        /// <summary>Professional role within the firm. E.g. "Senior Solicitor", "Legal Clerk", "Admin".</summary>
        public string JobRole { get; set; } = null!;

        /// <summary>System access role for authorisation. E.g. "Solicitor", "Adjuster", "Investigator", "Admin".</summary>
        public string SystemRole { get; set; } = null!;

        /// <summary>Work email address.</summary>
        public string? Email { get; set; }

        /// <summary>Direct office or mobile contact number.</summary>
        public string? ContactNumber { get; set; }

        /// <summary>Date this staff member joined the firm or was added to the system.</summary>
        public DateTime? JoinedDate { get; set; }

        /// <summary>Whether this account is currently active. Deactivate on resignation rather than deleting.</summary>
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<CaseFile> AssignedCases { get; set; } = new List<CaseFile>();
        public virtual ICollection<CaseJournal> JournalEntries { get; set; } = new List<CaseJournal>();
    }
}
