namespace MVAManagement.Models.MVA
{
    public class OperationalSchedule
    {
        public int Id { get; set; }
        public int LegalCaseId { get; set; } // Refactored from FK_ID
        public DateTime AppointmentDate { get; set; } // Refactored from SchDate
        public TimeSpan? AppointmentTime { get; set; } // Refactored from SchTime
        public string Subject { get; set; } = null!; // e.g., "Send client to Dr. Thiru"
        public string? ActivityType { get; set; } // Refactored from Type ("ME" = Med Exam, "CA" = Case Appt)
        public string? ContextNotes { get; set; } // Refactored from OthersInfo
        public bool IsCompleted { get; set; }

        public virtual LegalCase LegalCase { get; set; } = null!;
    }
}
