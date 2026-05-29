namespace MVAManagement.Models.MVA
{
    public class UserSessionLog
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string SessionId { get; set; } = null!;
        public string IpAddress { get; set; } = null!;
        public string ComputerName { get; set; } = null!;
        public string? UserAgent { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public bool IsActive { get; set; }
    }
}
