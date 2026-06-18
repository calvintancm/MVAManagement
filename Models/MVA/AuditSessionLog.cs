//AuditSessionLog
namespace MVAManagement.Models.MVA
{
    /// <summary>
    /// Immutable audit log of all user login sessions.
    /// Captures authentication events for security monitoring, compliance reporting,
    /// and forensic investigation in the event of a data breach or unauthorised access.
    /// Records are never modified — logout time is written once on session end.
    /// </summary>
    public class AuditSessionLog
    {
        public int Id { get; set; }

        // ── IDENTITY ──────────────────────────────────────────────────────────

        /// <summary>ASP.NET Identity UserId of the authenticated user.</summary>
        public string UserId { get; set; } = null!;

        /// <summary>Display username at the time of login, captured for audit resilience.</summary>
        public string Username { get; set; } = null!;

        // ── SESSION ───────────────────────────────────────────────────────────

        /// <summary>Unique identifier for this session, issued at login.</summary>
        public string SessionId { get; set; } = null!;

        // ── NETWORK & DEVICE ──────────────────────────────────────────────────

        /// <summary>IP address of the client at the time of login.</summary>
        public string IpAddress { get; set; } = null!;

        /// <summary>Network hostname or computer name of the client machine, where resolvable.</summary>
        public string? ComputerName { get; set; }

        /// <summary>HTTP User-Agent string from the login request, capturing browser and OS details.</summary>
        public string? UserAgent { get; set; }

        // ── TIMESTAMPS ────────────────────────────────────────────────────────

        /// <summary>UTC timestamp of the successful login event.</summary>
        public DateTime LoginTime { get; set; }

        /// <summary>UTC timestamp of session termination (logout or timeout). Null if session is active.</summary>
        public DateTime? LogoutTime { get; set; }

        /// <summary>
        /// Duration of the session in minutes. Computed on logout.
        /// Null if the session is still active.
        /// </summary>
        public int? SessionDurationMinutes => LogoutTime.HasValue
            ? (int)(LogoutTime.Value - LoginTime).TotalMinutes
            : null;

        // ── STATUS ────────────────────────────────────────────────────────────

        /// <summary>Whether this session is currently active.</summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// How the session ended, for security analysis.
        /// Values: "User Logout", "Session Timeout", "Forced Termination", "System Restart".
        /// Null if session is still active.
        /// </summary>
        public string? LogoutReason { get; set; }

        /// <summary>
        /// Whether this login was flagged as suspicious by the system
        /// (e.g. unusual IP, login outside business hours, multiple failed attempts prior).
        /// </summary>
        public bool IsFlaggedSuspicious { get; set; }

        /// <summary>Reason for flagging, if applicable.</summary>
        public string? SuspicionReason { get; set; }
    }
}
